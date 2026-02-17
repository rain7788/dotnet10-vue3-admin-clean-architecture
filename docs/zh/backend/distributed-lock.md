# 分布式锁

Art Admin 提供基于 Redis 的分布式锁，支持**看门狗自动续期**，保证锁在持有期间不会过期。

## 两种获取方式

### LockAsync — 等待获取

```csharp
// 等待获取锁，超时返回 null
await using var locker = await _cache.LockAsync(
    key: "order:process:123",      // 锁名称（自动添加 lock: 前缀）
    timeout: TimeSpan.FromSeconds(30),  // 锁超时时间
    waitTimeout: TimeSpan.FromSeconds(10), // 等待获取超时
    retryInterval: 50,              // 重试间隔（ms）
    enableWatchdog: true            // 看门狗续期（默认 true）
);

if (locker == null)
    throw new BadRequestException("系统繁忙，请稍后重试");

// 执行业务逻辑...
// locker 被 dispose 时自动释放锁
```

### TryLock — 立即返回

```csharp
// 尝试获取锁，获取不到立即返回 null
using var locker = _cache.TryLock("task:daily-cleanup", timeoutSeconds: 30);

if (locker == null)
    return; // 已有其他实例在执行

// 执行任务...
```

## 看门狗机制

当 `enableWatchdog: true` 时，后台定时器每 `timeout/3` 时间自动续期：

```
锁超时 30s → 每 10s 续期一次
锁超时 60s → 每 20s 续期一次
```

续期使用 Lua 脚本保证**仅当锁值匹配时才续期**，防止误续他人的锁：

```lua
if redis.call('get', KEYS[1]) == ARGV[1] then
    return redis.call('pexpire', KEYS[1], ARGV[2])
else
    return 0
end
```

## 原子解锁

释放锁使用 Lua 脚本保证原子性——只删除自己的锁：

```lua
if redis.call('get', KEYS[1]) == ARGV[1] then
    return redis.call('del', KEYS[1])
else
    return 0
end
```

## 实际场景

### 防止重复下单

```csharp
await using var locker = await _cache.LockAsync(
    $"order:create:{userId}", TimeSpan.FromSeconds(10));

if (locker == null)
    throw new BadRequestException("请勿重复提交");

await CreateOrderAsync(request);
```

### 定时任务防重

```csharp
// 任务调度器内置分布式锁
taskScheduler.AddRecurringTask(
    _dailyWorker.ClearLogs,
    TimeSpan.FromMinutes(21),
    useDistributedLock: true  // 多 Pod 只有一个执行
);
```
