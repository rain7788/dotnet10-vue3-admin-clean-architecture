# Message Queue

Art Admin implements a lightweight message queue using **Redis List** data structure, ideal for async task decoupling.

## Architecture

```
Producer → LPUSH → Redis List ← BRPOP → Consumer (LongRunningTask)
```

## Producer

```csharp
[Service(ServiceLifetime.Scoped)]
public class OrderService
{
    private readonly RedisClient _redis;

    public async Task CreateOrderAsync(OrderRequest req)
    {
        // 1. Save order
        var order = new Order { ... };
        await _db.Orders.AddAsync(order);
        await _db.SaveChangesAsync();

        // 2. Push message to queue
        var message = JsonSerializer.Serialize(new OrderMessage
        {
            OrderId = order.Id,
            Action = "send_notification"
        });
        await _redis.LPushAsync("queue:order:notify", message);
    }
}
```

## Consumer

```csharp
[Service(ServiceLifetime.Transient)]
public class DemoMessageQueueWorker
{
    private readonly IDbContextFactory<ArtDbContext> _contextFactory;
    private readonly RedisClient _redis;

    public async Task ProcessQueue(CancellationToken cancel)
    {
        var message = await _redis.BRPopAsync(5, "queue:order:notify");
        if (message == null) return;

        using var db = _contextFactory.CreateDbContext();
        var msg = JsonSerializer.Deserialize<OrderMessage>(message);

        // Process the message...
        await SendNotification(msg!.OrderId);
    }
}
```

## Registration

Register the consumer as a long-running task in `TaskConfiguration.cs`:

```csharp
taskScheduler.AddLongRunningTask(
    _worker.ProcessQueue,
    interval: TimeSpan.FromSeconds(1),
    processingInterval: TimeSpan.FromMilliseconds(100),
    runDuration: TimeSpan.FromSeconds(30),
    taskName: "order.notify.queue"
);
```

## Why Redis List?

| Feature | Redis List MQ | RabbitMQ/Kafka |
| --- | --- | --- |
| Dependencies | Just Redis | Need extra service |
| Setup complexity | Zero config | Complex |
| Performance | Excellent for moderate traffic | Better for massive scale |
| Reliability | At-most-once delivery | At-least-once / exactly-once |
| Best for | Internal async tasks | Cross-service, high-volume messaging |

::: tip
For most admin systems, Redis List MQ is sufficient and has zero additional infrastructure cost. Upgrade to RabbitMQ/Kafka only when you need guaranteed delivery or massive scale.
:::
