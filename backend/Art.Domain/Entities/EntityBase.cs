using System.ComponentModel.DataAnnotations;

namespace Art.Domain.Entities;

/// <summary>
/// 实体基类（long 主键）
/// 使用雪花ID作为主键
/// </summary>
public abstract class EntityBase
{
    /// <summary>
    /// 主键ID（雪花ID）
    /// </summary>
    [Key]
    public long Id { get; set; } = IdGen.NextId();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 带更新时间的实体基类（long 主键）
/// </summary>
public abstract class EntityBaseWithUpdate : EntityBase
{
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedTime { get; set; }
}

/// <summary>
/// 泛型实体基类
/// 用于需要自定义主键类型的场景（如 string、Guid 等）
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class EntityBase<TKey>
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [Key]
    public TKey Id { get; set; } = default!;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 带更新时间的泛型实体基类
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class EntityBaseWithUpdate<TKey> : EntityBase<TKey>
{
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedTime { get; set; }
}
