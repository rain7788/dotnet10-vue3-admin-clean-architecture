using SeaCode.Domain.Enums;

namespace SeaCode.Infra.Framework;

/// <summary>
/// API 元数据（用于路由鉴权配置）
/// </summary>
public class ApiMeta
{
    /// <summary>
    /// 鉴权类型
    /// </summary>
    public TokenType AuthType { get; set; } = TokenType.无;
}
