using Yitter.IdGenerator;

namespace SeaCode.Domain;

/// <summary>
/// 雪花ID生成器（轻量版，用于 Domain 层）
/// 实际初始化在 Infra 层的 SnowflakeIdGenerator 中完成
/// </summary>
public static class IdGen
{
    /// <summary>
    /// 生成雪花ID
    /// </summary>
    /// <returns>long 类型的雪花ID</returns>
    public static long NextId() => YitIdHelper.NextId();

    /// <summary>
    /// 生成雪花ID（字符串格式）
    /// </summary>
    /// <returns>string 类型的雪花ID</returns>
    public static string NextIdString() => YitIdHelper.NextId().ToString();
}
