using FreeRedis;

namespace Art.Infra.Cache;

/// <summary>
/// Redis 延迟队列扩展方法（基于 Sorted Set，score = 到期 Unix 时间戳）
/// </summary>
public static class RedisDelayQueueExtensions
{
    /// <summary>
    /// 投递延迟消息（单条）
    /// </summary>
    /// <param name="client">Redis 客户端</param>
    /// <param name="queueName">队列名称</param>
    /// <param name="value">消息内容</param>
    /// <param name="delay">延迟时间</param>
    /// <param name="overwrite">同值消息是否覆盖（true=ZAdd 覆盖分数，false=ZAddNx 不覆盖）</param>
    public static void DelayQueuePublish(
        this RedisClient client,
        string queueName,
        string value,
        TimeSpan delay,
        bool overwrite = true)
    {
        var score = (decimal)DateTimeOffset.UtcNow.Add(delay).ToUnixTimeMilliseconds();
        if (overwrite)
            client.ZAdd(queueName, score, value);
        else
            client.ZAddNx(queueName, score, value);
    }

    /// <summary>
    /// 批量投递延迟消息
    /// </summary>
    /// <param name="client">Redis 客户端</param>
    /// <param name="queueName">队列名称</param>
    /// <param name="values">消息列表</param>
    /// <param name="delay">统一延迟时间</param>
    /// <param name="overwrite">同值消息是否覆盖</param>
    public static void DelayQueuePublishBatch(
        this RedisClient client,
        string queueName,
        string[] values,
        TimeSpan delay,
        bool overwrite = true)
    {
        if (values.Length == 0) return;

        var score = (decimal)DateTimeOffset.UtcNow.Add(delay).ToUnixTimeMilliseconds();
        var members = values.Select(v => new ZMember(v, score)).ToArray();

        if (overwrite)
            client.ZAdd(queueName, members);
        else
            client.ZAddNx(queueName, members);
    }

    /// <summary>
    /// 原子性消费到期消息（Lua 脚本：ZRANGEBYSCORE + ZREM）
    /// </summary>
    /// <param name="client">Redis 客户端</param>
    /// <param name="queueName">队列名称</param>
    /// <param name="maxCount">单次最多消费条数</param>
    /// <returns>到期的消息列表，无消息时返回空数组</returns>
    public static string[] DelayQueueConsume(
        this RedisClient client,
        string queueName,
        int maxCount = 20)
    {
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Lua 保证 ZRANGEBYSCORE + ZREM 原子执行，多消费者安全
        const string script = @"
            local elements = redis.call('ZRANGEBYSCORE', KEYS[1], '-inf', ARGV[1], 'LIMIT', 0, ARGV[2])
            if #elements > 0 then
                redis.call('ZREM', KEYS[1], unpack(elements))
            end
            return elements";

        var result = client.Eval(script, new[] { queueName }, nowMs.ToString(), maxCount.ToString());
        if (result is null) return [];

        if (result is object[] arr)
            return arr.Select(x => x?.ToString() ?? "").Where(x => x.Length > 0).ToArray();

        return [];
    }

    /// <summary>
    /// 查看延迟队列状态
    /// </summary>
    public static DelayQueueInfo DelayQueueStatus(this RedisClient client, string queueName)
    {
        var total = client.ZCard(queueName);
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var ready = client.ZCount(queueName, decimal.MinValue, nowMs);
        var pending = total - ready;

        // 查看最近一条待触发消息的到期时间
        var nearest = client.ZRangeByScoreWithScores(queueName, nowMs, decimal.MaxValue, 0, 1);
        long? nextFireMs = nearest.Length > 0 ? (long)nearest[0].score : null;

        return new DelayQueueInfo
        {
            TotalCount = total,
            ReadyCount = ready,
            PendingCount = pending,
            NextFireAtUtc = nextFireMs.HasValue
                ? DateTimeOffset.FromUnixTimeMilliseconds(nextFireMs.Value).UtcDateTime
                : null
        };
    }

    /// <summary>
    /// 移除指定消息
    /// </summary>
    public static long DelayQueueRemove(this RedisClient client, string queueName, params string[] values)
    {
        return client.ZRem(queueName, values);
    }
}

/// <summary>
/// 延迟队列状态信息
/// </summary>
public class DelayQueueInfo
{
    /// <summary>队列中总消息数</summary>
    public long TotalCount { get; set; }

    /// <summary>已到期待消费数</summary>
    public long ReadyCount { get; set; }

    /// <summary>未到期等待中数</summary>
    public long PendingCount { get; set; }

    /// <summary>下一条消息到期时间（UTC），无待触发消息时为 null</summary>
    public DateTime? NextFireAtUtc { get; set; }
}
