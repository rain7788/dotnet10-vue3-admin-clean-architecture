# PredicateBuilder 动态查询

Art Admin 使用 **LinqKit** 的 `PredicateBuilder` 构建动态条件查询，这是所有列表查询的标准模式。

## 基本使用

```csharp
using LinqKit; // 全局引用已包含，无需显式 using

public async Task<UserListResponse> GetUserListAsync(UserListRequest request)
{
    // 1. 创建动态条件（true 表示默认匹配所有）
    var predicate = PredicateBuilder.New<SysUser>(true);

    // 2. 按条件追加筛选
    if (!string.IsNullOrWhiteSpace(request.Keyword))
    {
        predicate = predicate.And(x =>
            x.Username.Contains(request.Keyword) ||
            (x.RealName != null && x.RealName.Contains(request.Keyword)) ||
            (x.Phone != null && x.Phone.Contains(request.Keyword)));
    }

    if (request.Status.HasValue)
        predicate = predicate.And(x => x.Status == request.Status.Value);

    if (request.LastActiveTimeStart.HasValue)
        predicate = predicate.And(x => x.LastActiveTime >= request.LastActiveTimeStart.Value);

    if (request.LastActiveTimeEnd.HasValue)
        predicate = predicate.And(x => x.LastActiveTime <= request.LastActiveTimeEnd.Value);

    // 3. 使用 AsExpandable() 激活 LinqKit
    var query = _context.SysUser
        .AsExpandable()
        .Where(predicate)
        .OrderByDescending(x => x.CreatedTime);

    // 4. 分页
    var total = await query.CountAsync();
    var pageIndex = request.PageIndex ?? 1;
    var pageSize = request.PageSize ?? 20;
    var items = await query
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .Select(x => new UserListItem { ... })
        .ToListAsync();

    return new UserListResponse { Total = total, Items = items };
}
```

## 关键要点

### 1. PredicateBuilder.New\<T\>(true)

- `true` 表示默认条件为恒真（全匹配）
- 如果写 `false`，则需要至少一个 `.Or()` 才能匹配到数据

### 2. .AsExpandable()

- **必须调用**，否则 EF Core 无法正确翻译 `PredicateBuilder` 生成的表达式树
- 放在 `DbSet` 或 `IQueryable` 之后、`.Where()` 之前

### 3. .And() / .Or()

```csharp
// AND 条件：所有条件都必须满足
predicate = predicate.And(x => x.Status == ActiveStatus.正常);

// OR 条件：满足任一条件即可
predicate = predicate.Or(x => x.IsSuper == true);

// 复杂组合：先创建子条件，再合并
var subPredicate = PredicateBuilder.New<SysUser>(false);
subPredicate = subPredicate.Or(x => x.Username.Contains(keyword));
subPredicate = subPredicate.Or(x => x.RealName!.Contains(keyword));
predicate = predicate.And(subPredicate);
```

## 分页约定

- 请求包含 `int? PageIndex = 1` + `int? PageSize = 20`
- 响应包含 `int Total` + `List<T> Items`

```csharp
#region 请求/响应模型

public class UserListRequest
{
    public string? Keyword { get; set; }
    public ActiveStatus? Status { get; set; }
    public DateTime? LastActiveTimeStart { get; set; }
    public DateTime? LastActiveTimeEnd { get; set; }
    public int? PageIndex { get; set; } = 1;
    public int? PageSize { get; set; } = 20;
}

public class UserListResponse
{
    public int Total { get; set; }
    public List<UserListItem> Items { get; set; } = [];
}

#endregion
```

::: tip
DTO 统一定义在 Service 文件底部的 `#region 请求/响应模型` 中。跨服务共享的 DTO 放在 `Art.Domain/Models/` 下。
:::
