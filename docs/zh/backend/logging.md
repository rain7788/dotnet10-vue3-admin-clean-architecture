# 日志系统

Art Admin 使用 **Serilog** 日志框架，自研 **按天分表 MySQL Sink**，支持批量写入与自动建表。

## 架构一览

```
应用日志 → Serilog Pipeline → DailyMySqlSink → app_log_yyyyMMdd 表
HTTP 请求日志 → RequestResponseLoggingMiddleware → request_log_yyyyMMdd 表
```

## 按天分表

每天的日志写入独立的表（如 `app_log_20250315`），好处是：

- **查询快** — 只扫当天分表，不受历史数据影响
- **清理简单** — 直接 `DROP TABLE` 老表，无需 DELETE
- **运维友好** — 单表体积可控

### 自动建表

```csharp
// DailyLogTableManager 自动管理每日分表
public class DailyLogTableManager
{
    // 缓存已创建的表名，避免重复建表
    private readonly HashSet<string> _createdTables = [];

    public async Task EnsureTableAsync(string dateStr)
    {
        var tableName = $"app_log_{dateStr}";
        if (_createdTables.Contains(tableName)) return;

        // CREATE TABLE IF NOT EXISTS ...
        await CreateTableAsync(tableName);
        _createdTables.Add(tableName);
    }
}
```

## DailyMySqlSink

实现 Serilog `IBatchedLogEventSink`，批量写入日志：

```csharp
public class DailyMySqlSink : IBatchedLogEventSink
{
    public async Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        // 按日期分组
        var groups = batch.GroupBy(e => e.Timestamp.ToString("yyyyMMdd"));

        foreach (var group in groups)
        {
            await _tableManager.EnsureTableAsync(group.Key);
            await BulkInsertAsync($"app_log_{group.Key}", group);
        }
    }
}
```

### 批量写入配置

```csharp
// Program.cs 中的 Serilog 配置
.WriteTo.Sink(
    new DailyMySqlSink(connectionString, tableManager),
    restrictedToMinimumLevel: LogEventLevel.Information,
    batchedSinkConfiguration: batchedSinkConfig =>
    {
        batchedSinkConfig.BatchSizeLimit = 50;       // 每批最多 50 条
        batchedSinkConfig.Period = TimeSpan.FromSeconds(2); // 每 2 秒写入一次
    }
)
```

## 请求日志中间件

`RequestResponseLoggingMiddleware` 记录 HTTP 请求/响应详情，支持配置排除路径：

```csharp
// 排除 Swagger / 健康检查等接口
app.UseMiddleware<RequestResponseLoggingMiddleware>(new RequestResponseLoggingOptions
{
    ExcludePaths = ["/swagger", "/health", "/favicon.ico"]
});
```

记录内容包括：
- 请求方法、路径、Query、Body
- 响应状态码、耗时
- 客户端 IP、User-Agent
- 用户 ID（如已认证）

## 日志清理

配合 [定时任务](/zh/backend/task-scheduler) 自动清理过期日志表：

```csharp
// DailyWorker 中的日志清理
public async Task ClearLogs(CancellationToken cancel)
{
    using var context = _contextFactory.CreateDbContext();
    var cutoffDate = DateTime.Now.AddDays(-30); // 保留 30 天

    // DROP 30 天前的分表
    // DROP TABLE IF EXISTS app_log_20250215;
}
```

## 日志级别配置

```json
// appsettings.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

## 切换到 Elasticsearch

如果日志量极大（日均百万+），建议从 MySQL 分表切换到 Elasticsearch：

- 安装 `Serilog.Sinks.Elasticsearch`
- 替换 Sink 配置，结构化日志天然适合 ES
- Kibana 面板可视化

::: tip
中小规模场景，MySQL 按天分表已足够高效且运维简单。只有当需要全文检索或跨天复杂聚合时，才需要引入 ES。
:::
