using FreeRedis;

namespace SeaCode.Infra.Cache;

/// <summary>
/// RedisClient 分布式锁扩展方法
/// </summary>
public static class RedisLockExtensions
{
    private const string LockPrefix = "lock:";

    /// <summary>
    /// 获取分布式锁（等待直到超时）
    /// </summary>
    /// <param name="client">Redis 客户端</param>
    /// <param name="key">锁名称（自动添加 lock: 前缀）</param>
    /// <param name="timeout">锁超时时间</param>
    /// <param name="waitTimeout">等待获取锁的超时时间，默认等于锁超时时间</param>
    /// <param name="retryInterval">重试间隔，默认 50ms</param>
    /// <param name="enableWatchdog">是否启用看门狗自动续期，默认 true</param>
    /// <returns>锁对象，获取失败返回 null</returns>
    public static async Task<RedisLocker?> LockAsync(
        this RedisClient client,
        string key,
        TimeSpan timeout,
        TimeSpan? waitTimeout = null,
        int retryInterval = 50,
        bool enableWatchdog = true)
    {
        var lockKey = LockPrefix + key;
        var lockValue = Guid.NewGuid().ToString("N");
        var wait = waitTimeout ?? timeout;
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < wait)
        {
            if (client.SetNx(lockKey, lockValue, timeout))
            {
                return new RedisLocker(client, lockKey, lockValue, timeout, enableWatchdog);
            }

            await Task.Delay(retryInterval);
        }

        return null;
    }

    /// <summary>
    /// 尝试获取分布式锁（立即返回）
    /// </summary>
    /// <param name="client">Redis 客户端</param>
    /// <param name="key">锁名称（自动添加 lock: 前缀）</param>
    /// <param name="timeout">锁超时时间</param>
    /// <param name="enableWatchdog">是否启用看门狗自动续期，默认 false</param>
    /// <returns>锁对象，获取失败返回 null</returns>
    public static RedisLocker? TryLock(
        this RedisClient client,
        string key,
        TimeSpan timeout,
        bool enableWatchdog = false)
    {
        var lockKey = LockPrefix + key;
        var lockValue = Guid.NewGuid().ToString("N");

        if (client.SetNx(lockKey, lockValue, timeout))
        {
            return new RedisLocker(client, lockKey, lockValue, timeout, enableWatchdog);
        }

        return null;
    }

    /// <summary>
    /// 尝试获取分布式锁（立即返回，简化参数）
    /// </summary>
    /// <param name="client">Redis 客户端</param>
    /// <param name="key">锁名称（自动添加 lock: 前缀）</param>
    /// <param name="timeoutSeconds">锁超时秒数，默认 30 秒</param>
    /// <returns>锁对象，获取失败返回 null</returns>
    public static RedisLocker? TryLock(this RedisClient client, string key, int timeoutSeconds = 30)
        => client.TryLock(key, TimeSpan.FromSeconds(timeoutSeconds));
}
