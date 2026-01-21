using FreeRedis;

namespace SeaCode.Infra.Cache;

/// <summary>
/// Redis 分布式锁（支持看门狗自动续期）
/// </summary>
public sealed class RedisLocker : IDisposable, IAsyncDisposable
{
    private readonly RedisClient _client;
    private readonly string _lockKey;
    private readonly string _lockValue;
    private readonly TimeSpan _timeout;
    private readonly Timer? _watchdog;
    private volatile bool _disposed;

    private const string UnlockScript = @"
if redis.call('get', KEYS[1]) == ARGV[1] then
    return redis.call('del', KEYS[1])
else
    return 0
end";

    internal RedisLocker(RedisClient client, string lockKey, string lockValue, TimeSpan timeout, bool enableWatchdog)
    {
        _client = client;
        _lockKey = lockKey;
        _lockValue = lockValue;
        _timeout = timeout;

        if (enableWatchdog)
        {
            // 看门狗：每 timeout/3 续期一次，续期时间为 timeout
            var renewInterval = TimeSpan.FromMilliseconds(timeout.TotalMilliseconds / 3);
            _watchdog = new Timer(RenewLock, null, renewInterval, renewInterval);
        }
    }

    private void RenewLock(object? state)
    {
        if (_disposed) return;

        try
        {
            // 仅当锁值匹配时才续期
            var script = @"
if redis.call('get', KEYS[1]) == ARGV[1] then
    return redis.call('pexpire', KEYS[1], ARGV[2])
else
    return 0
end";
            _client.Eval(script, new[] { _lockKey }, _lockValue, (long)_timeout.TotalMilliseconds);
        }
        catch
        {
            // 续期失败，忽略（可能网络问题或锁已释放）
        }
    }

    /// <summary>
    /// 手动释放锁
    /// </summary>
    public bool Unlock()
    {
        if (_disposed) return false;
        _disposed = true;

        _watchdog?.Dispose();

        try
        {
            return _client.Eval(UnlockScript, new[] { _lockKey }, _lockValue)?.ToString() == "1";
        }
        catch
        {
            return false;
        }
    }

    public void Dispose() => Unlock();

    public ValueTask DisposeAsync()
    {
        Unlock();
        return ValueTask.CompletedTask;
    }
}
