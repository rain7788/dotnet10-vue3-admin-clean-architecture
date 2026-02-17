# 消息队列

Art Admin 使用 **Redis List** 实现轻量级消息队列（LPUSH 生产 / RPOP 消费），适合中小规模项目。

## 生产者

```csharp
// 发送消息到队列
_cache.LPush(CacheKeys.DemoMessageQueue, "Hello MQ!");

// 发送 JSON 消息
var message = JsonSerializer.Serialize(new { OrderId = 123, Action = "created" });
_cache.LPush(CacheKeys.DemoMessageQueue, message);
```

## 消费者（Worker）

```csharp
[Service(ServiceLifetime.Transient)]
public class DemoMessageQueueWorker
{
    private readonly RedisClient _cache;
    private readonly ILogger<DemoMessageQueueWorker> _logger;

    public DemoMessageQueueWorker(RedisClient cache, ILogger<DemoMessageQueueWorker> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task ProcessQueue(CancellationToken cancel)
    {
        const int maxBatchSize = 20;
        var processed = 0;

        for (var i = 0; i < maxBatchSize; i++)
        {
            var msg = _cache.RPop(CacheKeys.DemoMessageQueue);
            if (string.IsNullOrEmpty(msg))
                break;

            processed++;
            _logger.LogInformation("[DemoQueue] 消费消息: {Message}", msg);
        }

        if (processed > 0)
            _logger.LogInformation("[DemoQueue] 本轮处理完成: {Count}", processed);

        return Task.CompletedTask;
    }
}
```

## 注册为长任务

```csharp
// TaskConfiguration.cs
taskScheduler.AddLongRunningTask(
    _demoMessageQueueWorker.ProcessQueue,
    interval: TimeSpan.FromSeconds(1),           // 外层调度间隔
    processingInterval: TimeSpan.FromMilliseconds(100), // 每次消费间隔
    runDuration: TimeSpan.FromSeconds(30),        // 运行窗口时长
    taskName: "demo.queue.consume"
);
```

运行节奏说明：
- **interval** — 每 1 秒尝试进入消费窗口（会参与分布式锁竞争）
- **runDuration** — 获得锁后最多运行 30 秒，然后释放锁让其他 Pod 接管
- **processingInterval** — 每次消费后等 100ms，避免空队列时 CPU 空转

## 何时选择 Redis MQ？

| 场景 | 推荐 |
| --- | --- |
| 中小规模异步任务 | ✅ Redis MQ |
| 需要高可靠性（消息不能丢） | ❌ 使用 RabbitMQ / Kafka |
| 需要消息确认机制 | ❌ 使用专业 MQ |
| 简单的解耦场景 | ✅ Redis MQ |
