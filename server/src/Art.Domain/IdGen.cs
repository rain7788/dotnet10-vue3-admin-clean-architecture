using Yitter.IdGenerator;

namespace Art.Domain;

/// <summary>
/// ID 生成器
/// Domain 层的简单封装，避免直接依赖 Yitter.IdGenerator
/// </summary>
public static class IdGen
{
    /// <summary>
    /// 生成下一个雪花 ID
    /// </summary>
    /// <returns>雪花 ID</returns>
    public static long NextId()
    {
        return YitIdHelper.NextId();
    }
}