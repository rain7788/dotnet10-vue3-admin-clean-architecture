# 设计理念

## AI 友好

Art Admin 的核心设计目标是 **让 AI 能高效参与开发**。

传统框架的问题：
- 代码结构不一致，AI 需要大量上下文才能理解
- 隐式约定多（如约定式注入、魔法字符串），AI 容易犯错
- 框架 API 复杂，AI 生成的代码经常不符合框架要求

Art Admin 的解决方案：
- **一致的代码模式** — 所有 Service 用 `[Service]`、所有 Router 实现 `IAdminRouterBase`、所有 Entity 继承 `EntityBase`
- **显式标注** — `[Service(ServiceLifetime.Scoped)]` 明确声明生命周期，无需猜测
- **Copilot 指令文件** — `.github/copilot-instructions.md` 内置完整的编码规范，AI 自动遵循
- **简单的 API** — Minimal API 路由、`PredicateBuilder` 查询、`throw new BadRequestException()` 异常

## 约定优于配置

| 约定 | 说明 |
| --- | --- |
| `[Service]` | 自动扫描注入，无需手动注册 |
| `IAdminRouterBase` | 自动注册到 `/admin/*` 路由组 |
| `EntityBase` | 自带雪花 ID + 创建时间 |
| `PredicateBuilder` | 所有列表查询统一使用动态条件构建 |
| `异常即响应` | throw 异常 → 中间件自动转 JSON |
| `DTO 就近定义` | 请求/响应模型放在 Service 文件底部 |

## 清洁架构

四层分离的核心原则：

1. **Domain 层零依赖** — 只有实体、枚举、异常，可以独立测试
2. **Core 层纯业务** — 不关心 HTTP、不关心框架，只写业务逻辑
3. **Infra 层可替换** — 切换数据库、缓存提供者不影响业务层
4. **Api 层极薄** — 仅路由映射，一行代码对接一个 Service 方法

## 务实的技术选型

- **不用 JWT** — 使用 Reference Token，支持即时吊销和服务端控制
- **不用 MediatR** — 直接注入 Service，减少间接层
- **不用 FluentValidation** — 在 Service 方法内直接校验，代码更直观
- **ID 用 `long`** — 雪花 ID + `SmartLongConverter` 自动处理前端精度
- **单行 if 不加花括号** — 减少代码行数，提升可读性
