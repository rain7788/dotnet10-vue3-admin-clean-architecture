# 定时任务与长任务

Art Admin 使用自研任务调度器，支持**周期性任务**和**长期运行任务**两种模式，内置分布式锁防重。

## 两种任务类型

| 类型 | 方法 | 适用场景 |
| --- | --- | --- |
| Recurring | `AddRecurringTask` | 周期性执行（日志清理、数据统计） |
| LongRunning | `AddLongRunningTask` | 持续运行（消息队列消费、延迟队列） |

## 周期性任务

```csharp
// 日志清理任务
taskScheduler.AddRecurringTask(
    _dailyWorker.ClearLogs,
    interval: TimeSpan.FromMinutes(21),  // 每 21 分钟检查
    allowedHours: [2, 3],                // 只在凌晨 2-3 点执行
    preventDuplicateInterval: TimeSpan.FromHours(12), // 12小时内不重复
    useDistributedLock: true             // 多 Pod 只有一个执行
);
```

### 参数说明

| 参数 | 说明 |
| --- | --- |
| `interval` | 调度间隔（多久检查一次） |
| `allowedHours` | 允许执行的小时范围（不在范围内跳过） |
| `preventDuplicateInterval` | 防重复间隔（Redis 去重 Key） |
| `useDistributedLock` | 是否启用分布式锁（多 Pod 互斥） |

## 长期运行任务

```csharp
// 消息队列消费
taskScheduler.AddLongRunningTask(
    _demoMessageQueueWorker.ProcessQueue,
    interval: TimeSpan.FromSeconds(1),         // 窗口结束或抢锁失败后的冷却间隔
    processingInterval: TimeSpan.FromMilliseconds(100), // 每次处理后的间隔
    runDuration: TimeSpan.FromSeconds(30),     // 运行窗口时长
    taskName: "demo.queue.consume"
);
```

### 两层节奏

```
初始抖动 → 抢锁 →│←── runDuration ──────────────────→│释放锁│← interval →│再次抢锁
                  │ process → wait → process → wait → │
                  │    ↑                          ↑    │
                  │    └── processingInterval ─────┘    │
```

抢锁失败的 Pod 同样等待一个 `interval` 后重试。`interval` 从本轮结束后开始计算，不会积压定时器 tick；持锁 Pod 释放锁后会完整等待一个冷却间隔，让其他 Pod 获得实际接管机会。`runDuration` 会通过 CancellationToken 通知 Worker 结束当前窗口，因此 Worker 应在批次边界响应取消。

## Worker 编写

```csharp
[TaskWorker]
public class DailyWorker
{
    // Worker 使用 IDbContextFactory 而不是直接注入 ArtDbContext
    private readonly IDbContextFactory<ArtDbContext> _contextFactory;
    private readonly ILogger<DailyWorker> _logger;

    public DailyWorker(
        IDbContextFactory<ArtDbContext> contextFactory,
        ILogger<DailyWorker> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task ClearLogs(CancellationToken cancel)
    {
        using var context = _contextFactory.CreateDbContext();
        // 执行清理逻辑...
    }
}
```

::: warning 重要
`TaskWorker` 是单例，必须保持无状态和线程安全，只能注入单例服务。数据库上下文必须通过 `IDbContextFactory` 创建；直接注入 `ArtDbContext` 会被 DI 校验和架构测试拒绝。
:::

## 任务配置

在 `TaskConfiguration.cs` 中集中配置所有任务：

```csharp
public class TaskConfiguration : ITaskConfigurationProvider
{
    public void ConfigureTasks(ITaskScheduler taskScheduler)
    {
        // 周期性任务
        taskScheduler.AddRecurringTask(...);

        // 长期运行任务
        taskScheduler.AddLongRunningTask(...);
    }
}
```

## 核心特性

### 分布式锁防重

多 Pod 部署时，同一任务只有一个 Pod 执行。使用 Redis 分布式锁实现。

未配置 Redis 时任务以无锁模式运行；如果 Redis 已配置但运行期暂时不可用，任务会跳过当前轮次并在下个周期重试，避免多 Pod 无锁并发执行。

### 初始延迟分散

每个任务根据 `任务名 + PodId` 的 hash 值计算初始延迟（0-30 秒），避免所有任务在重启后同时启动。

### 优雅退出

```
应用停止信号 → 取消任务循环和业务任务 → Worker 在安全边界退出 → Host 等待任务完成
```

调度器将同一个停止 `CancellationToken` 传给循环和业务任务。Worker 应在批次或事务边界响应取消；最长等待时间由 Host 的停止期限控制。
