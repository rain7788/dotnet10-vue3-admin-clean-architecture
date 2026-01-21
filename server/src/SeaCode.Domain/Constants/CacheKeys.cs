namespace SeaCode.Domain.Constants;

/// <summary>
/// Redis Key 常量管理
/// 统一管理所有缓存 Key，避免 Key 冲突和散落
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// 缓存前缀
    /// </summary>
    private const string Prefix = "st:";

    #region 玩家相关

    /// <summary>
    /// 玩家信息缓存
    /// </summary>
    public static string PlayerInfo(string playerId) => $"{Prefix}player:{playerId}:info";

    /// <summary>
    /// 玩家 Token
    /// </summary>
    public static string PlayerToken(string token) => $"{Prefix}token:{token}";

    /// <summary>
    /// 玩家会话
    /// </summary>
    public static string PlayerSession(string playerId) => $"{Prefix}player:{playerId}:session";

    #endregion

    #region 市场相关

    /// <summary>
    /// 港口市场价格
    /// </summary>
    public static string MarketPrices(string portId) => $"{Prefix}market:{portId}:prices";

    /// <summary>
    /// 市场价格版本号
    /// </summary>
    public static string MarketVersion(string portId) => $"{Prefix}market:{portId}:version";

    /// <summary>
    /// 港口列表
    /// </summary>
    public static string PortList => $"{Prefix}ports:list";

    #endregion

    #region 配置相关

    /// <summary>
    /// 游戏配置
    /// </summary>
    public static string GameConfig => $"{Prefix}config:game";

    /// <summary>
    /// 商品配置
    /// </summary>
    public static string GoodsConfig => $"{Prefix}config:goods";

    /// <summary>
    /// 船只配置
    /// </summary>
    public static string ShipConfig => $"{Prefix}config:ships";

    #endregion

    #region 限流相关

    /// <summary>
    /// 请求限流计数
    /// </summary>
    public static string RateLimit(string ip, string endpoint) => $"{Prefix}rate:{ip}:{endpoint}";

    /// <summary>
    /// 登录失败计数
    /// </summary>
    public static string LoginFailCount(string identifier) => $"{Prefix}login:fail:{identifier}";

    #endregion

    #region 分布式锁

    /// <summary>
    /// 通用锁前缀
    /// </summary>
    public static string Lock(string name) => $"{Prefix}lock:{name}";

    /// <summary>
    /// 交易锁（防止并发交易）
    /// </summary>
    public static string TradeLock(string playerId) => $"{Prefix}lock:trade:{playerId}";

    /// <summary>
    /// 任务执行锁
    /// </summary>
    public static string TaskLock(string taskName) => $"{Prefix}lock:task:{taskName}";

    #endregion

    #region 通配符模式（用于批量删除）

    /// <summary>
    /// 玩家相关所有缓存
    /// </summary>
    public static string PlayerPattern(string playerId) => $"{Prefix}player:{playerId}:*";

    /// <summary>
    /// 市场相关所有缓存
    /// </summary>
    public static string MarketPattern(string portId) => $"{Prefix}market:{portId}:*";

    /// <summary>
    /// 所有配置缓存
    /// </summary>
    public static string ConfigPattern => $"{Prefix}config:*";

    #endregion
}

/// <summary>
/// 缓存过期时间常量
/// </summary>
public static class CacheExpiry
{
    /// <summary>
    /// 短期缓存：1分钟
    /// </summary>
    public static TimeSpan Short => TimeSpan.FromMinutes(1);

    /// <summary>
    /// 中期缓存：5分钟
    /// </summary>
    public static TimeSpan Medium => TimeSpan.FromMinutes(5);

    /// <summary>
    /// 长期缓存：30分钟
    /// </summary>
    public static TimeSpan Long => TimeSpan.FromMinutes(30);

    /// <summary>
    /// 超长期缓存：1小时
    /// </summary>
    public static TimeSpan VeryLong => TimeSpan.FromHours(1);

    /// <summary>
    /// Token 过期时间：7天
    /// </summary>
    public static TimeSpan Token => TimeSpan.FromDays(7);

    /// <summary>
    /// 限流窗口：1分钟
    /// </summary>
    public static TimeSpan RateWindow => TimeSpan.FromMinutes(1);
}
