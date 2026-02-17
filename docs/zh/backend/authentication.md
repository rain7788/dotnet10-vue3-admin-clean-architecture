# 认证鉴权

Art Admin 使用 **Reference Token（引用令牌）** 而非 JWT 进行认证，Token 存储在数据库 + Redis 缓存中。

## 为什么不用 JWT？

| | JWT | Reference Token |
| --- | --- | --- |
| Token 内容 | 携带用户信息（可解码） | 不透明字符串（`sc_` + GUID） |
| 即时吊销 | ❌ 无法及时失效 | ✅ 服务端删除即失效 |
| Token 大小 | 大（包含 Payload） | 小（32 字符） |
| 服务端状态 | 无状态 | 有状态（需要存储） |

Reference Token 的优势：可以随时吊销、不泄露用户信息、Token 长度可控。

## Token 生命周期

```
用户登录
  │
  ▼
创建 RefreshToken（180天有效）── 存入 token_refresh 表
  │
  ▼
创建 AccessToken（1小时有效）── 存入 token_access 表
  │                             Token 格式：sc_{GUID}
  ▼
返回给客户端
  │
  ├─── 正常请求 ──► Authorization: Bearer sc_xxx
  │                   │
  │                   ▼
  │              鉴权中间件：Redis 缓存 → DB 查询
  │
  ├─── Token 过期 ──► 用 RefreshToken 刷新
  │                   创建新 AccessToken + 更新 RefreshToken
  │
  └─── 退出登录 ──► 将 Token 过期时间设为当前时间
```

## 鉴权中间件流程

```csharp
public async Task Invoke(HttpContext context, ArtDbContext dbContext,
    RequestContext requestContext, RedisClient cache)
{
    // 1. 获取路由元数据（是否需要鉴权）
    var apiMeta = context.GetEndpoint()?.Metadata.GetMetadata<ApiMeta>();

    // 2. 提取 Token
    var token = ExtractToken(context); // Authorization: Bearer sc_xxx

    // 3. 提取租户信息
    ExtractTenantId(context, requestContext);

    // 4. 如果需要鉴权
    if (requiredTokenType != TokenType.无)
    {
        // Token → MD5 Hash → Redis 缓存查询 → DB 查询
        var userInfo = await GetUserInfo(token, dbContext, cache);
        PopulateContext(requestContext, userInfo);
    }

    // 5. 异步更新用户活跃时间（不阻塞主流程）
    _ = UpdateLastActiveTimeAsync(tokenType, userId);

    await _next(context);
}
```

## RequestContext

鉴权成功后，用户信息填充到 `RequestContext`（Scoped 生命周期）：

```csharp
public class RequestContext
{
    public long Id { get; set; }         // 用户 ID
    public string? Name { get; set; }    // 用户名
    public string? Account { get; set; } // 账号
    public bool IsSuper { get; set; }    // 是否超管
    public string TenantId { get; set; } // 租户 ID
    public string RequestId { get; set; }// 请求追踪 ID
    public string? RequestIp { get; set; }
}
```

在 Service 中直接注入使用：

```csharp
[Service(ServiceLifetime.Scoped)]
public class SysUserService
{
    private readonly RequestContext _user;

    public async Task<UserInfoResponse> GetCurrentUserAsync()
    {
        // 直接使用当前登录用户的 ID
        var user = await _db.SysUser
            .Where(x => x.Id == _user.Id)
            .FirstOrDefaultAsync();
    }
}
```

## 多端 Token 隔离

| Token 类型 | 路由前缀 | 鉴权接口 |
| --- | --- | --- |
| 平台端 Token | `/admin/*` | `IAdminRouterBase` |
| 客户端 Token | `/app/*` | `IAppRouterBase` |
| 无需 Token | `/common/*` | `ICommonRouterBase` |

平台端和客户端使用不同的 `TokenType`，互相不能混用。
