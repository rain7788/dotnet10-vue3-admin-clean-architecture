namespace SeaCode.Domain.Constants;

/// <summary>
/// 游戏配置常量
/// </summary>
public static class GameConfig
{
    /// <summary>
    /// 价格刷新间隔（小时）
    /// </summary>
    public const int PriceRefreshHours = 2;

    /// <summary>
    /// 初始金币
    /// </summary>
    public const int InitialGold = 1000;

    /// <summary>
    /// 初始货舱容量
    /// </summary>
    public const int InitialCargoCapacity = 50;

    /// <summary>
    /// 战斗逃跑成功率
    /// </summary>
    public const double FleeSuccessRate = 0.3;

    /// <summary>
    /// 暴击率
    /// </summary>
    public const double CritRate = 0.1;

    /// <summary>
    /// 暴击伤害倍率
    /// </summary>
    public const double CritMultiplier = 1.5;

    /// <summary>
    /// Token 有效期（天）
    /// </summary>
    public const int TokenExpireDays = 30;
}
