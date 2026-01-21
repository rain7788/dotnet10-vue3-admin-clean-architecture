using FreeRedis;
using Yitter.IdGenerator;

namespace SeaCode.Infra.Common;

/// <summary>
/// 雪花ID生成器
/// 配置: WorkerIdBitLength=6 (最大64个Worker), SeqBitLength=10 (每毫秒1024个ID, 约100万/秒)
/// </summary>
public static class SnowflakeIdGenerator
{
    private static bool _initialized = false;
    private static readonly object _lock = new();

    /// <summary>
    /// WorkerId 位长度 (6位 = 最大64个Worker, 范围0-63)
    /// </summary>
    private const byte WorkerIdBitLength = 6;

    /// <summary>
    /// 序列号位长度 (10位 = 每毫秒1024个ID)
    /// </summary>
    private const byte SeqBitLength = 10;

    /// <summary>
    /// 最大 WorkerId (2^6 - 1 = 63)
    /// </summary>
    private const int MaxWorkerId = (1 << WorkerIdBitLength) - 1;

    /// <summary>
    /// 手动初始化（指定 WorkerId）
    /// </summary>
    /// <param name="workerId">WorkerId (0-63)</param>
    public static void Initialize(ushort workerId)
    {
        lock (_lock)
        {
            if (_initialized)
                return;

            if (workerId > MaxWorkerId)
                throw new ArgumentOutOfRangeException(nameof(workerId), $"WorkerId 不能超过 {MaxWorkerId}");

            var options = new IdGeneratorOptions
            {
                WorkerId = workerId,
                WorkerIdBitLength = WorkerIdBitLength,
                SeqBitLength = SeqBitLength
            };
            YitIdHelper.SetIdGenerator(options);
            _initialized = true;
        }
    }

    /// <summary>
    /// 通过 Redis 自动分配 WorkerId 并初始化
    /// </summary>
    /// <param name="redisClient">Redis 客户端</param>
    public static void Initialize(RedisClient redisClient)
    {
        lock (_lock)
        {
            if (_initialized)
                return;

            var workerId = AllocateWorkerId(redisClient);

            var options = new IdGeneratorOptions
            {
                WorkerId = workerId,
                WorkerIdBitLength = WorkerIdBitLength,
                SeqBitLength = SeqBitLength
            };
            YitIdHelper.SetIdGenerator(options);
            _initialized = true;
        }
    }

    /// <summary>
    /// 通过 Redis 分配 WorkerId (循环分配，取模确保不超过最大值)
    /// </summary>
    private static ushort AllocateWorkerId(RedisClient client)
    {
        const string redisKey = "seacode:snowflake:workerid";

        // 使用 Redis INCR 原子递增，然后取模确保在有效范围内
        var counter = client.IncrBy(redisKey, 1);

        // 取模运算: 确保 WorkerId 在 0 ~ MaxWorkerId 范围内
        var workerId = (ushort)(counter % (MaxWorkerId + 1));

        return workerId;
    }

    /// <summary>
    /// 生成雪花ID
    /// </summary>
    /// <returns>long 类型的雪花ID</returns>
    /// <exception cref="InvalidOperationException">未初始化时抛出</exception>
    public static long NextId()
    {
        if (!_initialized)
            throw new InvalidOperationException("SnowflakeIdGenerator 尚未初始化，请先调用 Initialize 方法");

        return YitIdHelper.NextId();
    }

    /// <summary>
    /// 生成雪花ID（字符串格式）
    /// </summary>
    /// <returns>string 类型的雪花ID</returns>
    public static string NextIdString() => NextId().ToString();

    /// <summary>
    /// 是否已初始化
    /// </summary>
    public static bool IsInitialized => _initialized;
}
