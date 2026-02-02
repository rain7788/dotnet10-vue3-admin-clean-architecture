using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Art.Domain.Enums;

namespace Art.Domain.Entities;

/// <summary>
/// 系统权限（按钮权限）表
/// </summary>
[Table("sys_permission")]
public class SysPermission : EntityBase
{
    /// <summary>
    /// 所属菜单ID
    /// </summary>
    public long MenuId { get; set; }

    /// <summary>
    /// 权限名称
    /// </summary>
    [MaxLength(50)]
    public string Name { get; set; } = default!;

    /// <summary>
    /// 权限编码
    /// </summary>
    [MaxLength(50)]
    public string Code { get; set; } = default!;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 权限描述
    /// </summary>
    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public ActiveStatus Status { get; set; } = ActiveStatus.正常;
}
