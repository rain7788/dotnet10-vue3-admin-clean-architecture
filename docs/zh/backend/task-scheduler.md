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
    interval: TimeSpan.FromSeconds(1),         // 外层调度间隔
    processingInterval: TimeSpan.FromMilliseconds(100), // 每次处理后的间隔
    runDuration: TimeSpan.FromSeconds(30),     // 运行窗口时长
    taskName: "demo.queue.consume"
);
```

### 两层节奏

```
外层                        运行窗口内
│←── interval ──→│←── runDuration ──────────────────→│
                  │                                    │
                  │ process → wait → process → wait → │释放锁
                  │    ↑                          ↑    │
                  │    └── processingInterval ─────┘    │
```

## Worker 编写

```csharp
[Service(ServiceLifetime.Transient)]
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
Worker 中 **必须使用 `IDbContextFactory`** 创建 DbContext，因为 Worker 不在 HTTP 请求作用域内，直接注入 `ArtDbContext` 会报错。
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

### 初始延迟分散

每个任务根据 `任务名 + PodId` 的 hash 值计算初始延迟（0-30 秒），避免所有任务在重启后同时启动。

### 优雅退出

```
应用停止信号 → 取消任务循环 → 等待当前执行完成（最多 10 秒） → 强制退出
```

双层 CancellationToken 机制：
- **循环层** — 使用 CancellationToken 控制是否继续下一轮
- **业务层** — 传入 `CancellationToken.None`，确保 HTTP/DB 操作完整执行不被中断
