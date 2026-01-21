using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SeaCode.Domain.Enums;

namespace SeaCode.Domain.Entities.Admin;

/// <summary>
/// 系统菜单表
/// </summary>
[Table("sys_menu")]
public class SysMenu : EntityBaseWithUpdate
{
    /// <summary>
    /// 父级菜单ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 菜单名称
    /// </summary>
    [MaxLength(50)]
    public string Name { get; set; } = default!;

    /// <summary>
    /// 菜单编码
    /// </summary>
    [MaxLength(50)]
    public string Code { get; set; } = default!;

    /// <summary>
    /// 路由路径
    /// </summary>
    [MaxLength(200)]
    public string? Path { get; set; }

    /// <summary>
    /// 组件路径
    /// </summary>
    [MaxLength(200)]
    public string? Component { get; set; }

    /// <summary>
    /// 菜单图标
    /// </summary>
    [MaxLength(100)]
    public string? Icon { get; set; }

    /// <summary>
    /// 排序（越大越靠前）
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 是否外链
    /// </summary>
    public bool IsExternal { get; set; } = false;

    /// <summary>
    /// 是否内嵌iframe
    /// </summary>
    public bool IsIframe { get; set; } = false;

    /// <summary>
    /// 是否全屏页面
    /// </summary>
    public bool IsFullPage { get; set; } = false;

    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool KeepAlive { get; set; } = true;

    /// <summary>
    /// 外链地址
    /// </summary>
    [MaxLength(500)]
    public string? Link { get; set; }

    /// <summary>
    /// 角色权限标识，多个用逗号分隔
    /// </summary>
    [MaxLength(500)]
    public string? Roles { get; set; }

    /// <summary>
    /// 显示徽章
    /// </summary>
    public bool ShowBadge { get; set; } = false;

    /// <summary>
    /// 文本徽章，如 New、Hot
    /// </summary>
    [MaxLength(50)]
    public string? ShowTextBadge { get; set; }

    /// <summary>
    /// 固定标签
    /// </summary>
    public bool FixedTab { get; set; } = false;

    /// <summary>
    /// 标签隐藏
    /// </summary>
    public bool IsHideTab { get; set; } = false;

    /// <summary>
    /// 激活路径，用于详情页高亮父级菜单
    /// </summary>
    [MaxLength(200)]
    public string? ActivePath { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public ActiveStatus Status { get; set; } = ActiveStatus.正常;
}
