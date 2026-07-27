# 介绍

Art Admin 是一个 **AI 友好的全栈后台管理框架**，基于 **.NET 10 Minimal API** + **Vue 3 (art-design-pro)** 构建。采用清洁架构设计，提供完整的权限管理、用户认证和多端 API 路由功能。

## 为什么选择 Art Admin？

### 为 AI 协作而设计

传统后台框架代码冗余、风格不一致、隐式约定多，AI 难以理解和生成正确的代码。Art Admin 从架构层面解决了这个问题：

- **清洁分层** — 四层架构职责清晰，AI 知道代码该写在哪里
- **约定优于配置** — `[Service]` 自动注入、`IAdminRouterBase` 自动注册路由，零样板代码
- **一致的代码模式** — 所有 Service、Router、Entity 遵循相同的写法规范
- **完善的 Agent 指令** — 项目内置 `AGENTS.md`，AI 助手可直接读取编码与测试约定

### 开箱即用

| 能力 | 实现 |
| --- | --- |
| 自动依赖注入 | `[Service]` 特性 + 反射扫描 |
| 自动路由注册 | `IAdminRouterBase` / `IAppRouterBase` / `ICommonRouterBase` |
| 认证鉴权 | Reference Token + Redis 缓存 |
| RBAC 权限 | 用户-角色-权限-菜单完整权限体系 |
| 多租户 | `ITenantEntity` + 自动 QueryFilter |
| 分布式锁 | Redis SetNx + 看门狗续期 |
| 消息队列 | Redis List（RPUSH / RPOP） |
| 延迟队列 | Redis Sorted Set + Lua 原子消费 |
| 定时任务 | 自研调度器 + 分布式锁防重 + 优雅退出 |
| 日志系统 | Serilog 按天分表写 MySQL |
| 雪花 ID | Yitter + Redis 自动分配 WorkerId |

### 高颜值前端

前端基于 [art-design-pro](https://www.artd.pro/docs/zh/guide/introduce.html)，Vue 3 + Element Plus + TailwindCSS 4，提供现代化的中后台界面。

> 详细的前端文档请参考 [art-design-pro 官方文档](https://www.artd.pro/docs/zh/)

### 多端 API 支持

框架内置管理端、应用端、公共端三套独立 API，可以随意扩展：

| 接口 | 前缀 | 鉴权 |
| --- | --- | --- |
| `IAdminRouterBase` | `/admin/*` | 平台 Token |
| `IAppRouterBase` | `/app/*` | 客户端 Token |
| `ICommonRouterBase` | `/common/*` | 公开 |

## 技术栈

| 技术 | 版本 | 说明 |
| --- | --- | --- |
| .NET | 10.0 | 运行时框架 |
| ASP.NET Core | 10.0 | Web 框架（Minimal API） |
| Entity Framework Core | 9.0 | ORM |
| Pomelo.EntityFrameworkCore.MySql | 9.0 | MySQL 驱动 |
| FreeRedis | 1.5.5 | Redis 客户端 |
| Serilog | 4.3.0 | 结构化日志 |
| Yitter.IdGenerator | 1.0.14 | 雪花 ID 生成 |
| Swashbuckle | 10.1.0 | Swagger 文档 |
| Flurl.Http | 4.0.2 | HTTP 客户端 |
| Vue 3 | 3.x | 前端框架（Composition API） |
| Element Plus | - | UI 组件库 |
| TailwindCSS | 4.x | 原子化 CSS |
| Vite | 7.x | 极速构建 |
| Pinia | - | 状态管理 |

## 在线体验

- **管理后台**: [https://admin.aftbay.com](https://admin.aftbay.com)
- **Swagger API**: [https://api.aftbay.com/swagger](https://api.aftbay.com/swagger)

### API 文档（SwaggerSloop）

本项目的 Swagger UI 基于作者自研开源项目 **SwaggerSloop**，提供**高颜值**的 API 文档体验，并支持多分组与鉴权调试：

- 项目地址：[https://github.com/rain7788/SwaggerSloop](https://github.com/rain7788/SwaggerSloop)
- 使用说明：[/zh/backend/swagger](/zh/backend/swagger)

> 演示账号：`admin` / `123456`
>
> 演示环境已开启 **Demo 模式**，所有修改操作（新增、编辑、删除、修改密码等）会被拦截，不会实际写入数据库。

### Demo 模式

项目内置了演示模式开关，适用于部署公开 Demo 站点。开启后，用户可以正常浏览和登录，但所有写操作会返回 `403 演示环境，不允许修改数据` 提示。

**开启/关闭方式**：修改 `backend/Art.Api/appsettings.json`：

```json
{
  "Settings": {
    "DemoMode": true
  }
}
```

也可通过环境变量覆盖：`Settings__DemoMode=true`

> 💡 **完全删除演示模式**：如果不需要此功能，删除 `DemoModeMiddleware.cs` 并在 `Program.cs` 中移除 `app.UseMiddleware<DemoModeMiddleware>();` 即可。

## 开源协议

[MIT License](https://opensource.org/licenses/MIT) — 免费商用，无任何限制。
