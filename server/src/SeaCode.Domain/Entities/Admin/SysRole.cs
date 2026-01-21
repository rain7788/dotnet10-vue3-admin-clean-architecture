using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SeaCode.Domain.Enums;

namespace SeaCode.Domain.Entities.Admin;

/// <summary>
/// 系统角色表
/// </summary>
[Table("sys_role")]
public class SysRole : EntityBaseWithUpdate
{
    /// <summary>
    /// 角色名称
    /// </summary>
    [MaxLength(50)]
    public string Name { get; set; } = default!;

    /// <summary>
    /// 角色编码
    /// </summary>
    [MaxLength(50)]
    public string Code { get; set; } = default!;

    /// <summary>
    /// 角色描述
    /// </summary>
    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public ActiveStatus Status { get; set; } = ActiveStatus.正常;
}
