# 异常处理

Art Admin 使用 **异常驱动** 的错误处理方式：在 Service 中直接 `throw` 自定义异常，全局异常中间件统一转换为 `{ code, msg }` JSON 响应。

## 异常类型

| 异常类 | HTTP 状态码 | 典型场景 |
| --- | --- | --- |
| `BadRequestException` | 400 | 参数错误、业务规则校验失败 |
| `UnauthorizedException` | 401 | 未登录或 Token 过期 |
| `ForbiddenException` | 403 | 无权限访问 |
| `NotFoundException` | 404 | 资源不存在 |
| `NotAcceptableException` | 406 | 不可接受（限购、库存不足等） |
| `InternalServerException` | 500 | 服务器内部错误 |
| `ThirdPartyServerException` | 502 | 第三方服务异常 |

## 使用方式

```csharp
// 单行 if 不加花括号
if (user == null)
    throw new NotFoundException("用户不存在");

if (user.Status != ActiveStatus.正常)
    throw new BadRequestException("用户已被禁用");

if (!PasswordHelper.Verify(request.Password, user.Password))
    throw new BadRequestException("密码错误");
```

## 自定义错误码

```csharp
// 使用自定义错误码（优先于 HTTP 状态码）
throw new BadRequestException(10001, "余额不足");

// 中间件返回：
// { "code": 10001, "msg": "余额不足" }
```

## 响应格式

```json
{
  "code": 400,
  "msg": "用户名和密码不能为空"
}
```

- `code` — 优先使用自定义 `ErrorCode`，否则使用 HTTP 状态码
- `msg` — 异常消息（生产环境隐藏非业务异常的内部消息）

## 开发环境

开发环境（`ASPNETCORE_ENVIRONMENT=Development`）会显示真实异常消息，方便调试。生产环境对于非业务异常（未标记的 `Exception`）仅返回"服务器错误"。
