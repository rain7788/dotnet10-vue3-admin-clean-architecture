# 乐观锁并发控制

在高并发场景下（如秒杀、库存扣减、状态变更），需要防止多个请求同时修改同一行数据导致数据不一致。Art Admin 使用 EF Core 的 `[ConcurrencyCheck]` 实现乐观锁。

## 什么是乐观锁？

乐观锁不使用数据库锁，而是在更新时检查数据是否被其他请求修改过：

1. 读取记录时，记住关键字段的当前值
2. 更新时，在 `WHERE` 条件中加上这些字段的旧值
3. 如果 `UPDATE` 影响了 0 行，说明数据已被修改，抛出并发冲突异常

## 使用 ConcurrencyCheck

在需要并发保护的关键字段上添加 `[ConcurrencyCheck]` 注解：

```csharp
[Table("order")]
public class Order : EntityBaseWithUpdate
{
    [MaxLength(50)]
    public string OrderNo { get; set; } = default!;

    /// <summary>
    /// 订单状态 — 关键业务状态，使用乐观锁保护
    /// </summary>
    [ConcurrencyCheck]
    public OrderStatus Status { get; set; }

    /// <summary>
    /// 库存数量 — 使用乐观锁防止超卖
    /// </summary>
    [ConcurrencyCheck]
    public int Stock { get; set; }

    public decimal Amount { get; set; }
}
```

## EF Core 生成的 SQL

当标注了 `[ConcurrencyCheck]` 后，EF Core 更新时自动在 WHERE 中包含该字段的原始值：

```sql
-- EF Core 自动生成
UPDATE `order`
SET `status` = @newStatus, `stock` = @newStock, `updated_time` = @now
WHERE `id` = @id
  AND `status` = @originalStatus    -- ConcurrencyCheck 添加
  AND `stock` = @originalStock;     -- ConcurrencyCheck 添加
```

如果另一个请求在此期间修改了 `status` 或 `stock`，此 SQL 影响 0 行，EF Core 抛出 `DbUpdateConcurrencyException`。

## 处理并发冲突

```csharp
public async Task ReduceStockAsync(long orderId, int quantity)
{
    var order = await _db.Order.FirstOrDefaultAsync(x => x.Id == orderId);
    if (order == null)
        throw new NotFoundException("订单不存在");

    if (order.Stock < quantity)
        throw new BadRequestException("库存不足");

    order.Stock -= quantity;
    order.UpdatedTime = DateTime.Now;

    try
    {
        await _db.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        // 并发冲突：其他请求已修改了 stock
        throw new BadRequestException("操作冲突，请刷新后重试");
    }
}
```

## 适用场景

| 场景 | 是否需要乐观锁 | 说明 |
| --- | --- | --- |
| 订单状态变更 | ✅ | 防止重复操作（如重复支付） |
| 库存扣减 | ✅ | 防止超卖 |
| 余额变动 | ✅ | 防止重复扣款 |
| 基础信息修改 | ❌ | 覆盖即可，不需要并发保护 |
| 日志记录 | ❌ | 只有 INSERT，不存在并发更新 |

## 乐观锁 vs 分布式锁

| | 乐观锁 `[ConcurrencyCheck]` | 分布式锁 `RedisLocker` |
| --- | --- | --- |
| 实现层 | 数据库 WHERE 条件 | Redis SetNx |
| 锁粒度 | 行级别（精确到字段） | 资源级别（自定义 Key） |
| 性能 | 高（无额外 IO） | 需要 Redis 往返 |
| 冲突处理 | 抛异常，客户端重试 | 等待或立即失败 |
| 适用 | 低冲突频率 | 高冲突或需要互斥执行 |

::: tip 最佳实践
对于**低冲突频率**的场景（如订单状态更新），使用 `[ConcurrencyCheck]` 更轻量。
对于**高频并发**操作（如秒杀），建议组合使用分布式锁 + 乐观锁双重保障。
:::
