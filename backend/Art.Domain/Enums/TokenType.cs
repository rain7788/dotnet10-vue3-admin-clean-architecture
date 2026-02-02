namespace Art.Domain.Enums;

/// <summary>
/// Token 类型（鉴权类型）
/// </summary>
public enum TokenType
{
    /// <summary>
    /// 无需鉴权
    /// </summary>
    无,

    /// <summary>
    /// App端
    /// </summary>
    玩家端,

    /// <summary>
    /// 管理端
    /// </summary>
    平台端 = 9,
}

/// <summary>
/// 客户端类型
/// </summary>
public enum ClientType
{
    小程序,
    H5,
    App,
    未知 = 99
}
