using MySqlConnector;

namespace SeaCode.Infra.Logging;

/// <summary>
/// 日志分表管理器 - 按天分表
/// 表命名格式: logs_YYYYMMDD
/// </summary>
public class DailyLogTableManager
{
    private readonly string _connectionString;
    private static readonly HashSet<string> _existingTables = new();
    private static readonly object _lock = new();

    public DailyLogTableManager(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// 获取指定日期的日志表名
    /// </summary>
    public static string GetTableName(DateTime date)
    {
        return $"logs_{date:yyyyMMdd}";
    }

    /// <summary>
    /// 获取指定日期范围内的所有表名（按日期降序）
    /// </summary>
    public static List<string> GetTableNames(DateTime startDate, DateTime endDate)
    {
        var tables = new List<string>();
        for (var date = endDate.Date; date >= startDate.Date; date = date.AddDays(-1))
        {
            tables.Add(GetTableName(date));
        }
        return tables;
    }

    /// <summary>
    /// 获取最近N天的表名列表
    /// </summary>
    public static List<string> GetRecentTableNames(int days)
    {
        return GetTableNames(DateTime.Today.AddDays(-days + 1), DateTime.Today);
    }

    /// <summary>
    /// 确保指定日期的日志表存在（带缓存）
    /// </summary>
    public async Task EnsureTableExistsAsync(DateTime date)
    {
        var tableName = GetTableName(date);

        lock (_lock)
        {
            if (_existingTables.Contains(tableName))
                return;
        }

        await CreateTableIfNotExistsAsync(tableName);

        lock (_lock)
        {
            _existingTables.Add(tableName);
        }
    }

    /// <summary>
    /// 创建日志表（如果不存在）
    /// </summary>
    private async Task CreateTableIfNotExistsAsync(string tableName)
    {
        var createTableSql = GetCreateTableSql(tableName);

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 获取创建表的 SQL
    /// 优化的表结构：
    /// - 提取常用查询字段作为独立列
    /// - 添加适当的索引
    /// - 使用更高效的数据类型
    /// </summary>
    private static string GetCreateTableSql(string tableName)
    {
        return $@"
CREATE TABLE IF NOT EXISTS `{tableName}` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `timestamp` VARCHAR(100) NULL,
    `level` VARCHAR(20) NULL,
    `template` TEXT NULL,
    `message` LONGTEXT NULL,
    `exception` LONGTEXT NULL,
    `properties` LONGTEXT NULL,
    `_ts` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- 提取的常用查询字段（优化查询性能）
    `request_path` VARCHAR(500) NULL,
    `request_method` VARCHAR(10) NULL,
    `status_code` INT NULL,
    `request_id` VARCHAR(100) NULL,
    `ip_address` VARCHAR(50) NULL,
    `user_id` VARCHAR(100) NULL,
    `user_name` VARCHAR(200) NULL,
    `elapsed` DECIMAL(10,2) NULL,
    
    -- 请求/响应内容（限制大小，超过 10KB 截断）
    `request` TEXT NULL,
    `response` TEXT NULL,
    
    -- 索引优化
    INDEX `idx_ts` (`_ts`),
    INDEX `idx_level` (`level`),
    INDEX `idx_request_path` (`request_path`(100)),
    INDEX `idx_user_id` (`user_id`),
    INDEX `idx_ip_address` (`ip_address`),
    INDEX `idx_status_code` (`status_code`),
    INDEX `idx_level_ts` (`level`, `_ts`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
";
    }

    /// <summary>
    /// 删除指定日期的日志表
    /// </summary>
    public async Task DropTableAsync(DateTime date)
    {
        var tableName = GetTableName(date);
        await DropTableAsync(tableName);
    }

    /// <summary>
    /// 删除指定表名的日志表
    /// </summary>
    public async Task DropTableAsync(string tableName)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = $"DROP TABLE IF EXISTS `{tableName}`";
        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();

        lock (_lock)
        {
            _existingTables.Remove(tableName);
        }
    }

    /// <summary>
    /// 删除指定天数之前的所有日志表
    /// </summary>
    public async Task DropOldTablesAsync(int keepDays)
    {
        var cutoffDate = DateTime.Today.AddDays(-keepDays);
        var existingTableNames = await GetAllLogTableNamesAsync();

        foreach (var tableName in existingTableNames)
        {
            // 解析表名中的日期
            if (TryParseTableDate(tableName, out var tableDate) && tableDate < cutoffDate)
            {
                await DropTableAsync(tableName);
            }
        }
    }

    /// <summary>
    /// 获取数据库中所有日志分表的名称
    /// </summary>
    public async Task<List<string>> GetAllLogTableNamesAsync()
    {
        var tables = new List<string>();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SHOW TABLES LIKE 'logs_%'";
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var tableName = reader.GetString(0);
            // 只包含日期格式的表名
            if (TryParseTableDate(tableName, out _))
            {
                tables.Add(tableName);
            }
        }

        return tables;
    }

    /// <summary>
    /// 获取实际存在的表名（在指定范围内）
    /// </summary>
    public async Task<List<string>> GetExistingTableNamesAsync(DateTime startDate, DateTime endDate)
    {
        var allTables = await GetAllLogTableNamesAsync();
        var tableSet = new HashSet<string>(allTables);

        var result = new List<string>();
        for (var date = endDate.Date; date >= startDate.Date; date = date.AddDays(-1))
        {
            var tableName = GetTableName(date);
            if (tableSet.Contains(tableName))
            {
                result.Add(tableName);
            }
        }

        return result;
    }

    /// <summary>
    /// 尝试从表名解析日期
    /// </summary>
    private static bool TryParseTableDate(string tableName, out DateTime date)
    {
        date = default;
        if (!tableName.StartsWith("logs_") || tableName.Length != 13) // logs_YYYYMMDD
            return false;

        var dateStr = tableName.Substring(5);
        return DateTime.TryParseExact(dateStr, "yyyyMMdd", null,
            System.Globalization.DateTimeStyles.None, out date);
    }
}
