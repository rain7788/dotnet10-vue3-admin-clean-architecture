using System.ComponentModel.DataAnnotations.Schema;

namespace Art.Domain.Entities;

/// <summary>
/// 用户角色关联表
/// </summary>
[Table("sys_user_role")]
public class SysUserRole
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    [ForeignKey(nameof(UserId))]
    public SysUser SysUser { get; set; } = default!;

    [ForeignKey(nameof(RoleId))]
    public SysRole SysRole { get; set; } = default!;
}
