# 乐观锁并发控制

Art Admin 使用 EF Core 的 `[ConcurrencyCheck]` 实现乐观锁，作为数据安全的**兜底保护**，主要用于两类场景：

1. **低频编辑场景** — 防止多人同时编辑同一条记录导致数据覆盖（如后台配置、角色编辑）
2. **状态流转场景** — 防止并发请求同时修改状态字段导致状态跳变异常（如订单状态、审批状态、卡券核销）

## 什么是乐观锁？

乐观锁不使用数据库锁，而是在更新时检查数据是否被其他请求修改过：

1. 读取记录时，记住关键字段的当前值
2. 更新时，在 `WHERE` 条件中加上这些字段的旧值
3. 如果 `UPDATE` 影响了 0 行，说明数据已被修改，抛出并发冲突异常

::: warning 重要
乐观锁是一种**兜底机制**，冲突时会直接抛异常让用户重试。因此它只适合冲突概率很低的场景（如后台管理员编辑配置）。对于**高并发写入场景**，应根据业务特征选择合适的方案：
- **库存扣减、余额变动** — 使用[分布式锁](/zh/backend/distributed-lock)保证互斥，适合并发量可控的场景
- **秒杀、抢购** — 使用[消息队列](/zh/backend/message-queue)做流量削峰，将瞬时高并发请求异步化，避免大量请求排队等锁导致 Redis 压力过大
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

## 状态字段的乐观锁保护

除了 `UpdatedTime`，`[ConcurrencyCheck]` 还有一个重要用途：**保护状态字段的流转安全**。

状态字段（如订单状态、审批状态）本质上是一个有限状态机。在并发场景下，两个请求可能同时读到相同的旧状态并尝试修改，导致状态跳变异常。在状态字段上加 `[ConcurrencyCheck]`，可以在数据库层面兜底，确保只有一个状态变更能成功。

### 示例：订单状态流转

```csharp
[Table("biz_order")]
public class BizOrder : EntityBaseWithUpdate
{
    public long UserId { get; set; }

    [MaxLength(50)]
    public string OrderNo { get; set; } = default!;

    public decimal Amount { get; set; }

    /// <summary>
    /// 订单状态：0-待支付 1-已支付 2-已发货 3-已完成 4-已取消
    /// 加 ConcurrencyCheck 防止并发状态跳变
    /// </summary>
    [ConcurrencyCheck]
    public int Status { get; set; }
}
```

```csharp
public async Task PayOrderAsync(long orderId)
{
    var order = await _db.BizOrder.FirstOrDefaultAsync(x => x.Id == orderId)
        ?? throw new NotFoundException("订单不存在");

    if (order.Status != 0)
        throw new BadRequestException("订单状态不允许支付");

    order.Status = 1; // 待支付 → 已支付
    order.UpdatedTime = DateTime.Now;

    try
    {
        await _db.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        // 另一个请求已经修改了状态（如用户同时点了取消）
        throw new BadRequestException("订单状态已变更，请刷新后重试");
    }
}
```

生成的 SQL 会同时校验状态的旧值：

```sql
UPDATE `biz_order`
SET `status` = 1, `updated_time` = NOW()
WHERE `id` = @id AND `status` = 0;   -- 只有状态仍为「待支付」才能更新
-- 如果已被改为 4（已取消），影响 0 行 → 抛出并发异常
```

### 典型场景

| 场景 | 并发风险 | 说明 |
| --- | --- | --- |
| 订单状态 | 用户点支付的同时点取消，或后台同时操作发货 | 防止「待支付」同时变为「已支付」和「已取消」 |
| 审批流程 | 多个审批人同时通过/驳回同一条申请 | 防止重复审批或状态跳变 |
| 工单状态 | 客服同时处理同一工单（接单、关闭） | 防止已被接单的工单被另一人重复接单 |
| 优惠券/卡券 | 用户在多端同时使用同一张券 | 「未使用 → 已使用」只能成功一次 |
| 退款状态 | 管理员同时审批同一笔退款 | 防止重复退款 |

::: tip 与分布式锁的配合
对于状态字段，乐观锁是**轻量且有效的兜底手段**，无需引入 Redis 依赖。如果业务上还涉及金额计算等需要严格互斥的操作，可以在外层加分布式锁做主动保护，状态字段的 `[ConcurrencyCheck]` 作为最后一道防线。
:::

## 适用场景

| 场景 | 推荐方案 | 说明 |
| --- | --- | --- |
| 系统配置编辑 | ✅ 乐观锁 | 管理员同时编辑概率低，冲突时提示刷新即可 |
| 角色权限编辑 | ✅ 乐观锁 | 同上，低频操作 |
| 状态字段流转 | ✅ 乐观锁 | 订单/审批/工单等状态变更，防止并发跳变，在数据库层兜底 |
| 优惠券/卡券核销 | ✅ 乐观锁 | 「未使用→已使用」只能成功一次，乐观锁天然适合 |
| 用户资料修改 | ❌ 不需要 | 通常只有自己编辑，覆盖即可 |
| 库存扣减 | ❌ **用分布式锁** | 并发量可控，需要严格互斥保证数据一致性 |
| 余额变动 | ❌ **用分布式锁** | 同上，需要严格互斥 |
| 秒杀 / 抢购 | ❌ **用消息队列** | 瞬时流量极高，分布式锁会导致大量请求排队等锁，Redis 压力过大；应通过消息队列削峰异步处理 |
| 日志记录 | ❌ 不需要 | 只有 INSERT，不存在并发更新 |

## 乐观锁 vs 分布式锁 vs 消息队列

| | 乐观锁 `[ConcurrencyCheck]` | 分布式锁 `RedisLocker` | 消息队列 `Redis MQ` |
| --- | --- | --- | --- |
| 定位 | 兜底安全网 | 主动互斥 | 流量削峰、异步解耦 |
| 实现层 | 数据库 WHERE 条件 | Redis SetNx | Redis List (LPUSH/RPOP) |
| 冲突处理 | 抛异常，用户手动重试 | 等待获锁或立即失败 | 请求入队，异步消费 |
| 性能 | 高（无额外 IO） | 需要 Redis 往返 | 入队极快，消费按自身节奏 |
| 适用 | 低冲突（后台配置编辑） | 中等并发互斥（库存、余额） | 瞬时高并发（秒杀、抢购） |

::: tip 最佳实践
- **后台管理场景**（编辑配置、角色、菜单等）：加 `[ConcurrencyCheck]` 即可，作为兜底保护
- **互斥写入场景**（库存扣减、余额变动等）：使用[分布式锁](/zh/backend/distributed-lock)保证同一资源不被并发修改
- **瞬时高流量场景**（秒杀、抢购等）：使用[消息队列](/zh/backend/message-queue)做流量削峰，先快速响应用户「排队中」，再由消费者按可控速率处理订单。直接用分布式锁会导致大量请求阻塞等锁，极端情况下可能压垮 Redis
- 三者不冲突，可按需组合：消息队列削峰 → 消费者内部用分布式锁保证互斥 → 乐观锁做最后一道防线
:::
