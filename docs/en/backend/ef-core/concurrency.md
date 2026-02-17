# Concurrency Control

Art Admin uses EF Core's `[ConcurrencyCheck]` attribute for **optimistic concurrency control**, preventing silent data overwrites.

## The Problem

```
User A reads: price = 100
User B reads: price = 100
User A updates: price = 150 ✅
User B updates: price = 120 ← overwrites A's change?
```

## Solution

### Add Concurrency Field

```csharp
[Table("product")]
public class Product : EntityBaseWithUpdate
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    [ConcurrencyCheck]
    public DateTime? UpdatedTime { get; set; }
}
```

### How It Works

EF Core adds `UpdatedTime` to the `WHERE` clause:

```sql
-- User A reads UpdatedTime = 2025-03-15 10:00:00

UPDATE product
SET price = 150, updated_time = NOW()
WHERE id = 1 AND updated_time = '2025-03-15 10:00:00';
-- affected 1 row ✅

-- User B still has UpdatedTime = 2025-03-15 10:00:00
UPDATE product
SET price = 120, updated_time = NOW()
WHERE id = 1 AND updated_time = '2025-03-15 10:00:00';
-- affected 0 rows → DbUpdateConcurrencyException ❌
```

### Exception Handling

```csharp
public async Task UpdateAsync(ProductUpdateRequest req)
{
    var entity = await _db.Products.FindAsync(req.Id)
        ?? throw new NotFoundException("Product not found");

    entity.Price = req.Price;
    entity.UpdatedTime = DateTime.Now;

    try
    {
        await _db.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        throw new BadRequestException("Data has been modified by another user, please refresh and try again");
    }
}
```

## Why Use UpdatedTime?

Unlike a dedicated `RowVersion`/`ConcurrencyStamp` field:
- Reuses the existing `UpdatedTime` field
- No additional column needed
- For most admin scenarios, second-level precision is sufficient

## When to Use

| Scenario | Need Concurrency Control? |
| --- | --- |
| Product/config editing | ✅ Multiple admins may edit |
| Order status changes | ✅ Prevent double processing |
| User profile updates | ❌ Only self-editing |
| Log/record inserts | ❌ Append-only, no conflicts |
