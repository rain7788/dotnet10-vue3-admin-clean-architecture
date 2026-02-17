# Logging System

Art Admin uses **Serilog** with a custom **daily-partitioned MySQL Sink** for batch log writing and auto table creation.

## Architecture

```
App logs     → Serilog Pipeline → DailyMySqlSink → app_log_yyyyMMdd table
HTTP request → RequestResponseLoggingMiddleware → request_log_yyyyMMdd table
```

## Daily Partitioning

Each day's logs go to a separate table (e.g., `app_log_20250315`):

- **Fast queries** — only scans the current day's table
- **Easy cleanup** — `DROP TABLE` old tables, no DELETE needed
- **Manageable** — individual table size stays small

### Auto Table Creation

```csharp
public class DailyLogTableManager
{
    private readonly HashSet<string> _createdTables = [];

    public async Task EnsureTableAsync(string dateStr)
    {
        var tableName = $"app_log_{dateStr}";
        if (_createdTables.Contains(tableName)) return;

        await CreateTableAsync(tableName);
        _createdTables.Add(tableName);
    }
}
```

## DailyMySqlSink

Implements Serilog `IBatchedLogEventSink` for batch inserts:

```csharp
public class DailyMySqlSink : IBatchedLogEventSink
{
    public async Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        var groups = batch.GroupBy(e => e.Timestamp.ToString("yyyyMMdd"));
        foreach (var group in groups)
        {
            await _tableManager.EnsureTableAsync(group.Key);
            await BulkInsertAsync($"app_log_{group.Key}", group);
        }
    }
}
```

### Batch Configuration

```csharp
.WriteTo.Sink(
    new DailyMySqlSink(connectionString, tableManager),
    restrictedToMinimumLevel: LogEventLevel.Information,
    batchedSinkConfiguration: config =>
    {
        config.BatchSizeLimit = 50;
        config.Period = TimeSpan.FromSeconds(2);
    }
)
```

## Request Logging Middleware

`RequestResponseLoggingMiddleware` records HTTP request/response details:

```csharp
app.UseMiddleware<RequestResponseLoggingMiddleware>(new RequestResponseLoggingOptions
{
    ExcludePaths = ["/swagger", "/health", "/favicon.ico"]
});
```

Captured data: method, path, query, body, status code, duration, client IP, User-Agent, user ID.

## Log Cleanup

Paired with [Task Scheduler](/en/backend/task-scheduler) to auto-drop expired tables:

```csharp
public async Task ClearLogs(CancellationToken cancel)
{
    using var context = _contextFactory.CreateDbContext();
    // DROP TABLE IF EXISTS app_log_XXXXXXXX for tables > 30 days old
}
```

## Switching to Elasticsearch

For high-volume scenarios (millions/day), switch from MySQL partitioning to Elasticsearch:

- Install `Serilog.Sinks.Elasticsearch`
- Replace Sink configuration
- Use Kibana for visualization

::: tip
For small-to-medium scale, daily MySQL partitioning is efficient and operationally simple. Only introduce ES when you need full-text search or complex cross-day aggregations.
:::
