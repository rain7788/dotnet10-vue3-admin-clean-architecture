# Task Scheduler

Art Admin uses a custom task scheduler supporting **recurring tasks** and **long-running tasks**, with built-in distributed lock deduplication.

## Two Task Types

| Type | Method | Use Case |
| --- | --- | --- |
| Recurring | `AddRecurringTask` | Periodic execution (log cleanup, data aggregation) |
| LongRunning | `AddLongRunningTask` | Continuous running (message queue consumer, delay queue) |

## Recurring Tasks

```csharp
taskScheduler.AddRecurringTask(
    _dailyWorker.ClearLogs,
    interval: TimeSpan.FromMinutes(21),  // Check every 21 minutes
    allowedHours: [2, 3],                // Only run at 2-3 AM
    preventDuplicateInterval: TimeSpan.FromHours(12), // No repeats within 12h
    useDistributedLock: true             // Only one Pod executes
);
```

### Parameters

| Parameter | Description |
| --- | --- |
| `interval` | Scheduling interval (how often to check) |
| `allowedHours` | Allowed execution hours (skipped outside range) |
| `preventDuplicateInterval` | Deduplication interval (Redis dedup key) |
| `useDistributedLock` | Enable distributed lock (multi-Pod mutex) |

## Long-Running Tasks

```csharp
taskScheduler.AddLongRunningTask(
    _demoMessageQueueWorker.ProcessQueue,
    interval: TimeSpan.FromSeconds(1),             // Outer scheduling interval
    processingInterval: TimeSpan.FromMilliseconds(100), // Interval within processing window
    runDuration: TimeSpan.FromSeconds(30),          // Processing window duration
    taskName: "demo.queue.consume"
);
```

### Two-Level Rhythm

```
Outer                         Processing Window
│←── interval ──→│←── runDuration ──────────────────→│
                  │                                    │
                  │ process → wait → process → wait → │release lock
                  │    ↑                          ↑    │
                  │    └── processingInterval ─────┘    │
```

## Writing Workers

```csharp
[Service(ServiceLifetime.Transient)]
public class DailyWorker
{
    // Workers use IDbContextFactory, NOT direct ArtDbContext injection
    private readonly IDbContextFactory<ArtDbContext> _contextFactory;

    public DailyWorker(IDbContextFactory<ArtDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task ClearLogs(CancellationToken cancel)
    {
        using var context = _contextFactory.CreateDbContext();
        // Cleanup logic...
    }
}
```

::: warning Important
Workers **must use `IDbContextFactory`** to create DbContext. Workers run outside HTTP request scope — direct `ArtDbContext` injection will throw.
:::

## Task Configuration

All tasks are configured centrally in `TaskConfiguration.cs`:

```csharp
public class TaskConfiguration : ITaskConfigurationProvider
{
    public void ConfigureTasks(ITaskScheduler taskScheduler)
    {
        taskScheduler.AddRecurringTask(...);
        taskScheduler.AddLongRunningTask(...);
    }
}
```

## Core Features

### Distributed Lock Deduplication

In multi-Pod deployments, each task runs on only one Pod. Redis distributed locks handle the coordination.

### Staggered Initial Delay

Each task computes an initial delay (0-30s) from a hash of `taskName + PodId`, preventing all tasks from starting simultaneously after restart.

### Graceful Shutdown

```
Stop signal → Cancel task loop → Wait for current execution (max 10s) → Force exit
```

Dual CancellationToken mechanism:
- **Loop level** — CancellationToken controls whether to continue the next iteration
- **Business level** — Uses `CancellationToken.None` to ensure HTTP/DB operations complete without interruption
