namespace Art.Domain.MultiTenancy;

/// <summary>
/// 租户实体接口 - 需要按租户隔离的实体实现此接口
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// 租户 ID
    /// </summary>
    string TenantId { get; set; }
}
