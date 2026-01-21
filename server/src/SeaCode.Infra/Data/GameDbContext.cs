using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SeaCode.Domain.Entities;
using SeaCode.Infra.MultiTenancy;

namespace SeaCode.Infra.Data;

/// <summary>
/// 游戏数据库上下文
/// 继承 MultiTenantDbContext 自动支持多租户过滤
/// </summary>
public class GameDbContext : MultiTenantDbContext
{
    public GameDbContext(
        DbContextOptions<GameDbContext> options,
        IHttpContextAccessor httpContextAccessor)
        : base(options, httpContextAccessor)
    {
    }

    #region Token 相关

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public virtual DbSet<TokenRefresh> TokenRefresh { get; set; } = default!;

    /// <summary>
    /// 访问令牌
    /// </summary>
    public virtual DbSet<TokenAccess> TokenAccess { get; set; } = default!;

    #endregion

    #region 系统管理

    /// <summary>
    /// 系统用户
    /// </summary>
    public virtual DbSet<SysUser> SysUser { get; set; } = default!;

    /// <summary>
    /// 系统角色
    /// </summary>
    public virtual DbSet<SysRole> SysRole { get; set; } = default!;

    /// <summary>
    /// 系统菜单
    /// </summary>
    public virtual DbSet<SysMenu> SysMenu { get; set; } = default!;

    /// <summary>
    /// 系统权限
    /// </summary>
    public virtual DbSet<SysPermission> SysPermission { get; set; } = default!;

    /// <summary>
    /// 用户角色关联
    /// </summary>
    public virtual DbSet<SysUserRole> SysUserRole { get; set; } = default!;

    /// <summary>
    /// 角色菜单关联
    /// </summary>
    public virtual DbSet<SysRoleMenu> SysRoleMenu { get; set; } = default!;

    /// <summary>
    /// 角色权限关联
    /// </summary>
    public virtual DbSet<SysRolePermission> SysRolePermission { get; set; } = default!;

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 用户角色关联（复合主键）
        modelBuilder.Entity<SysUserRole>()
            .HasKey(x => new { x.UserId, x.RoleId });

        // 角色菜单关联（复合主键）
        modelBuilder.Entity<SysRoleMenu>()
            .HasKey(x => new { x.RoleId, x.MenuId });

        // 角色权限关联（复合主键）
        modelBuilder.Entity<SysRolePermission>()
            .HasKey(x => new { x.RoleId, x.PermissionId });
    }
}
