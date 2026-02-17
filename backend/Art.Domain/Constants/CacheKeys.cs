namespace Art.Domain.Constants;

/// <summary>
/// Redis Key 常量管理
/// </summary>
public static class CacheKeys
{
    private const string Prefix = "sc:";

    /// <summary>
    /// 用户 Token 缓存
    /// </summary>
    public static string UserToken(string tokenHash) => $"{Prefix}ut:{tokenHash}";

    /// <summary>
    /// 登录失败计数
    /// </summary>
    public static string LoginFailCount(string identifier) => $"{Prefix}login:fail:{identifier}";

    /// <summary>
    /// Demo：消息队列（Redis List）
    /// </summary>
    public static string DemoMessageQueue => $"{Prefix}demo:mq";
}
