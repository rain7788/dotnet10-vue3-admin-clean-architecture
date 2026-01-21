namespace SeaCode.Infra.Common;

/// <summary>
/// Token 辅助类
/// 提供 Token 生成相关功能，供 Admin 和 Game 服务共用
/// </summary>
public static class TokenHelper
{
    /// <summary>
    /// Token 前缀
    /// </summary>
    private const string TokenPrefix = "sc_";

    /// <summary>
    /// 生成 Access Token
    /// </summary>
    /// <returns>带前缀的 Token</returns>
    public static string GenerateAccessToken()
    {
        return $"{TokenPrefix}{Guid.NewGuid():N}";
    }

    /// <summary>
    /// 生成 Refresh Token
    /// </summary>
    /// <returns>Refresh Token</returns>
    public static string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// 检查 Token 格式是否有效
    /// </summary>
    /// <param name="token">Token 字符串</param>
    /// <returns>是否有效</returns>
    public static bool IsValidFormat(string? token)
    {
        return !string.IsNullOrWhiteSpace(token) && token.StartsWith(TokenPrefix);
    }
}
