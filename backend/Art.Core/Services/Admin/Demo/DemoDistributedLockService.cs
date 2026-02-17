using Microsoft.Extensions.DependencyInjection;
using RedisClient = FreeRedis.RedisClient;
using Art.Domain.Exceptions;
using Art.Infra.Cache;
using Art.Infra.Framework;

namespace Art.Core.Services.Admin.Demo;

/// <summary>
/// Demo：Redis 分布式锁演示（TryLock 立即返回 + LockAsync 等待获取）
/// </summary>
[Service(ServiceLifetime.Scoped)]
public class DemoDistributedLockService
{
    private readonly RedisClient _cache;
    private const string DemoLockKey = "demo:lock:shared";

    public DemoDistributedLockService(RedisClient cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// 演示 TryLock：立即尝试获取锁，获取成功则持有指定秒数后自动释放
    /// </summary>
    public async Task<DemoLockResultResponse> TryLockDemoAsync(DemoTryLockRequest request)
    {
        var holdSeconds = Math.Clamp(request.HoldSeconds ?? 5, 1, 30);

        using var locker = _cache.TryLock(DemoLockKey, timeoutSeconds: holdSeconds + 5);
        if (locker == null)
        {
            return new DemoLockResultResponse
            {
                Acquired = false,
                Message = "获取锁失败：锁已被其他人持有，TryLock 立即返回",
                LockKey = DemoLockKey,
                HeldForMs = 0
            };
        }

        // 模拟持有锁执行业务
        var sw = global::System.Diagnostics.Stopwatch.StartNew();
        await Task.Delay(TimeSpan.FromSeconds(holdSeconds));
        sw.Stop();

        return new DemoLockResultResponse
        {
            Acquired = true,
            Message = $"获取锁成功，持有 {sw.ElapsedMilliseconds}ms 后释放（using 自动释放）",
            LockKey = DemoLockKey,
            HeldForMs = sw.ElapsedMilliseconds
        };
    }

    /// <summary>
    /// 演示 LockAsync：等待获取锁，超时则失败
    /// </summary>
    public async Task<DemoLockResultResponse> WaitLockDemoAsync(DemoWaitLockRequest request)
    {
        var holdSeconds = Math.Clamp(request.HoldSeconds ?? 3, 1, 15);
        var waitSeconds = Math.Clamp(request.WaitSeconds ?? 10, 1, 30);

        await using var locker = await _cache.LockAsync(
            DemoLockKey,
            timeout: TimeSpan.FromSeconds(holdSeconds + 5),
            waitTimeout: TimeSpan.FromSeconds(waitSeconds),
            retryInterval: 200,
            enableWatchdog: true);

        if (locker == null)
        {
            return new DemoLockResultResponse
            {
                Acquired = false,
                Message = $"获取锁失败：等待 {waitSeconds}s 超时，锁仍被其他人持有",
                LockKey = DemoLockKey,
                HeldForMs = 0
            };
        }

        // 模拟持有锁执行业务
        var sw = global::System.Diagnostics.Stopwatch.StartNew();
        await Task.Delay(TimeSpan.FromSeconds(holdSeconds));
        sw.Stop();

        return new DemoLockResultResponse
        {
            Acquired = true,
            Message = $"获取锁成功（等待后获得），持有 {sw.ElapsedMilliseconds}ms 后释放（await using 自动释放）",
            LockKey = DemoLockKey,
            HeldForMs = sw.ElapsedMilliseconds
        };
    }

    /// <summary>
    /// 查询当前锁状态
    /// </summary>
    public Task<DemoLockStatusResponse> GetLockStatusAsync()
    {
        var fullKey = "lock:" + DemoLockKey;
        var value = _cache.Get(fullKey);
        var ttl = value != null ? _cache.Ttl(fullKey) : -2;

        return Task.FromResult(new DemoLockStatusResponse
        {
            IsLocked = value != null,
            LockKey = DemoLockKey,
            RemainingTtlSeconds = ttl > 0 ? ttl : 0
        });
    }
}

public class DemoTryLockRequest
{
    /// <summary>
    /// 持有锁的秒数（1-30，默认5）
    /// </summary>
    public int? HoldSeconds { get; set; }
}

public class DemoWaitLockRequest
{
    /// <summary>
    /// 持有锁的秒数（1-15，默认3）
    /// </summary>
    public int? HoldSeconds { get; set; }

    /// <summary>
    /// 等待获取锁的超时秒数（1-30，默认10）
    /// </summary>
    public int? WaitSeconds { get; set; }
}

public class DemoLockResultResponse
{
    public bool Acquired { get; set; }
    public string Message { get; set; } = "";
    public string LockKey { get; set; } = "";
    public long HeldForMs { get; set; }
}

public class DemoLockStatusResponse
{
    public bool IsLocked { get; set; }
    public string LockKey { get; set; } = "";
    public long RemainingTtlSeconds { get; set; }
}
