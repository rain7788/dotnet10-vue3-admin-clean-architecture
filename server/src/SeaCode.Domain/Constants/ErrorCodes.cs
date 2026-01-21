namespace SeaCode.Domain.Constants;

/// <summary>
/// 错误码定义
/// </summary>
public static class ErrorCodes
{
    // ============ 通用 0-999 ============
    public const int Success = 0;
    public const int UnknownError = 1;
    public const int InvalidParam = 2;

    // ============ 认证 1000-1999 ============
    public const int Unauthorized = 401;
    public const int TokenExpired = 1001;
    public const int TokenInvalid = 1002;
    public const int LoginFailed = 1003;

    // ============ 权限 403x ============
    public const int Forbidden = 403;

    // ============ 玩家 2000-2999 ============
    public const int PlayerNotFound = 2001;
    public const int InsufficientGold = 2002;
    public const int CargoFull = 2003;
    public const int CargoEmpty = 2004;
    public const int ShipNotFound = 2005;
    public const int LevelNotEnough = 2006;

    // ============ 交易 3000-3999 ============
    public const int NotInPort = 3001;
    public const int GoodsNotFound = 3002;
    public const int StockNotEnough = 3003;
    public const int GoodsNotAccepted = 3004;

    // ============ 航行 4000-4999 ============
    public const int AlreadySailing = 4001;
    public const int NotSailing = 4002;
    public const int PortLocked = 4003;

    // ============ 战斗 5000-5999 ============
    public const int BattleNotFound = 5001;
    public const int ShipDestroyed = 5002;

    // ============ 服务器 500x ============
    public const int ServerError = 500;
    public const int ServiceUnavailable = 503;
}
