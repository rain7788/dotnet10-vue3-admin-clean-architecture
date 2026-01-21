using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using SeaCode.Domain.Exceptions;
using SeaCode.Domain.Models;
using SeaCode.Domain.Models.Admin;
using SeaCode.Infra.Data;
using SeaCode.Infra.Framework;
using SeaCode.Infra.Logging;

namespace SeaCode.Core.Services.Admin.System;

/// <summary>
/// 日志管理服务 - 按天分表查询（单表查询，高效）
/// 表命名格式: logs_YYYYMMDD
/// </summary>
[Service(ServiceLifetime.Scoped)]
public class SysLogService
{
    private readonly RequestContext _user;
    private readonly GameDbContext _context;
    private readonly string _connectionString;

    private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "pwd",
        "newPassword",
        "oldPassword",
        "token",
        "refreshToken"
    };

    public SysLogService(RequestContext user, GameDbContext context)
    {
        _user = user;
        _context = context;
        _connectionString = _context.Database.GetConnectionString() ?? throw new InvalidOperationException("数据库连接字符串不能为空");
    }

    /// <summary>
    /// 获取日志列表 - 单表查询（高效）
    /// </summary>
    public async Task<SysLogListResponse> GetListAsync(SysLogListRequest model)
    {
        var result = new SysLogListResponse
        {
            Page = model.PageIndex,
            PageSize = model.PageSize
        };

        // 获取查询日期对应的表名
        var tableName = DailyLogTableManager.GetTableName(model.QueryDate);

        // 检查表是否存在
        if (!await TableExistsAsync(tableName))
        {
            result.Total = 0;
            result.Data = new List<SysLogListItem>();
            return result;
        }

        // 构建查询条件
        var whereClause = BuildWhereClause(model);
        var parameters = BuildParameters(model);

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // 获取总数
        result.Total = await GetCountAsync(connection, tableName, whereClause, parameters);

        // 获取分页数据
        result.Data = await GetPagedDataAsync(connection, tableName, whereClause, parameters, model.Skip, model.PageSize);

        return result;
    }

    #region 私有方法

    /// <summary>
    /// 检查表是否存在
    /// </summary>
    private async Task<bool> TableExistsAsync(string tableName)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = $"SHOW TABLES LIKE '{tableName}'";
        await using var command = new MySqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    /// <summary>
    /// 构建 WHERE 子句
    /// </summary>
    private static string BuildWhereClause(SysLogListRequest model)
    {
        var conditions = new List<string>();

        // 排除日志查询接口自身的日志
        conditions.Add("(request_path IS NULL OR request_path NOT LIKE '%/admin/system/log/%')");

        if (!string.IsNullOrWhiteSpace(model.Level))
            conditions.Add("level = @level");

        if (model.StartTime.HasValue)
            conditions.Add("_ts >= @startTime");

        if (model.EndTime.HasValue)
            conditions.Add("_ts <= @endTime");

        if (!string.IsNullOrEmpty(model.RequestPath))
            conditions.Add("request_path LIKE @requestPath");

        if (!string.IsNullOrWhiteSpace(model.UserId))
            conditions.Add("user_id = @userId");

        if (!string.IsNullOrWhiteSpace(model.IpAddress))
            conditions.Add("ip_address = @ipAddress");

        // 状态码筛选：0表示查询NULL，其他值正常查询
        if (model.StatusCode.HasValue)
        {
            if (model.StatusCode.Value == 0)
                conditions.Add("status_code IS NULL");
            else
                conditions.Add("status_code = @statusCode");
        }

        // 关键字搜索：仅在 message 字段中模糊匹配
        if (!string.IsNullOrWhiteSpace(model.Keyword))
            conditions.Add("message LIKE @keywordLike");

        return "WHERE " + string.Join(" AND ", conditions);
    }

    /// <summary>
    /// 构建查询参数
    /// </summary>
    private static Dictionary<string, object> BuildParameters(SysLogListRequest model)
    {
        var parameters = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(model.Level))
            parameters["@level"] = model.Level;

        if (model.StartTime.HasValue)
            parameters["@startTime"] = model.StartTime.Value;

        if (model.EndTime.HasValue)
            parameters["@endTime"] = model.EndTime.Value;

        if (!string.IsNullOrEmpty(model.RequestPath))
            parameters["@requestPath"] = $"%{model.RequestPath}%";

        if (!string.IsNullOrWhiteSpace(model.UserId))
            parameters["@userId"] = model.UserId;

        if (!string.IsNullOrWhiteSpace(model.IpAddress))
            parameters["@ipAddress"] = model.IpAddress;

        // 状态码：只有非0值才添加参数
        if (model.StatusCode.HasValue && model.StatusCode.Value != 0)
            parameters["@statusCode"] = model.StatusCode.Value;

        if (!string.IsNullOrWhiteSpace(model.Keyword))
            parameters["@keywordLike"] = $"%{model.Keyword}%";

        return parameters;
    }

    /// <summary>
    /// 获取总数 - 单表查询
    /// </summary>
    private static async Task<int> GetCountAsync(MySqlConnection connection, string tableName, string whereClause, Dictionary<string, object> parameters)
    {
        var sql = $"SELECT COUNT(*) FROM `{tableName}` {whereClause}";

        await using var command = new MySqlCommand(sql, connection);
        foreach (var param in parameters)
            command.Parameters.AddWithValue(param.Key, param.Value);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result ?? 0);
    }

    /// <summary>
    /// 获取分页数据 - 单表查询
    /// </summary>
    private static async Task<List<SysLogListItem>> GetPagedDataAsync(
        MySqlConnection connection,
        string tableName,
        string whereClause,
        Dictionary<string, object> parameters,
        int skip,
        int take)
    {
        var sql = $@"
SELECT id, timestamp, level, message, exception, _ts,
       request_path, request_method, status_code, request_id, 
       ip_address, user_id, user_name, elapsed, request, response
FROM `{tableName}` {whereClause}
ORDER BY _ts DESC
LIMIT {skip}, {take}";

        await using var command = new MySqlCommand(sql, connection);
        foreach (var param in parameters)
            command.Parameters.AddWithValue(param.Key, param.Value);

        var logs = new List<SysLogListItem>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            logs.Add(MapToLogItem(reader));

        return logs;
    }

    /// <summary>
    /// 映射数据库记录到响应对象
    /// </summary>
    private static SysLogListItem MapToLogItem(MySqlDataReader reader)
    {
        var item = new SysLogListItem
        {
            Id = reader.GetInt32("id"),
            Timestamp = reader.IsDBNull(reader.GetOrdinal("_ts")) ? DateTime.MinValue : reader.GetDateTime("_ts"),
            Level = reader.IsDBNull(reader.GetOrdinal("level")) ? "" : reader.GetString("level"),
            Message = reader.IsDBNull(reader.GetOrdinal("message")) ? "" : reader.GetString("message"),
            Exception = reader.IsDBNull(reader.GetOrdinal("exception")) ? null : reader.GetString("exception"),
            RequestPath = reader.IsDBNull(reader.GetOrdinal("request_path")) ? null : reader.GetString("request_path"),
            RequestMethod = reader.IsDBNull(reader.GetOrdinal("request_method")) ? null : reader.GetString("request_method"),
            StatusCode = reader.IsDBNull(reader.GetOrdinal("status_code")) ? null : reader.GetInt32("status_code"),
            RequestId = reader.IsDBNull(reader.GetOrdinal("request_id")) ? null : reader.GetString("request_id"),
            IpAddress = reader.IsDBNull(reader.GetOrdinal("ip_address")) ? null : reader.GetString("ip_address"),
            UserId = reader.IsDBNull(reader.GetOrdinal("user_id")) ? null : reader.GetString("user_id"),
            UserName = reader.IsDBNull(reader.GetOrdinal("user_name")) ? null : reader.GetString("user_name"),
            Elapsed = reader.IsDBNull(reader.GetOrdinal("elapsed")) ? null : reader.GetDecimal("elapsed"),
            Request = FormatJson(DesensitizeJson(reader.IsDBNull(reader.GetOrdinal("request")) ? null : reader.GetString("request"))),
            Response = FormatJson(DesensitizeJson(reader.IsDBNull(reader.GetOrdinal("response")) ? null : reader.GetString("response")))
        };

        return item;
    }

    /// <summary>
    /// 格式化 JSON 字符串
    /// </summary>
    private static string? FormatJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch
        {
            return json;
        }
    }

    /// <summary>
    /// 对 JSON 中的敏感字段进行脱敏
    /// </summary>
    private static string? DesensitizeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        try
        {
            var node = JsonNode.Parse(json);
            if (node == null)
                return json;

            DesensitizeJsonNode(node);
            return node.ToJsonString();
        }
        catch
        {
            return json;
        }
    }

    private static void DesensitizeJsonNode(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                {
                    var keys = new List<string>();
                    foreach (var pair in obj)
                        keys.Add(pair.Key);

                    foreach (var key in keys)
                    {
                        if (!obj.TryGetPropertyValue(key, out var value) || value == null)
                            continue;

                        if (SensitiveFields.Contains(key))
                        {
                            obj[key] = "***";
                            continue;
                        }

                        DesensitizeJsonNode(value);
                    }
                    break;
                }
            case JsonArray arr:
                foreach (var item in arr)
                {
                    if (item != null)
                        DesensitizeJsonNode(item);
                }
                break;
        }
    }

    #endregion
}
