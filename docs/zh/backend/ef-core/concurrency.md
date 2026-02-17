# 乐观锁并发控制

Art Admin 使用 EF Core 的 `[ConcurrencyCheck]` 实现乐观锁，作为数据安全的**兜底保护**，防止低频并发场景下多人同时编辑同一条记录导致数据覆盖。

## 什么是乐观锁？

乐观锁不使用数据库锁，而是在更新时检查数据是否被其他请求修改过：

1. 读取记录时，记住关键字段的当前值
2. 更新时，在 `WHERE` 条件中加上这些字段的旧值
3. 如果 `UPDATE` 影响了 0 行，说明数据已被修改，抛出并发冲突异常

::: warning 重要
乐观锁是一种**兜底机制**，冲突时会直接抛异常让用户重试。因此它只适合冲突概率很低的场景（如后台管理员编辑配置）。对于**高并发写入场景**（如库存扣减、秒杀、余额变动），应该使用[分布式锁](/zh/backend/distributed-lock)来保证互斥，而非乐观锁。
:::

## 使用 ConcurrencyCheck

在需要并发保护的字段上添加 `[ConcurrencyCheck]` 注解。推荐直接复用 `UpdatedTime` 字段：

```csharp
[Table("sys_config")]
public class SysConfig : EntityBaseWithUpdate
{
    [MaxLength(100)]
    public string ConfigKey { get; set; } = default!;

    [MaxLength(500)]
    public string ConfigValue { get; set; } = default!;

    [MaxLength(200)]
    public string? Remark { get; set; }

    /// <summary>
    /// 使用更新时间作为乐观锁字段
    /// 多人同时编辑配置项时，后保存的会收到冲突提示
    /// </summary>
    [ConcurrencyCheck]
    public new DateTime? UpdatedTime { get; set; }
}
```

## EF Core 生成的 SQL

标注 `[ConcurrencyCheck]` 后，EF Core 更新时自动将该字段的原始值加入 WHERE 条件：

```sql
-- 管理员 A 读取时 updated_time = '2025-03-15 10:00:00'
UPDATE `sys_config`
SET `config_value` = @newValue, `updated_time` = NOW()
WHERE `id` = @id
  AND `updated_time` = '2025-03-15 10:00:00';  -- ConcurrencyCheck 添加
-- 影响 1 行 ✅

-- 管理员 B 也在同一时间读取了相同记录（updated_time 还是旧值）
UPDATE `sys_config`
SET `config_value` = @newValue2, `updated_time` = NOW()
WHERE `id` = @id
  AND `updated_time` = '2025-03-15 10:00:00';  -- 已被 A 改过，匹配不上
-- 影响 0 行 → EF Core 抛出 DbUpdateConcurrencyException ❌
```

## 处理并发冲突

```csharp
public async Task UpdateConfigAsync(ConfigUpdateRequest req)
{
    var config = await _db.SysConfig.FirstOrDefaultAsync(x => x.Id == req.Id);
    if (config == null)
        throw new NotFoundException("配置不存在");

    config.ConfigValue = req.ConfigValue;
    config.Remark = req.Remark;
    config.UpdatedTime = DateTime.Now;

    try
    {
        await _db.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        // 兜底保护：另一个管理员刚刚修改了这条记录
        throw new BadRequestException("该记录已被其他人修改，请刷新后重试");
    }
}
```

## 适用场景

| 场景 | 推荐方案 | 说明 |
| --- | --- | --- |
| 系统配置编辑 | ✅ 乐观锁 | 管理员同时编辑概率低，冲突时提示刷新即可 |
| 角色权限编辑 | ✅ 乐观锁 | 同上，低频操作 |
| 用户资料修改 | ❌ 不需要 | 通常只有自己编辑，覆盖即可 |
| 库存扣减 / 秒杀 | ❌ **用分布式锁** | 高并发场景，乐观锁会导致大量异常 |
| 余额变动 | ❌ **用分布式锁** | 同上，需要严格互斥 |
| 订单状态变更 | ❌ **用分布式锁** | 并发频率高，且失败重试体验差 |
| 日志记录 | ❌ 不需要 | 只有 INSERT，不存在并发更新 |

## 乐观锁 vs 分布式锁

| | 乐观锁 `[ConcurrencyCheck]` | 分布式锁 `RedisLocker` |
| --- | --- | --- |
| 定位 | 兜底安全网 | 主动互斥 |
| 实现层 | 数据库 WHERE 条件 | Redis SetNx |
| 冲突处理 | 抛异常，用户手动重试 | 等待获锁或立即失败 |
| 性能 | 高（无额外 IO） | 需要 Redis 往返 |
| 适用 | 低冲突（后台配置编辑） | 高冲突（库存、余额、秒杀） |

::: tip 最佳实践
- **后台管理场景**（编辑配置、角色、菜单等）：加 `[ConcurrencyCheck]` 即可，作为兜底保护
- **高并发写入场景**（库存、余额、秒杀等）：必须使用[分布式锁](/zh/backend/distributed-lock)，乐观锁不适用
- 两者不冲突，可以组合使用：分布式锁做主要保护，乐观锁做最后一道防线
:::
