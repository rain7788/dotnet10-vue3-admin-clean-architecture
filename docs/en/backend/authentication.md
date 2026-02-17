# Authentication

Art Admin uses **JWT Token** authentication with multi-client Token type support.

## Token Types

| Type | Routes | Scenario |
| --- | --- | --- |
| Platform Token | `/admin/*` | Backend admin users |
| Client Token | `/app/*` | Mobile/client users |
| None | `/common/*` | Public APIs |

## Authentication Flow

```
1. Login → Verify credentials → Generate Token (contains userId + tenantId + tokenType)
2. Request → Authorization Header → Token Middleware
3. Middleware → Validate Token → Extract userId → Fill RequestContext
4. Service → Use _user.Id to access current user
```

## Token Service

```csharp
public class TokenService
{
    public string GenerateToken(long userId, string? tenantId, TokenType type)
    {
        // Generate JWT containing userId, tenantId, tokenType
        // Return token string
    }

    public TokenPayload? ValidateToken(string token)
    {
        // Validate and parse Token
        // Return payload or null if invalid
    }
}
```

## Using Current User in Services

```csharp
[Service(ServiceLifetime.Scoped)]
public class OrderService
{
    private readonly ArtDbContext _db;
    private readonly RequestContext _user;

    public OrderService(ArtDbContext db, RequestContext user)
    {
        _db = db;
        _user = user;
    }

    public async Task<object> GetMyOrdersAsync()
    {
        // _user.Id is automatically available after authentication
        var orders = await _db.Orders
            .Where(x => x.UserId == _user.Id)
            .ToListAsync();
        return orders;
    }
}
```

## Route Auth Configuration

### Default Behavior

Routes implement the corresponding interface to get the right auth:

```csharp
// Requires Platform Token
public class UserRouter : IAdminRouterBase { }

// Requires Client Token
public class AppOrderRouter : IAppRouterBase { }

// No auth required
public class PublicRouter : ICommonRouterBase { }
```

### Override per Endpoint

```csharp
// Skip auth for specific endpoint
g.MapGet("public-info", ...)
    .AllowAnonymous();

// Or set auth type via ApiMeta
g.MapGet("public-info", ...)
    .WithMetadata(new ApiMeta { AuthType = TokenType.无 });
```

## Password Handling

Passwords are hashed using `PasswordHelper`:

```csharp
// Hash password
var hash = PasswordHelper.HashPassword("raw_password");

// Verify password
var isValid = PasswordHelper.VerifyPassword("raw_password", hash);
```
