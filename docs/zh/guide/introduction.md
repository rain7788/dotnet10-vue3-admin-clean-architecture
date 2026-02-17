# 介绍

Art Admin 是一个 **AI 友好的全栈后台管理框架**，基于 **.NET 10 Minimal API** + **Vue 3 (art-design-pro)** 构建。

## 为什么选择 Art Admin？

### 为 AI 协作而设计

传统后台框架代码冗余、风格不一致、隐式约定多，AI 难以理解和生成正确的代码。Art Admin 从架构层面解决了这个问题：

- **清洁分层** — 四层架构职责清晰，AI 知道代码该写在哪里
- **约定优于配置** — `[Service]` 自动注入、`IAdminRouterBase` 自动注册路由，零样板代码
- **一致的代码模式** — 所有 Service、Router、Entity 遵循相同的写法规范
- **完善的 Copilot 指令** — 项目内置 `.github/copilot-instructions.md`，AI 助手可直接读取框架约定

### 开箱即用

| 能力 | 实现 |
| --- | --- |
| 自动依赖注入 | `[Service]` 特性 + 反射扫描 |
| 自动路由注册 | `IAdminRouterBase` / `IAppRouterBase` / `ICommonRouterBase` |
| 认证鉴权 | Reference Token + Redis 缓存 |
| 多租户 | `ITenantEntity` + 自动 QueryFilter |
| 分布式锁 | Redis SetNx + 看门狗续期 |
| 消息队列 | Redis List（RPUSH / RPOP） |
| 延迟队列 | Redis Sorted Set + Lua 原子消费 |
| 定时任务 | 自研调度器 + 分布式锁防重 |
| 日志系统 | Serilog 按天分表写 MySQL |
| 雪花 ID | Yitter + Redis 自动分配 WorkerId |

### 高颜值前端

前端基于 [art-design-pro](https://www.artd.pro/docs/zh/guide/introduce.html)，Vue 3 + Element Plus + TailwindCSS 4，提供现代化的中后台界面。

> 详细的前端文档请参考 [art-design-pro 官方文档](https://www.artd.pro/docs/zh/)

## 在线体验

- **管理后台**: [https://admin.aftbay.com](https://admin.aftbay.com)
- **Swagger API**: [https://api.aftbay.com/swagger](https://api.aftbay.com/swagger)

## 开源协议

[MIT License](https://opensource.org/licenses/MIT) — 免费商用，无任何限制。
