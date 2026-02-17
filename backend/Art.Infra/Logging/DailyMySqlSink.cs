using MySqlConnector;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System.Text.Json;

namespace Art.Infra.Logging;

/// <summary>
/// 自定义 MySQL Sink - 支持按天分表
/// 日志会写入当天对应的表 logs_YYYYMMDD
/// </summary>
public class DailyMySqlSink : ILogEventSink, IDisposable
{
    private readonly string _connectionString;
    private readonly DailyLogTableManager _tableManager;
    private readonly int _batchSize;
    private readonly TimeSpan _flushInterval;
    private readonly List<LogEvent> _buffer;
    private readonly object _lock = new();
    private readonly Timer _flushTimer;
    private bool _disposed;

    public DailyMySqlSink(
        string connectionString,
        int batchSize = 50,
        TimeSpan? flushInterval = null)
    {
        _connectionString = connectionString;
        _tableManager = new DailyLogTableManager(connectionString);
        _batchSize = batchSize;
        _flushInterval = flushInterval ?? TimeSpan.FromSeconds(2);
        _buffer = new List<LogEvent>();

        // 启动定时刷新
        _flushTimer = new Timer(FlushTimerCallback, null, _flushInterval, _flushInterval);
    }

    public void Emit(LogEvent logEvent)
    {
        if (_disposed) return;

        lock (_lock)
        {
            _buffer.Add(logEvent);

            if (_buffer.Count >= _batchSize)
                FlushBuffer();
        }
    }

    private void FlushTimerCallback(object? state)
    {
        lock (_lock)
        {
            if (_buffer.Count > 0)
                FlushBuffer();
        }
    }

    private void FlushBuffer()
    {
        if (_buffer.Count == 0) return;

        var logsToWrite = _buffer.ToList();
        _buffer.Clear();

        // 异步写入，不阻塞主线程
        Task.Run(async () =>
        {
            try
            {
                await WriteBatchAsync(logsToWrite);
            }
            catch (Exception ex)
            {
                // 日志写入失败不应影响应用程序
                Console.Error.WriteLine($"Failed to write logs to MySQL: {ex.Message}");
            }
        });
    }

    private async Task WriteBatchAsync(List<LogEvent> logs)
    {
        // 按日期分组
        var groupedByDate = logs.GroupBy(l => l.Timestamp.LocalDateTime.Date);

        foreach (var group in groupedByDate)
        {
            var date = group.Key;
            var tableName = DailyLogTableManager.GetTableName(date);

            // 确保表存在
            await _tableManager.EnsureTableExistsAsync(date);

            // 批量插入
            await InsertLogsAsync(tableName, group.ToList());
        }
    }

    private async Task InsertLogsAsync(string tableName, List<LogEvent> logs)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = $@"
INSERT INTO `{tableName}` 
(`timestamp`, `level`, `template`, `message`, `exception`, `properties`, `_ts`,
 `request_path`, `request_method`, `status_code`, `request_id`, `ip_address`, `user_id`, `user_name`, `elapsed`,
 `request`, `response`)
VALUES 
(@timestamp, @level, @template, @message, @exception, @properties, @ts,
 @request_path, @request_method, @status_code, @request_id, @ip_address, @user_id, @user_name, @elapsed,
 @request, @response)";

        foreach (var log in logs)
        {
            try
            {
                await using var command = new MySqlCommand(sql, connection);

                var properties = ExtractProperties(log);

                command.Parameters.AddWithValue("@timestamp", log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fffzzz"));
                command.Parameters.AddWithValue("@level", log.Level.ToString());
                command.Parameters.AddWithValue("@template", log.MessageTemplate.Text ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@message", log.RenderMessage() ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@exception", log.Exception?.ToString() ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@properties", properties.Json ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ts", log.Timestamp.LocalDateTime);

                // 提取的字段
                command.Parameters.AddWithValue("@request_path", properties.RequestPath ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@request_method", properties.RequestMethod ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@status_code", properties.StatusCode.HasValue ? properties.StatusCode.Value : DBNull.Value);
                command.Parameters.AddWithValue("@request_id", properties.RequestId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ip_address", properties.IpAddress ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@user_id", properties.UserId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@user_name", properties.UserName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@elapsed", properties.Elapsed.HasValue ? properties.Elapsed.Value : DBNull.Value);
                command.Parameters.AddWithValue("@request", properties.Request ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@response", properties.Response ?? (object)DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to insert log: {ex.Message}");
            }
        }
    }

    private ExtractedProperties ExtractProperties(LogEvent logEvent)
    {
        var result = new ExtractedProperties();
        var propertiesDict = new Dictionary<string, object?>();

        foreach (var property in logEvent.Properties)
        {
            var value = GetPropertyValue(property.Value);
            propertiesDict[property.Key] = value;

            // 提取常用字段
            switch (property.Key)
            {
                case "RequestPath":
                    result.RequestPath = value?.ToString();
                    break;
                case "RequestMethod":
                    result.RequestMethod = value?.ToString();
                    break;
                case "StatusCode":
                    if (int.TryParse(value?.ToString(), out var statusCode))
                        result.StatusCode = statusCode;
                    break;
                case "RequestId":
                    result.RequestId = value?.ToString();
                    break;
                case "IpAddress":
                    result.IpAddress = value?.ToString();
                    break;
                case "UserId":
                    result.UserId = value?.ToString();
                    break;
                case "UserName":
                    result.UserName = value?.ToString();
                    break;
                case "Elapsed":
                    if (decimal.TryParse(value?.ToString(), out var elapsed))
                        result.Elapsed = elapsed;
                    break;
                case "Request":
                    result.Request = TruncateContent(value?.ToString(), 10 * 1024); // 限制 10KB
                    break;
                case "Response":
                    result.Response = TruncateContent(value?.ToString(), 10 * 1024); // 限制 10KB
                    break;
            }
        }

        result.Json = JsonSerializer.Serialize(propertiesDict);
        return result;
    }

    /// <summary>
    /// 截断内容，超过限制大小则截断并添加提示
    /// </summary>
    private static string? TruncateContent(string? content, int maxLength)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= maxLength)
            return content;

        return content.Substring(0, maxLength) + "... [truncated]";
    }

    private static object? GetPropertyValue(LogEventPropertyValue propertyValue)
    {
        return propertyValue switch
        {
            ScalarValue sv => sv.Value,
            SequenceValue seqv => seqv.Elements.Select(GetPropertyValue).ToArray(),
            StructureValue stv => stv.Properties.ToDictionary(p => p.Name, p => GetPropertyValue(p.Value)),
            DictionaryValue dv => dv.Elements.ToDictionary(kvp => GetPropertyValue(kvp.Key)?.ToString() ?? "", kvp => GetPropertyValue(kvp.Value)),
            _ => propertyValue.ToString()
        };
    }

    private class ExtractedProperties
    {
        public string? Json { get; set; }
        public string? RequestPath { get; set; }
        public string? RequestMethod { get; set; }
        public int? StatusCode { get; set; }
        public string? RequestId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public decimal? Elapsed { get; set; }
        public string? Request { get; set; }
        public string? Response { get; set; }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _flushTimer.Dispose();

        // 最后刷新一次
        lock (_lock)
        {
            if (_buffer.Count > 0)
                FlushBuffer();
        }
    }
}

/// <summary>
/// Serilog 配置扩展
/// </summary>
public static class DailyMySqlSinkExtensions
{
    /// <summary>
    /// 配置按天分表的 MySQL 日志写入
    /// </summary>
    /// <param name="loggerConfiguration">Logger 配置</param>
    /// <param name="connectionString">MySQL 连接字符串</param>
    /// <param name="restrictedToMinimumLevel">最小日志级别</param>
    /// <param name="batchSize">批量写入大小</param>
    /// <param name="flushInterval">刷新间隔</param>
    /// <returns></returns>
    public static LoggerConfiguration DailyMySQL(
        this LoggerSinkConfiguration loggerConfiguration,
        string connectionString,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Information,
        int batchSize = 50,
        TimeSpan? flushInterval = null)
    {
        return loggerConfiguration.Sink(
            new DailyMySqlSink(connectionString, batchSize, flushInterval),
            restrictedToMinimumLevel);
    }
}
