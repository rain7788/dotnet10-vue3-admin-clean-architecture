using System.ComponentModel.DataAnnotations;

namespace SeaCode.Domain.Models.Admin;

#region 日志管理相关模型

/// <summary>
/// 日志查询请求
/// </summary>
public class SysLogListRequest
{
    /// <summary>
    /// 查询日期（必填，只能查询单天的数据）
    /// 格式：yyyy-MM-dd
    /// </summary>
    [Required(ErrorMessage = "查询日期不能为空")]
    public DateTime QueryDate { get; set; } = DateTime.Today;

    /// <summary>
    /// 日志级别 (Information, Warning, Error, Debug, Fatal)
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// 开始时间（当天内的时间筛选）
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间（当天内的时间筛选）
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 关键字搜索（仅搜索 message 字段）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 请求路径（包含匹配）
    /// </summary>
    public string? RequestPath { get; set; }

    /// <summary>
    /// 用户ID（精确匹配）
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// IP地址（精确匹配）
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 状态码（0 表示查询 NULL）
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// 页码（从 1 开始）
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 跳过的记录数
    /// </summary>
    public int Skip => (PageIndex - 1) * PageSize;
}

/// <summary>
/// 日志列表响应
/// </summary>
public class SysLogListResponse : PagedResponse
{
    /// <summary>
    /// 日志列表数据
    /// </summary>
    public List<SysLogListItem> Data { get; set; } = new();
}

/// <summary>
/// 日志列表项
/// </summary>
public class SysLogListItem
{
    /// <summary>
    /// 日志ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 日志级别
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// 日志消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 异常信息
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// 请求路径
    /// </summary>
    public string? RequestPath { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    public string? RequestMethod { get; set; }

    /// <summary>
    /// 状态码
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// 请求ID
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// 用户Id
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 耗时(毫秒)
    /// </summary>
    public decimal? Elapsed { get; set; }

    /// <summary>
    /// 请求参数
    /// </summary>
    public string? Request { get; set; }

    /// <summary>
    /// 响应内容
    /// </summary>
    public string? Response { get; set; }
}

#endregion
