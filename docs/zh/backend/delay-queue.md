# 延迟队列

Art Admin 使用 **Redis Sorted Set** 实现延迟队列，通过 Lua 脚本保证原子消费，支持多消费者安全。

## 原理

- 使用 Sorted Set，`score` 为**到期时间的 Unix 时间戳**
- 消费时用 `ZRANGEBYSCORE` 查到期消息 + `ZREM` 原子删除
- Lua 脚本保证原子执行，多消费者不会重复消费

## 生产者

### 单条投递

```csharp
// 投递一条 5 分钟后到期的消息
_cache.DelayQueuePublish(
    CacheKeys.DemoDelayQueue,
    "order:timeout:123",
    delay: TimeSpan.FromMinutes(5)
);
```

### 批量投递

```csharp
_cache.DelayQueuePublishBatch(
    CacheKeys.DemoDelayQueue,
    new[] { "msg1", "msg2", "msg3" },
    delay: TimeSpan.FromSeconds(30)
);
```

### 参数说明

```csharp
public static void DelayQueuePublish(
    this RedisClient client,
    string queueName,   // 队列名称
    string value,       // 消息内容
    TimeSpan delay,     // 延迟时间
    bool overwrite = true // true: 覆盖同值消息的延迟时间
                          // false: 同值消息不覆盖
);
```

## 消费者

```csharp
[Service(ServiceLifetime.Transient)]
public class DemoDelayQueueWorker
{
    private readonly RedisClient _cache;
    private readonly ILogger<DemoDelayQueueWorker> _logger;

    public Task ProcessQueue(CancellationToken cancel)
    {
        // 原子消费到期消息（最多 20 条）
        var messages = _cache.DelayQueueConsume(CacheKeys.DemoDelayQueue, maxCount: 20);

        if (messages.Length == 0) return Task.CompletedTask;

        foreach (var msg in messages)
        {
            _logger.LogInformation("[DemoDelayQueue] 消费到期消息: {Message}", msg);
        }

        return Task.CompletedTask;
    }
}
```

消费端 Lua 脚本：

```lua
local elements = redis.call('ZRANGEBYSCORE', KEYS[1], '-inf', ARGV[1], 'LIMIT', 0, ARGV[2])
if #elements > 0 then
    redis.call('ZREM', KEYS[1], unpack(elements))
end
return elements
```

## 注册为长任务

```csharp
taskScheduler.AddLongRunningTask(
    _demoDelayQueueWorker.ProcessQueue,
    interval: TimeSpan.FromSeconds(1),
    processingInterval: TimeSpan.FromMilliseconds(500),
    runDuration: TimeSpan.FromSeconds(30),
    taskName: "demo.delay-queue.consume"
);
```

## 队列状态查询

```csharp
var status = _cache.DelayQueueStatus(CacheKeys.DemoDelayQueue);
// status.TotalCount    — 队列中总消息数
// status.ReadyCount    — 已到期待消费数
// status.PendingCount  — 未到期等待中数
// status.NextFireAtUtc — 下一条消息到期时间
```

## 典型场景

| 场景 | 延迟时间 |
| --- | --- |
| 订单超时自动取消 | 30 分钟 |
| 短信验证码重发 | 60 秒 |
| 活动定时开启 | 指定时间 |
| 延迟通知推送 | N 分钟 |
