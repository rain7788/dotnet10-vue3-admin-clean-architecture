# Optimistic Concurrency Control

Art Admin uses EF Core's `[ConcurrencyCheck]` attribute for optimistic concurrency control — a **safety net** serving two purposes:

1. **Low-frequency editing** — prevents silent data overwrites when multiple admins edit the same record simultaneously (e.g., system configs, role permissions)
2. **Status field transitions** — prevents concurrent requests from causing invalid state jumps (e.g., order status, approval status, coupon redemption)

## The Problem

```
Admin A reads config: value = "old"
Admin B reads config: value = "old"
Admin A updates: value = "foo" ✅
Admin B updates: value = "bar" ← overwrites A's change silently?
```

::: warning Important
Optimistic locking is a **safety net** mechanism — it throws an exception on conflict, requiring the user to retry. It is only suitable for **low-contention scenarios** (e.g., admin editing configurations). For **high-concurrency writes**, choose the right strategy based on the traffic pattern:
- **Inventory / balance changes** — use a [distributed lock](/en/backend/distributed-lock) for mutual exclusion, suitable when concurrency is moderate and controllable
- **Flash sales / limited-time offers** — use a [message queue](/en/backend/message-queue) for traffic shaping (peak shaving); distributed locks would cause massive request queuing and could overwhelm Redis under extreme load
:::

## Add Concurrency Field

Reuse the `UpdatedTime` field as the concurrency token — no extra column needed:

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
    /// Use UpdatedTime as optimistic lock field.
    /// If two admins edit simultaneously, the later save gets a conflict error.
    /// </summary>
    [ConcurrencyCheck]
    public new DateTime? UpdatedTime { get; set; }
}
```

## How It Works

EF Core adds `UpdatedTime` to the `WHERE` clause:

```sql
-- Admin A reads UpdatedTime = '2025-03-15 10:00:00'
UPDATE sys_config
SET config_value = 'foo', updated_time = NOW()
WHERE id = 1 AND updated_time = '2025-03-15 10:00:00';
-- affected 1 row ✅

-- Admin B still has UpdatedTime = '2025-03-15 10:00:00'
UPDATE sys_config
SET config_value = 'bar', updated_time = NOW()
WHERE id = 1 AND updated_time = '2025-03-15 10:00:00';
-- affected 0 rows → DbUpdateConcurrencyException ❌
```

## Exception Handling

```csharp
public async Task UpdateConfigAsync(ConfigUpdateRequest req)
{
    var entity = await _db.SysConfig.FindAsync(req.Id)
        ?? throw new NotFoundException("Config not found");

    entity.ConfigValue = req.ConfigValue;
    entity.Remark = req.Remark;
    entity.UpdatedTime = DateTime.Now;

    try
    {
        await _db.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        // Safety net: another admin just modified this record
        throw new BadRequestException("This record has been modified by another user, please refresh and try again");
    }
}
```

## Why Use UpdatedTime?

- Reuses the existing `UpdatedTime` field from `EntityBaseWithUpdate`
- No additional column needed
- For admin scenarios, second-level precision is sufficient

## Optimistic Lock on Status Fields

Beyond `UpdatedTime`, `[ConcurrencyCheck]` has another important use case: **guarding status field transitions**.

Status fields (order status, approval status, etc.) are essentially finite state machines. Under concurrency, two requests may read the same old status and attempt conflicting transitions. Adding `[ConcurrencyCheck]` to the status field ensures only one transition succeeds at the database level.

### Example: Order Status Transition

```csharp
[Table("biz_order")]
public class BizOrder : EntityBaseWithUpdate
{
    public long UserId { get; set; }

    [MaxLength(50)]
    public string OrderNo { get; set; } = default!;

    public decimal Amount { get; set; }

    /// <summary>
    /// Order status: 0-Pending 1-Paid 2-Shipped 3-Completed 4-Cancelled
    /// ConcurrencyCheck prevents concurrent status jumps
    /// </summary>
    [ConcurrencyCheck]
    public int Status { get; set; }
}
```

```csharp
public async Task PayOrderAsync(long orderId)
{
    var order = await _db.BizOrder.FirstOrDefaultAsync(x => x.Id == orderId)
        ?? throw new NotFoundException("Order not found");

    if (order.Status != 0)
        throw new BadRequestException("Order status does not allow payment");

    order.Status = 1; // Pending → Paid
    order.UpdatedTime = DateTime.Now;

    try
    {
        await _db.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        // Another request already changed the status (e.g., user cancelled simultaneously)
        throw new BadRequestException("Order status has changed, please refresh and try again");
    }
}
```

The generated SQL validates the old status value:

```sql
UPDATE `biz_order`
SET `status` = 1, `updated_time` = NOW()
WHERE `id` = @id AND `status` = 0;   -- Only updates if still "Pending"
-- If already changed to 4 (Cancelled), affects 0 rows → throws concurrency exception
```

### Typical Scenarios

| Scenario | Concurrency Risk | Description |
| --- | --- | --- |
| Order status | User clicks pay and cancel simultaneously, or admin ships at the same time | Prevents "Pending" from becoming both "Paid" and "Cancelled" |
| Approval workflow | Multiple approvers approve/reject the same request concurrently | Prevents duplicate approvals or status jumps |
| Work order status | Multiple agents handle the same ticket (claim, close) | Prevents a claimed ticket from being claimed again |
| Coupon / voucher | User redeems the same coupon on multiple devices | "Unused → Used" can only succeed once |
| Refund status | Multiple admins approve the same refund | Prevents duplicate refunds |

::: tip Combining with Distributed Lock
For status fields, optimistic locking is a **lightweight and effective safety net** with no Redis dependency. If the business logic also involves amount calculations requiring strict mutual exclusion, add a distributed lock as the primary protection while `[ConcurrencyCheck]` on the status field serves as the final line of defense.
:::

## When to Use

| Scenario | Recommended Approach | Why |
| --- | --- | --- |
| System config editing | ✅ Optimistic lock | Low contention, conflict prompt is fine |
| Role/permission editing | ✅ Optimistic lock | Low-frequency admin operations |
| Status field transitions | ✅ Optimistic lock | Order/approval/work order status changes — prevents concurrent state jumps at the DB level |
| Coupon / voucher redemption | ✅ Optimistic lock | "Unused → Used" must succeed only once; optimistic lock is a natural fit |
| User profile updates | ❌ Not needed | Self-editing only, overwrite is fine |
| Inventory deduction | ❌ **Distributed lock** | Moderate concurrency, requires strict mutual exclusion for data consistency |
| Balance changes | ❌ **Distributed lock** | Same as above, strict mutual exclusion needed |
| Flash sales / limited offers | ❌ **Message queue** | Extreme burst traffic; distributed locks would cause massive request queuing and risk overwhelming Redis. Use MQ for peak shaving and async processing |
| Log/record inserts | ❌ Not needed | Append-only, no conflicts |

## Optimistic Lock vs Distributed Lock vs Message Queue

| | Optimistic Lock `[ConcurrencyCheck]` | Distributed Lock `RedisLocker` | Message Queue `Redis MQ` |
| --- | --- | --- | --- |
| Role | Safety net (last line of defense) | Active mutual exclusion | Traffic shaping & async decoupling |
| Implementation | Database WHERE clause | Redis SetNx | Redis List (LPUSH/RPOP) |
| On conflict | Throws exception, user retries manually | Waits for lock or fails immediately | Request enqueued, consumed asynchronously |
| Performance | High (no extra IO) | Requires Redis round-trip | Enqueue is very fast, consumption at its own pace |
| Best for | Low contention (admin config editing) | Moderate concurrency mutual exclusion (inventory, balance) | Burst high concurrency (flash sales, limited offers) |

::: tip Best Practice
- **Admin management** (editing configs, roles, menus): add `[ConcurrencyCheck]` as a safety net
- **Mutual exclusion writes** (inventory deduction, balance changes): use [distributed lock](/en/backend/distributed-lock) to prevent concurrent modifications on the same resource
- **Burst traffic scenarios** (flash sales, limited offers): use [message queue](/en/backend/message-queue) for peak shaving — respond instantly with "queued", then process orders asynchronously at a controlled rate. Using distributed locks here would cause massive request blocking and could crash Redis under extreme load
- The three are complementary and can be combined: MQ for peak shaving → distributed lock inside the consumer for mutual exclusion → optimistic lock as the final safety net
:::
