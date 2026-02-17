# Routing

Art Admin uses .NET **Minimal API** for route definitions. Routes are organized by interface contracts into three categories.

## Route Interfaces

| Interface | Prefix | Auth |
| --- | --- | --- |
| `IAdminRouterBase` | `/admin/*` | Platform Token |
| `IAppRouterBase` | `/app/*` | Client Token |
| `ICommonRouterBase` | `/common/*` | Public |

## Route Example

```csharp
public class UserRouter : IAdminRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var g = group.MapGroup("system/user")
            .WithGroupName(ApiGroups.Admin)
            .WithTags("User Management");

        g.MapPost("list", async (UserListRequest req, UserService svc)
            => await svc.GetListAsync(req))
            .WithSummary("User List");

        g.MapPost("save", async (UserSaveRequest req, UserService svc)
            => await svc.SaveAsync(req))
            .WithSummary("Add/Edit User");

        g.MapDelete("{id}", async (long id, UserService svc)
            => await svc.DeleteAsync(id))
            .WithSummary("Delete User");
    }
}
```

## Key Rules

### All List Queries Use POST

```csharp
// ✅ Correct — pagination + filters in request body
g.MapPost("list", async (UserListRequest req, UserService svc)
    => await svc.GetListAsync(req));

// ❌ Wrong — GET with query params
g.MapGet("list", async ([AsParameters] UserListRequest req, UserService svc)
    => await svc.GetListAsync(req));
```

### Service Injection via Lambda Parameters

Services are injected through Minimal API's lambda parameter binding — no need for `[FromServices]`:

```csharp
g.MapPost("list", async (
    UserListRequest req,    // request body
    UserService svc         // auto-injected from DI
) => await svc.GetListAsync(req));
```

### Override Authentication

```csharp
// Allow anonymous access
g.MapGet("public-data", ...)
    .AllowAnonymous();

// Or use ApiMeta
g.MapGet("public-data", ...)
    .WithMetadata(new ApiMeta { AuthType = TokenType.无 });
```

## Multi-Client Support

The same business logic can serve both Admin and App clients. Token types are distinguished by middleware, and `RequestContext` is populated accordingly.

```
Admin routes → Platform Token → RequestContext.Id
App routes   → Client Token  → RequestContext.Id
```
