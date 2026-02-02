using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Art.Domain.Enums;

namespace Art.Domain.Entities;

/// <summary>
/// 刷新令牌
/// </summary>
[Table("token_refresh")]
public class TokenRefresh
{
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 用户 ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Token 类型
    /// </summary>
    public TokenType Type { get; set; }

    /// <summary>
    /// 客户端类型
    /// </summary>
    public ClientType? Client { get; set; }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string Token { get; set; } = default!;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime ExpirationTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.Now;
}
