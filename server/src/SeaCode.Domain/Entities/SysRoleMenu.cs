using System.ComponentModel.DataAnnotations.Schema;

namespace SeaCode.Domain.Entities;

/// <summary>
/// 角色菜单关联表
/// </summary>
[Table("sys_role_menu")]
public class SysRoleMenu
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 菜单ID
    /// </summary>
    public long MenuId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    [ForeignKey(nameof(RoleId))]
    public SysRole SysRole { get; set; } = default!;

    [ForeignKey(nameof(MenuId))]
    public SysMenu SysMenu { get; set; } = default!;
}
