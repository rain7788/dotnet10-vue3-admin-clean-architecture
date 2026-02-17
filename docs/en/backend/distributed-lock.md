# Distributed Lock

Art Admin provides the `RedisLocker` utility for Redis-based distributed locks, supporting auto-renewal (watchdog), timeout, and multiple acquisition modes.

## Use Cases

- Task deduplication across multiple Pods
- Preventing concurrent modification of the same resource
- Ensuring ordered processing of critical operations

## Lock Modes

### Auto-Wait Lock (LockAsync)

Blocks until the lock is acquired:

```csharp
await using var locker = await RedisLocker.LockAsync(
    _redis,
    "order:process:1001",          // Lock key
    TimeSpan.FromSeconds(30),       // Lock timeout
    TimeSpan.FromSeconds(10)        // Max wait time
);

// Critical section — lock auto-released on dispose
await ProcessOrder(orderId);
```

### Try Lock (TryLock)

Returns immediately if lock is unavailable:

```csharp
var locker = RedisLocker.TryLock(
    _redis,
    "report:generate",
    TimeSpan.FromMinutes(5)
);

if (locker != null)
{
    await using (locker)
    {
        await GenerateReport();
    }
}
else
{
    _logger.LogWarning("Report generation already in progress");
}
```

## Watchdog Auto-Renewal

For long-running tasks, the lock's watchdog timer automatically extends expiration to prevent premature release:

```
Lock acquired (TTL: 30s)
├── Business logic executing...
├── Watchdog: TTL renewed at 20s mark → reset to 30s
├── Business logic still running...
├── Watchdog: TTL renewed again
└── Business logic complete → lock released
```

If the process crashes, the watchdog stops and the lock expires naturally after TTL — no deadlock.

## Implementation Details

Uses Lua scripts for atomic operations:

```lua
-- Acquire lock (atomic SET NX with expiry)
if redis.call('SET', KEYS[1], ARGV[1], 'NX', 'PX', ARGV[2]) then
    return 1
end
return 0

-- Release lock (only release if value matches)
if redis.call('GET', KEYS[1]) == ARGV[1] then
    return redis.call('DEL', KEYS[1])
end
return 0
```

Each lock holder has a unique identifier (GUID) to ensure only the owner can release the lock.

## Integration with Task Scheduler

The [Task Scheduler](/en/backend/task-scheduler) uses distributed locks internally for multi-Pod deduplication:

```csharp
taskScheduler.AddRecurringTask(
    worker.DoWork,
    interval: TimeSpan.FromMinutes(5),
    useDistributedLock: true    // Built-in distributed lock
);
```
