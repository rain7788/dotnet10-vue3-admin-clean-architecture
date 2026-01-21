using System.ComponentModel.DataAnnotations.Schema;

namespace SeaCode.Domain.Entities;

/// <summary>
/// 角色权限关联表
/// </summary>
[Table("sys_role_permission")]
public class SysRolePermission
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    public long PermissionId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    [ForeignKey(nameof(RoleId))]
    public SysRole SysRole { get; set; } = default!;

    [ForeignKey(nameof(PermissionId))]
    public SysPermission SysPermission { get; set; } = default!;
}
