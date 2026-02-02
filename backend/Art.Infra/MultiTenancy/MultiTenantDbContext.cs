using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Art.Domain.MultiTenancy;
using Art.Infra.Framework;

namespace Art.Infra.MultiTenancy;

/// <summary>
/// 多租户 DbContext 基类
/// 提供自动的租户过滤和租户 ID 填充功能
/// 使用 IHttpContextAccessor 延迟获取 RequestContext，兼容 DbContextPool
/// </summary>
public abstract class MultiTenantDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private RequestContext? _cachedRequestContext;

    protected MultiTenantDbContext(
        DbContextOptions options,
        IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 获取当前请求上下文（延迟获取，兼容连接池）
    /// </summary>
    private RequestContext? RequestContext =>
        _cachedRequestContext ??= _httpContextAccessor.HttpContext?.RequestServices.GetService<RequestContext>();

    /// <summary>
    /// 当前租户 ID
    /// </summary>
    public string CurrentTenantId => RequestContext?.TenantId ?? "default";

    /// <summary>
    /// 是否忽略租户过滤
    /// </summary>
    public bool IgnoreTenantFilter => RequestContext?.IgnoreTenantFilter ?? false;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 为所有实现 ITenantEntity 的实体自动添加租户过滤器
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = GetType()
                    .GetMethod(nameof(ApplyTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, [modelBuilder]);
            }
        }
    }

    /// <summary>
    /// 为实体应用租户过滤器
    /// </summary>
    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ITenantEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            IgnoreTenantFilter || e.TenantId == CurrentTenantId);
    }

    public override int SaveChanges()
    {
        ApplyTenantIdOnSave();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTenantIdOnSave();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTenantIdOnSave();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTenantIdOnSave();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// 保存时自动填充租户 ID
    /// </summary>
    private void ApplyTenantIdOnSave()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.TenantId))
            {
                entry.Entity.TenantId = CurrentTenantId;
            }
        }
    }
}
