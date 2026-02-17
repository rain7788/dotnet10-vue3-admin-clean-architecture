# 多租户

Art Admin 内置了基于 **共享数据库 + 行级隔离** 的多租户方案。实体实现 `ITenantEntity` 即自动启用租户过滤，零侵入。

## 快速接入

### 1. 实体实现 ITenantEntity

```csharp
public interface ITenantEntity
{
    string TenantId { get; set; }
}

// 示例：订单表需要按租户隔离
[Table("order")]
public class Order : EntityBase, ITenantEntity
{
    public string TenantId { get; set; } = default!;
    public string OrderNo { get; set; } = default!;
    public decimal Amount { get; set; }
}
```

### 2. 自动生效

就这样，不需要其他配置。框架会自动：
- **查询时** — 自动添加 `WHERE tenant_id = 'xxx'` 过滤
- **插入时** — 自动填充 `TenantId` 字段

## 工作原理

### MultiTenantDbContext

`ArtDbContext` 继承自 `MultiTenantDbContext`，核心逻辑：

```csharp
public abstract class MultiTenantDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // 延迟获取 RequestContext，兼容 DbContextPool
    public string CurrentTenantId => RequestContext?.TenantId ?? "default";
    public bool IgnoreTenantFilter => RequestContext?.IgnoreTenantFilter ?? false;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 自动为所有 ITenantEntity 添加全局查询过滤器
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                // 动态调用 ApplyTenantFilter<TEntity>
                // → HasQueryFilter(e => IgnoreTenantFilter || e.TenantId == CurrentTenantId)
            }
        }
    }

    // SaveChanges 时自动填充 TenantId
    private void ApplyTenantIdOnSave()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.TenantId))
                entry.Entity.TenantId = CurrentTenantId;
        }
    }
}
```

### 租户解析

通过 HTTP Header 传递租户信息：

| Header | 说明 |
| --- | --- |
| `X-Tenant-Id` | 指定当前租户 ID |
| `X-Ignore-Tenant-Filter` | 设为 `true` 跳过租户过滤（平台端跨租户查询） |

```bash
# 切换到租户 A
curl -H "X-Tenant-Id: tenant-a" /admin/order/list

# 平台端跨租户查询所有订单
curl -H "X-Ignore-Tenant-Filter: true" /admin/order/list
```

## 单租户模式

如果不需要多租户，不实现 `ITenantEntity` 即可。`TenantId` 默认值为 `"default"`，不影响任何功能。
