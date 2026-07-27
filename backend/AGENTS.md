# Backend Instructions

本文件适用于 `backend/`。同时遵循仓库根目录 `AGENTS.md`。

## 架构边界

| 项目 | 职责 | 当前允许的项目引用 |
| --- | --- | --- |
| `Art.Domain` | 实体、枚举、异常和共享模型 | 无 |
| `Art.Infra` | 数据访问、缓存和框架支撑 | `Art.Domain` |
| `Art.Core` | 业务服务和后台 Worker | `Art.Domain`、`Art.Infra` |
| `Art.Api` | 路由入口和应用装配 | `Art.Core`、`Art.Domain`、`Art.Infra` |

- `Art.Api` 只负责启动、路由和应用级配置，不放业务逻辑。
- `Art.Core/Services/Admin/` 和 `App/` 放对应端业务；`Shared/` 只放真正跨端且无请求上下文耦合的逻辑。
- `Art.Domain` 保持基础且稳定，不依赖 API 或基础设施实现。
- `Art.Infra` 承担 DbContext、缓存、中间件、自动注册和调度器等框架能力。
- 所有测试项目和测试源码必须位于 `backend/tests/`；当前默认项目为 `tests/Art.Tests/`。

## C# 与领域约定

- ID 使用 `long`，新 ID 通过 `IdGen.NextId()` 生成。
- 实体继承 `EntityBase` 或 `EntityBaseWithUpdate`，并用 `[Table("...")]` 指定表名；EF 使用 snake_case，数据库为 MySQL 8.0。
- 单语句 `if` 不加花括号，多语句或嵌套控制流保留花括号。
- 请求/响应 DTO 默认放在对应 Service 文件底部；跨服务共享模型放到 `Art.Domain/Models/Admin/` 或 `Models/App/`。
- 分页请求使用 `PageIndex`、`PageSize`，分页响应使用 `Total`、`Items`。

## 依赖注入

- 常规业务服务使用 `[Service(ServiceLifetime.Scoped)]` 自动扫描注册，不要在 `Program.cs` 重复注册。
- 请求级服务可以依赖 `ArtDbContext` 和 `RequestContext`。
- 后台 Worker 必须使用 `[TaskWorker]`。该特性固定为单例，因此 Worker 必须无状态且不能依赖 `ArtDbContext`、`RequestContext` 或其他 Scoped 服务。
- Worker 访问数据库时注入 `IDbContextFactory<ArtDbContext>`，在每次执行中创建并释放独立 DbContext。
- 修改 Worker 构造函数或注册规则时，同步更新架构测试，确保生命周期约束在测试阶段失败。

## Minimal API 路由

| 接口 | 路由前缀 | 默认鉴权 |
| --- | --- | --- |
| `IAdminRouterBase` | `/admin/*` | 平台端 Token |
| `IAppRouterBase` | `/app/*` | 客户端 Token |
| `ICommonRouterBase` | `/common/*` | 公开 |

- Router 只做路由映射、模型绑定和元数据配置，业务逻辑委托给 Service。
- 列表查询统一使用 POST；分页和筛选放请求体。
- 用 `.AllowAnonymous()` 或 `ApiMeta` 明确覆盖默认鉴权，不自行复制鉴权逻辑。
- 业务异常使用现有 `BadRequestException`、`UnauthorizedException`、`ForbiddenException`、`NotFoundException` 和 `InternalServerException`，由中间件统一转为 `{ code, msg }`。

## 查询与一致性

- 多条件查询使用 LinqKit `PredicateBuilder` + `AsExpandable()`，不要拼接动态 SQL。
- 余额、积分、库存和关键状态变更使用事务及 `FOR UPDATE` 行锁；实体关键字段增加 `[ConcurrencyCheck]` 作为并发兜底。
- 批量锁定多行时，所有调用方必须按一致顺序获取锁，降低死锁风险。
- 数据库变更继续遵守根目录的 schema、seed、migration 三同步规则。

## 后台任务

- 在 `Art.Api/Hosting/TaskConfiguration.cs` 统一注册任务，Worker 自身不负责启动循环。
- `AddRecurringTask` 的 `interval` 表示周期检查间隔；需要时间窗口或防重复时使用 `allowedHours`、`preventDuplicateInterval` 和稳定的 `taskName`。
- `AddLongRunningTask` 中，`runDuration` 是单轮运行窗口，`processingInterval` 是窗口内处理节奏，`interval` 是窗口结束或抢锁失败后再次尝试的等待时间。
- Worker 必须及时响应传入的 `CancellationToken`；取消时不要吞掉应向调度器传播的 `OperationCanceledException`。
- 分布式任务默认使用 Redis 锁。关闭 `useDistributedLock` 前必须确认任务允许多实例并发执行。

## Redis 能力

- 分布式锁使用 `Art.Infra.Cache.RedisLockExtensions`，锁 key 不手工重复添加 `lock:` 前缀。
- 同步快速失败场景使用 `TryLock`；需要等待和自动续期的异步场景使用 `LockAsync`，并用 `await using` 释放。
- 延迟队列使用 `RedisDelayQueueExtensions`；队列名称统一定义在 `Art.Domain/Constants/CacheKeys.cs`。
- 消费 Worker 每次处理有上限的批次并立即返回，由调度器的 `processingInterval` 控制节奏。

## 测试与验证

- 使用 xUnit。测试命名应说明行为和预期，不依赖执行顺序或共享的可变状态。
- 单元测试不得依赖真实生产服务；需要 Redis、MySQL 等外部设施的测试应显式归类为集成测试。
- 新测试默认加入 `tests/Art.Tests/`；只有依赖或运行特征明显不同时才拆分项目。
- 后端改动完成后，从仓库根目录运行：

```bash
dotnet test backend/Art.sln --configuration Release
```
