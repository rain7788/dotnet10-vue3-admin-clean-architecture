using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Art.Domain.Enums;

namespace Art.Domain.Entities;

/// <summary>
/// 系统用户表（后台管理员）
/// </summary>
[Table("sys_user")]
public class SysUser : EntityBase
{
    /// <summary>
    /// 用户名
    /// </summary>
    [MaxLength(50)]
    public string Username { get; set; } = default!;

    /// <summary>
    /// 密码（加密存储）
    /// </summary>
    [MaxLength(1024)]
    public string Password { get; set; } = default!;

    /// <summary>
    /// 真实姓名
    /// </summary>
    [MaxLength(50)]
    public string? RealName { get; set; }

    /// <summary>
    /// 是否超级管理员
    /// </summary>
    public bool IsSuper { get; set; } = false;

    /// <summary>
    /// 手机号
    /// </summary>
    [MaxLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    [MaxLength(200)]
    public string? Avatar { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public ActiveStatus Status { get; set; } = ActiveStatus.正常;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginTime { get; set; }

    /// <summary>
    /// 最近活跃时间（由中间件在请求鉴权时刷新）
    /// </summary>
    public DateTime? LastActiveTime { get; set; }
}
