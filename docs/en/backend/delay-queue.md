# Delay Queue

Art Admin implements delay queues using **Redis Sorted Set**, enabling scheduled task execution at specified future times.

## Architecture

```
Producer → ZADD (score=timestamp) → Redis Sorted Set → Consumer polls → Execute when due
```

## Core Concept

Messages are added with a score equal to the target execution timestamp. The consumer polls for items whose score ≤ current time.

## Producer

```csharp
[Service(ServiceLifetime.Scoped)]
public class DelayQueueService
{
    private readonly RedisClient _redis;
    private const string QueueKey = "delay_queue:demo";

    // Publish a delayed message
    public async Task PublishAsync(object message, TimeSpan delay)
    {
        var executeAt = DateTimeOffset.UtcNow.Add(delay).ToUnixTimeSeconds();
        var json = JsonSerializer.Serialize(message);

        // Score = future execution timestamp
        await _redis.ZAddAsync(QueueKey, (decimal)executeAt, json);
    }

    // Example: send reminder 30 minutes later
    public async Task ScheduleReminder(long orderId)
    {
        await PublishAsync(
            new { OrderId = orderId, Action = "remind" },
            TimeSpan.FromMinutes(30)
        );
    }
}
```

## Consumer

```csharp
[Service(ServiceLifetime.Transient)]
public class DemoDelayQueueWorker
{
    private readonly RedisClient _redis;
    private readonly IDbContextFactory<ArtDbContext> _contextFactory;
    private const string QueueKey = "delay_queue:demo";

    public async Task ConsumeAsync(CancellationToken cancel)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Get items with score <= now (due for execution)
        var items = await _redis.ZRangeByScoreAsync(QueueKey, 0, (decimal)now, 0, 10);
        if (items == null || items.Length == 0) return;

        foreach (var item in items)
        {
            // Remove atomically (prevents duplicate processing)
            var removed = await _redis.ZRemAsync(QueueKey, item);
            if (removed <= 0) continue;  // Another consumer got it

            // Process the delayed task
            var message = JsonSerializer.Deserialize<DelayMessage>(item);
            await ProcessMessage(message!);
        }
    }
}
```

## Registration

```csharp
taskScheduler.AddLongRunningTask(
    _delayQueueWorker.ConsumeAsync,
    interval: TimeSpan.FromSeconds(1),
    processingInterval: TimeSpan.FromMilliseconds(500),
    runDuration: TimeSpan.FromSeconds(30),
    taskName: "demo.delay.queue"
);
```

## Use Cases

| Scenario | Delay |
| --- | --- |
| Order auto-cancel if unpaid | 30 minutes |
| Delivery confirmation reminder | 7 days |
| Temporary coupon expiry processing | Custom |
| Scheduled notification dispatch | Exact time |

## Queue Status

Check pending items:

```csharp
// Get pending item count
var count = await _redis.ZCardAsync(QueueKey);

// Get next N items (without removing)
var pending = await _redis.ZRangeByScoreAsync(QueueKey, 0, decimal.MaxValue, 0, 10);
```

## Why Redis Sorted Set?

- **Zero infrastructure** — reuses existing Redis
- **Precision** — second-level accuracy
- **Persistence** — survives app restart (data in Redis)
- **Atomic operations** — `ZREM` prevents duplicate processing
- **Visibility** — easily inspect queue state with `ZCARD` / `ZRANGE`
