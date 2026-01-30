using Art.Domain.Enums;

namespace Art.Infra.Framework;

/// <summary>
/// 请求上下文（Scoped 生命周期）
/// 存储当前请求的用户信息和租户信息
/// </summary>
public class RequestContext
{
    /// <summary>
    /// 用户 ID（雪花ID）
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 用户 ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 账号（用户名）
    /// </summary>
    public string? Account { get; set; }

    /// <summary>
    /// 客户端类型
    /// </summary>
    public ClientType? ClientType { get; set; }

    /// <summary>
    /// 是否超级管理员
    /// </summary>
    public bool IsSuper { get; set; } = false;

    /// <summary>
    /// 请求 ID（用于链路追踪）
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// 请求 IP
    /// </summary>
    public string? RequestIp { get; set; }

    /// <summary>
    /// User Agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 当前 Token
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// 是否已认证
    /// </summary>
    public bool IsAuthenticated => Id > 0;

    #region 多租户

    /// <summary>
    /// 当前租户 ID（每个请求必须属于一个租户）
    /// 单租户场景默认为 "default"
    /// </summary>
    public string TenantId { get; set; } = "default";

    /// <summary>
    /// 是否忽略租户过滤（用于平台端跨租户查询场景）
    /// 默认 false，启用租户过滤
    /// </summary>
    public bool IgnoreTenantFilter { get; set; } = false;

    #endregion
}
