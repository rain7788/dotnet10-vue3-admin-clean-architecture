using Microsoft.Extensions.DependencyInjection;
using RedisClient = FreeRedis.RedisClient;
using Art.Domain.Constants;
using Art.Domain.Exceptions;
using Art.Infra.Cache;
using Art.Infra.Framework;

namespace Art.Core.Services.Admin.Demo;

/// <summary>
/// Demo：Redis 延迟消息队列（Sorted Set，score = 到期时间戳）
/// </summary>
[Service(ServiceLifetime.Scoped)]
public class DemoDelayQueueService
{
    private readonly RedisClient _cache;

    public DemoDelayQueueService(RedisClient cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// 投递单条延迟消息
    /// </summary>
    public DemoDelayEnqueueResponse Enqueue(DemoDelayEnqueueRequest request)
    {
        var message = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(message))
            throw new BadRequestException("消息内容不能为空");

        var delaySeconds = Math.Clamp(request.DelaySeconds ?? 10, 1, 300);

        CheckRateLimit();

        _cache.DelayQueuePublish(
            CacheKeys.DemoDelayQueue,
            message,
            TimeSpan.FromSeconds(delaySeconds),
            overwrite: request.Overwrite ?? true);

        return new DemoDelayEnqueueResponse
        {
            Message = message,
            DelaySeconds = delaySeconds,
            EstimatedFireAt = DateTime.UtcNow.AddSeconds(delaySeconds)
        };
    }

    /// <summary>
    /// 批量投递延迟消息
    /// </summary>
    public DemoDelayBatchEnqueueResponse BatchEnqueue(DemoDelayBatchEnqueueRequest request)
    {
        if (request.Messages == null || request.Messages.Count == 0)
            throw new BadRequestException("消息列表不能为空");

        if (request.Messages.Count > 100)
            throw new BadRequestException("单次最多投递 100 条消息");

        var delaySeconds = Math.Clamp(request.DelaySeconds ?? 10, 1, 300);
        var validMessages = request.Messages
            .Select(m => m?.Trim())
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .ToArray();

        if (validMessages.Length == 0)
            throw new BadRequestException("消息内容不能全部为空");

        CheckRateLimit();

        _cache.DelayQueuePublishBatch(
            CacheKeys.DemoDelayQueue,
            validMessages!,
            TimeSpan.FromSeconds(delaySeconds),
            overwrite: request.Overwrite ?? true);

        return new DemoDelayBatchEnqueueResponse
        {
            Count = validMessages.Length,
            DelaySeconds = delaySeconds,
            EstimatedFireAt = DateTime.UtcNow.AddSeconds(delaySeconds)
        };
    }

    /// <summary>
    /// 查看队列状态
    /// </summary>
    public DemoDelayQueueStatusResponse GetStatus()
    {
        var info = _cache.DelayQueueStatus(CacheKeys.DemoDelayQueue);
        return new DemoDelayQueueStatusResponse
        {
            TotalCount = info.TotalCount,
            ReadyCount = info.ReadyCount,
            PendingCount = info.PendingCount,
            NextFireAtUtc = info.NextFireAtUtc
        };
    }

    /// <summary>
    /// 查看队列中的消息（不消费，仅预览）
    /// </summary>
    public List<DemoDelayMessagePreview> Preview(int count = 20)
    {
        count = Math.Clamp(count, 1, 50);
        var members = _cache.ZRangeByScoreWithScores(CacheKeys.DemoDelayQueue, decimal.MinValue, decimal.MaxValue, 0, count);
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        return members.Select(m => new DemoDelayMessagePreview
        {
            Message = m.member,
            FireAtUtc = DateTimeOffset.FromUnixTimeMilliseconds((long)m.score).UtcDateTime,
            IsReady = (long)m.score <= nowMs,
            RemainingSeconds = Math.Max(0, ((long)m.score - nowMs) / 1000.0)
        }).ToList();
    }

    /// <summary>
    /// 限流：每 5 秒最多 100 次写入
    /// </summary>
    private void CheckRateLimit()
    {
        var key = CacheKeys.DemoDelayQueueRateLimit;
        var count = _cache.Incr(key);
        if (count == 1) _cache.Expire(key, 5);
        if (count > 100) throw new BadRequestException("操作太频繁，请稍后再试");
    }
}

// ==================== Request / Response DTOs ====================

public class DemoDelayEnqueueRequest
{
    public string? Message { get; set; }
    /// <summary>延迟秒数（1-300，默认10）</summary>
    public int? DelaySeconds { get; set; }
    /// <summary>同内容消息是否覆盖延迟时间（默认 true）</summary>
    public bool? Overwrite { get; set; }
}

public class DemoDelayBatchEnqueueRequest
{
    public List<string> Messages { get; set; } = [];
    /// <summary>统一延迟秒数（1-300，默认10）</summary>
    public int? DelaySeconds { get; set; }
    /// <summary>同内容消息是否覆盖延迟时间（默认 true）</summary>
    public bool? Overwrite { get; set; }
}

public class DemoDelayEnqueueResponse
{
    public string Message { get; set; } = "";
    public int DelaySeconds { get; set; }
    public DateTime EstimatedFireAt { get; set; }
}

public class DemoDelayBatchEnqueueResponse
{
    public int Count { get; set; }
    public int DelaySeconds { get; set; }
    public DateTime EstimatedFireAt { get; set; }
}

public class DemoDelayQueueStatusResponse
{
    public long TotalCount { get; set; }
    public long ReadyCount { get; set; }
    public long PendingCount { get; set; }
    public DateTime? NextFireAtUtc { get; set; }
}

public class DemoDelayMessagePreview
{
    public string Message { get; set; } = "";
    public DateTime FireAtUtc { get; set; }
    public bool IsReady { get; set; }
    public double RemainingSeconds { get; set; }
}
