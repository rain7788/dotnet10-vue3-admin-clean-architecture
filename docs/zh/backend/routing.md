# 路由系统

Art Admin 使用 .NET Minimal API 路由，通过三套路由接口实现 **多端 API 隔离**。路由类实现对应接口，启动时自动扫描注册。

## 三套路由接口

```csharp
// 管理端 — /admin/*，需要平台 Token
public interface IAdminRouterBase
{
    void AddRoutes(RouteGroupBuilder group);
}

// 应用端 — /app/*，需要客户端 Token
public interface IAppRouterBase
{
    void AddRoutes(RouteGroupBuilder group);
}

// 公共端 — /common/*，无需鉴权
public interface ICommonRouterBase
{
    void AddRoutes(RouteGroupBuilder group);
}
```

## 路由定义示例

```csharp
public class SysUserRouter : IAdminRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var g = group.MapGroup("system/user")
            .WithGroupName(ApiGroups.Admin)
            .WithTags("系统用户");

        // 登录（覆盖鉴权 — 无需 Token）
        g.MapPost("login", async (LoginRequest req, SysUserService svc) =>
            await svc.LoginAsync(req))
            .WithMetadata(new ApiMeta { AuthType = TokenType.无 })
            .WithSummary("登录");

        // 获取当前用户信息（默认需要平台 Token）
        g.MapGet("info", async (SysUserService svc) =>
            await svc.GetCurrentUserAsync())
            .WithSummary("获取当前用户信息");

        // 获取用户列表（POST 传分页 + 筛选参数）
        g.MapPost("list", async (UserListRequest req, SysUserService svc) =>
            await svc.GetUserListAsync(req))
            .WithSummary("获取用户列表");

        // 删除用户
        g.MapDelete("{id}", async (long id, SysUserService svc) =>
        {
            await svc.DeleteUserAsync(id);
            return Results.Ok(new { message = "删除成功" });
        })
            .WithSummary("删除用户");
    }
}
```

## 关键特性

### 服务通过 Lambda 参数注入

路由 handler 的参数由 DI 容器自动注入，无需手动 `GetService()`：

```csharp
g.MapPost("list", async (UserListRequest req, SysUserService svc) =>
    await svc.GetUserListAsync(req));
//                              ^^^ 自动从 DI 容器注入
```

### 覆盖鉴权

通过 `ApiMeta` 元数据覆盖默认的 Token 要求：

```csharp
// 设置为无需鉴权
.WithMetadata(new ApiMeta { AuthType = TokenType.无 })

// 或使用 AllowAnonymous
.AllowAnonymous()
```

### Swagger 分组

每个路由接口对应一个 Swagger 文档分组：

| 接口 | Swagger 分组 | URL |
| --- | --- | --- |
| `IAdminRouterBase` | 管理端 | `/swagger/admin/swagger.json` |
| `IAppRouterBase` | 应用端 | `/swagger/app/swagger.json` |
| `ICommonRouterBase` | 公共端 | `/swagger/common/swagger.json` |

### 所有列表查询用 POST

为什么？因为列表查询通常需要传递复杂的筛选条件和分页参数，GET 请求的 QueryString 不适合传递复杂对象。

```csharp
// ✅ 正确：POST + 请求体
g.MapPost("list", async (UserListRequest req, SysUserService svc) => ...)

// ❌ 避免：GET + 多个 QueryString
g.MapGet("list", async (string? keyword, int? status, int? page) => ...)
```
