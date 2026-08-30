# Backend Instructions

本文件适用于 `backend/`，同时遵循仓库根目录 `AGENTS.md`。

## 架构边界

| 项目 | 职责 | 允许引用 |
| --- | --- | --- |
| `Art.Domain` | 实体、枚举、异常、共享模型 | 无 |
| `Art.Infra` | 数据访问、缓存、框架能力 | `Art.Domain` |
| `Art.Core` | 业务 Service、Worker | `Art.Domain`、`Art.Infra` |
| `Art.Api` | 路由、启动和应用装配 | `Art.Core`、`Art.Domain`、`Art.Infra` |

- Router 只负责映射、绑定和元数据，业务放 Service；框架能力放 `Art.Infra`。
- Admin、App 业务分别放 `Services/Admin/`、`Services/App/`；`Shared/` 只放真正跨端的逻辑。
- 测试统一放在 `backend/tests/`，默认使用 `tests/Art.Tests/`。

## 推荐实现索引

新增实现前先阅读对应基础设施定义和参考调用；直接复用或扩展，不创建平行实现。

| 场景 | 基础设施 | 参考实现 |
| --- | --- | --- |
| Minimal API + Service | `IAdminRouterBase`、`ServiceAttribute` | `Art.Api/Routes/Admin/System/SysRoleRouter.cs`、`Art.Core/Services/Admin/System/SysRoleService.cs` |
| EF 动态筛选与分页 | `PredicateBuilder`、`AsExpandable` | `Art.Core/Services/Admin/System/SysUserService.cs` 的 `GetUserListAsync` |
| Redis 分布式锁 | `Art.Infra/Cache/RedisLockExtensions.cs` | `Art.Core/Services/Admin/Demo/DemoDistributedLockService.cs` |
| Redis List 消息队列 | FreeRedis `LPush` / `RPop` | `Art.Core/Services/Admin/Demo/DemoMessageQueueService.cs`、`Art.Core/Workers/DemoMessageQueueWorker.cs` |
| Redis 延迟队列 | `Art.Infra/Cache/RedisDelayQueueExtensions.cs` | `Art.Core/Services/Admin/Demo/DemoDelayQueueService.cs`、`Art.Core/Workers/DemoDelayQueueWorker.cs` |
| 周期与长期任务 | `Art.Infra/Framework/Jobs/ITaskScheduler.cs` | `Art.Core/Workers/DailyWorker.cs`、`Art.Api/Hosting/TaskConfiguration.cs` |

以上 Demo 仅是对应基础设施的用法样板，业务校验、消息模型和错误处理按目标模块设计。

## 领域与 EF

- ID 使用 `long`，通过 `IdGen.NextId()` 生成；实体继承 `EntityBase` 或 `EntityBaseWithUpdate`，使用 `[Table]`，数据库为 MySQL 8.0、snake_case。
- 请求/响应 DTO 默认放对应 Service 文件底部；跨 Service 模型放 `Art.Domain/Models/Admin/` 或 `Models/App/`。
- 分页字段使用 `PageIndex`、`PageSize`、`Total`、`Items`。
- 多条件查询沿用 `PredicateBuilder + AsExpandable`；分页沿用 `CountAsync + Skip/Take + Select + ToListAsync`，不要自行增加 Repository 或分页封装。
- 保留 `ArtDbContext` 的多租户过滤；修改公共查询、事务或并发行为前检查所有调用方。
- 单语句 `if` 不加花括号，多语句或嵌套控制流保留花括号。

## 注册与路由

- 常规 Service 使用 `[Service(ServiceLifetime.Scoped)]` 自动注册，不在 `Program.cs` 重复注册。
- Admin、App、公开路由分别实现 `IAdminRouterBase`、`IAppRouterBase`、`ICommonRouterBase`；列表查询统一 POST，筛选和分页放请求体。
- 使用 `.AllowAnonymous()` 或 `ApiMeta` 覆盖默认鉴权，不复制鉴权逻辑。
- 使用现有业务异常，由中间件统一返回 `{ code, msg }`。

## Worker 与队列

- Worker 使用 `[TaskWorker]`，固定为无状态单例；不得注入 `ArtDbContext`、`RequestContext` 或其他 Scoped 服务。数据库访问使用 `IDbContextFactory<ArtDbContext>`，每轮创建并释放 DbContext。
- 任务只在 `Art.Api/Hosting/TaskConfiguration.cs` 注册。普通周期任务使用 `AddRecurringTask`；队列消费和需要运行窗口的任务使用 `AddLongRunningTask`。
- 长期任务的循环由调度器负责。Worker 每次只处理一个有上限的批次并返回，不自行编写常驻循环；消费节奏由 `processingInterval` 控制。
- `interval` 是周期检查间隔，或长期任务窗口结束/抢锁失败后的重试间隔；`runDuration` 是长期任务单轮窗口；`processingInterval` 是窗口内两次调用的间隔。
- Worker 在批次和工作单元边界检查 `CancellationToken`；取消时让 `OperationCanceledException` 向调度器传播。
- 调度任务默认启用 Redis 分布式锁。需要防重复或时间窗口时配置 `allowedHours`、`preventDuplicateInterval` 和稳定 `taskName`；确认允许多实例并发后才关闭锁。
- 业务缓存 key 和队列名统一放 `Art.Domain/Constants/CacheKeys.cs`。普通 FIFO 队列沿用 `LPush + RPop`；延迟队列使用 `DelayQueuePublish/Batch/Consume`，不要重写 Sorted Set 或 Lua 消费逻辑。
- 消费必须限制单批数量。延迟队列使用 `DelayQueueConsume` 的原子领取；不要先查询再手工删除。
- 修改 Worker 生命周期、构造函数或注册规则时，同步更新 `TaskWorkerArchitectureTests`。

## Redis 锁

- 锁必须使用 `RedisLockExtensions`，key 不手工添加 `lock:` 前缀。
- 快速失败使用 `TryLock`；等待和自动续期使用 `LockAsync`，并以 `using` / `await using` 释放。
- 现有锁能力不足时扩展 `RedisLockExtensions` 并补测试，不在业务 Service 内另写锁协议。

## 验证

- 使用 xUnit；外部 Redis、MySQL 测试显式归类为集成测试。
- 后端改动后从仓库根目录运行：`dotnet test backend/Art.sln --configuration Release`。
