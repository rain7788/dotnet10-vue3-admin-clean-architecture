# Swagger 文档

Art Admin 使用自研中间件 **SwaggerSloop** 提供 API 文档，支持多分组、Token 认证。

## 在线预览

访问 [api.aftbay.com/swagger](https://api.aftbay.com/swagger) 查看在线文档。

## 多 API 分组

Art Admin 将 API 按业务分为多个组，分别展示在 Swagger UI 中：

| 分组 | 说明 | 前缀 |
| --- | --- | --- |
| `Admin` | 后台管理接口 | `/admin/*` |
| `App` | 客户端接口 | `/app/*` |
| `Common` | 公共接口 | `/common/*` |

### 路由定义时指定分组

```csharp
public class XxxRouter : IAdminRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var g = group.MapGroup("module/xxx")
            .WithGroupName(ApiGroups.Admin)  // 指定分组
            .WithTags("Xxx管理");            // 指定分类标签

        g.MapPost("list", async (XxxListRequest req, XxxService svc)
            => await svc.GetListAsync(req))
            .WithSummary("查询列表");        // 接口说明

        g.MapPost("save", async (XxxSaveRequest req, XxxService svc)
            => await svc.SaveAsync(req))
            .WithSummary("新增/编辑");
    }
}
```

### 分组常量

```csharp
// ApiGroups.cs
public static class ApiGroups
{
    public const string Admin = "admin";
    public const string App = "app";
    public const string Common = "common";
}
```

## Token 认证

Swagger UI 集成了 Bearer Token 认证，点击 `Authorize` 按钮输入 Token 即可调试需要鉴权的接口。

## SwaggerSloop

[SwaggerSloop](https://github.com/rain7788/SwaggerSloop) 是 Art Admin 作者开源的 Swagger 增强中间件，特性：

- 自动扫描 Minimal API 路由生成 OpenAPI 文档
- 支持多分组导航（下拉切换 Admin / App / Common）
- 内置 Bearer Token 认证 UI
- 支持 `WithSummary()` / `WithDescription()` / `WithTags()` 等描述

### 注册

```csharp
// Program.cs
builder.Services.AddSwaggerSloop();

// 中间件
app.UseSwaggerSloop();
```

## 常用注解

| 方法 | 说明 |
| --- | --- |
| `.WithGroupName(name)` | 指定 API 分组 |
| `.WithTags(tag)` | 指定分类标签 |
| `.WithSummary(text)` | 接口简要说明 |
| `.WithDescription(text)` | 接口详细描述 |

## 覆盖鉴权

```csharp
// 某个接口跳过鉴权
g.MapGet("public-data", ...)
    .AllowAnonymous();

// 或使用 ApiMeta 指定
g.MapGet("public-data", ...)
    .WithMetadata(new ApiMeta { AuthType = TokenType.无 });
```
