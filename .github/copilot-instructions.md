# Art Admin AI 编码指南

## ⛔ 原则

- **禁止猜测** — 不确定的 API、类名、方法名必须先搜索确认
- **禁止编造** — 不存在的文件、配置、依赖包不能使用
- **修改后必须验证** — 后端 `dotnet build`；前端 `get_errors`
- **数据库变更三同步** — `database/schemas/` + `seeds/` + `migrations/yyyyMMdd_desc.sql`（无外键约束）
- **新增页面** — 必须在 `database/migrations/` 中插入 `sys_menu` 菜单记录

---

## 🏗️ 后端（.NET Minimal API 自创架构）

### 分层

```
Art.Api（路由入口，无业务）→ Art.Core（业务逻辑）→ Art.Domain（实体/枚举/异常，零依赖）
                                       ↘ Art.Infra（DbContext/缓存/框架支撑）
```

| 目录                   | 职责                               | RequestContext |
| ---------------------- | ---------------------------------- | -------------- |
| `Core/Services/Admin/` | 后台管理业务                       | ✅ `_user.Id`  |
| `Core/Services/App/`   | 客户端业务                         | ✅ `_user.Id`  |
| `Core/Workers/`        | 定时任务（用 `IDbContextFactory`） | ❌             |
| `Core/Shared/`         | 复用逻辑（参数传入）               | ❌             |

### 关键约定

- **ID 用 `long`**，雪花 ID 由 `IdGen.NextId()` 生成，框架自动处理前端精度
- **实体继承** `EntityBase`（Id + CreatedTime）或 `EntityBaseWithUpdate`（+ UpdatedTime），用 `[Table("表名")]` 注解
- **EF Snake Case 命名**，MySQL 8.0
- **单行 if 不加花括号**：`if (x == null)` 换行 `throw new NotFoundException("...");`
- 多语句/嵌套控制流保留花括号

### 服务注入（`[Service]` 特性，自动扫描，禁止在 Program.cs 重复注册）

```csharp
[Service(ServiceLifetime.Scoped)]
public class XxxService
{
    private readonly ArtDbContext _db;
    private readonly RequestContext _user;
    public XxxService(ArtDbContext db, RequestContext user) { _db = db; _user = user; }
}
```

### 路由（Minimal API，服务通过 lambda 参数注入）

| 接口                | 前缀        | 鉴权         |
| ------------------- | ----------- | ------------ |
| `IAdminRouterBase`  | `/admin/*`  | 平台 Token   |
| `IAppRouterBase`    | `/app/*`    | 客户端 Token |
| `ICommonRouterBase` | `/common/*` | 公开         |

```csharp
public class XxxRouter : IAdminRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var g = group.MapGroup("module/xxx").WithGroupName(ApiGroups.Admin).WithTags("Xxx管理");

        g.MapPost("list", async (XxxListRequest req, XxxService svc) => await svc.GetListAsync(req))
            .WithSummary("列表");

        g.MapPost("save", async (XxxSaveRequest req, XxxService svc) => await svc.SaveAsync(req))
            .WithSummary("新增/编辑");

        g.MapDelete("{id}", async (long id, XxxService svc) => { await svc.DeleteAsync(id); })
            .WithSummary("删除");

        // 覆盖鉴权: .WithMetadata(new ApiMeta { AuthType = TokenType.无 }) 或 .AllowAnonymous()
    }
}
```

> **所有列表查询用 POST**（请求体传分页+筛选参数）

### 异常（中间件自动转 `{ code, msg }` JSON）

`BadRequestException`(400) / `UnauthorizedException`(401) / `ForbiddenException`(403) / `NotFoundException`(404) / `InternalServerException`(500)

### 多条件查询（必须用 LinqKit）

```csharp
var predicate = PredicateBuilder.New<XxxEntity>(true);
if (!string.IsNullOrWhiteSpace(req.Keyword))
    predicate = predicate.And(x => x.Name.Contains(req.Keyword));

var query = _db.Xxx.AsExpandable().Where(predicate);
var total = await query.CountAsync();
var items = await query.OrderByDescending(x => x.Id)
    .Skip(((req.PageIndex ?? 1) - 1) * (req.PageSize ?? 20)).Take(req.PageSize ?? 20).ToListAsync();
```

### DTO 约定

- **请求/响应 DTO 定义在同一 Service 文件底部**（`#region 请求/响应模型`）
- 跨服务共享的 DTO 放 `Art.Domain/Models/Admin/` 或 `Models/App/`
- 分页请求含 `int? PageIndex = 1` + `int? PageSize = 20`
- 分页响应含 `int Total` + `List<T> Items`

### 并发控制（余额/积分/关键状态变更）

涉及余额、积分、库存等关键字段更新时，必须用事务 + `FOR UPDATE` 行锁查询后再操作，并在实体关键字段加 `[ConcurrencyCheck]` 兜底。

```csharp
// 实体：关键字段加 [ConcurrencyCheck]
public class WalletEntity : EntityBase
{
    [ConcurrencyCheck]
    public decimal Balance { get; set; }
}

// Service：事务 + FOR UPDATE 行锁
await using var tx = await _db.Database.BeginTransactionAsync();

// FOR UPDATE 锁定行，防止并发读取脏数据
var wallet = await _db.Wallet
    .FromSqlRaw("SELECT * FROM wallet WHERE id = {0} FOR UPDATE", id)
    .FirstOrDefaultAsync();
if (wallet == null)
    throw new NotFoundException("钱包不存在");

wallet.Balance -= amount;  // EF 修改
await _db.SaveChangesAsync(); // [ConcurrencyCheck] 字段值不一致时抛 DbUpdateConcurrencyException
await tx.CommitAsync();
```

> `FOR UPDATE` 锁行到事务结束；`[ConcurrencyCheck]` 在 `SaveChanges` 时追加 `WHERE balance = @old` 作为第二道防线。批量锁多行时 `WHERE id IN (...)` 加 `FOR UPDATE`，需保证所有调用方按相同顺序加锁以避免死锁。

---

## 💻 前端（art-design-pro 3.0.2）

**技术栈**: Vue 3 `<script setup>` + Vite + Element Plus + Pinia + TailwindCSS 4 + Axios

**自动导入**: `ref`, `computed`, `watch`, `onMounted`, `useRouter` 等 Vue/VueUse API 无需 import

**路径别名**: `@` → `src/`，`@views` → `src/views/`

### 核心规则

1. **API 封装在 `src/api/`**，view 禁止直接写请求
2. **禁止重复弹窗** — `src/utils/http` 已有全局 `ElMessage.error` 拦截，`catch` 里只做状态还原，不要再弹错误
3. **后端路由模式** — 菜单由 `sys_menu` 表驱动，禁止修改前端静态路由文件（`asyncRoutes.ts`、`routesAlias.ts`）
4. **类型可用 any** — 对接期 `ref<any[]>([])` 避免阻塞
5. **权限指令**: `v-auth="'system:user:add'"` 控制按钮显隐

### HTTP 请求（`import request from '@/utils/http'`）

```ts
request.post<T>({ url: '/admin/xxx/list', params: { ... } })   // POST params 自动转 body
request.get<T>({ url: '/admin/xxx/info', params: { id } })
request.del<T>({ url: `/admin/xxx/${id}` })
// 选项: showErrorMessage: false（关闭错误弹窗）、showSuccessMessage: true（显示成功提示）
```

### API 封装（命名: `fetch` + 动作 + 资源）

```ts
export function fetchGetXxxList(params: any) {
  return request.post<any>({ url: "/admin/module/xxx/list", params });
}
export function fetchSaveXxx(data: any) {
  return request.post<any>({ url: "/admin/module/xxx/save", data });
}
export function fetchDeleteXxx(id: string) {
  return request.del<any>({ url: `/admin/module/xxx/${id}` });
}
```

### 页面结构（`src/views/{模块}/{页面}/index.vue` + `modules/` 子组件）

```vue
<template>
  <div class="xxx-page art-full-height">
    <XxxSearch
      v-model="searchForm"
      @search="handleSearch"
      @reset="resetSearchParams"
    />
    <ElCard class="art-table-card" shadow="never">
      <ArtTableHeader
        v-model:columns="columnChecks"
        :loading="loading"
        @refresh="refreshData"
      >
        <template #left>
          <ElButton @click="showDialog('add')" v-auth="'module:xxx:add'"
            >新增</ElButton
          >
        </template>
      </ArtTableHeader>
      <ArtTable
        :loading="loading"
        :data="data"
        :columns="columns"
        :pagination="pagination"
        @pagination:size-change="handleSizeChange"
        @pagination:current-change="handleCurrentChange"
      />
    </ElCard>
    <XxxDialog
      v-model:visible="dialogVisible"
      :type="dialogType"
      :data="currentData"
      @submit="handleDialogSubmit"
    />
  </div>
</template>

<script setup lang="ts">
const {
  columns,
  columnChecks,
  data,
  loading,
  pagination,
  getData,
  searchParams,
  resetSearchParams,
  handleSizeChange,
  handleCurrentChange,
  refreshData,
} = useTable({
  core: {
    apiFn: fetchGetXxxList,
    apiParams: { ...searchForm.value },
    columnsFactory: () => [
      /* 列定义 */
    ],
  },
});
</script>
```

分页参数名: `pageIndex` / `pageSize`，响应自动识别 `list|data|records` + `total|count`

### 枚举

- **固定枚举**: `src/enums/` 定义，页面直接用
- **动态枚举**: `const opts = await getEnumOptions('ActiveStatus')`（自动缓存，从 `@/utils/dict` 导入）

---

## ⏰ 后台任务（`Art.Api/Hosting/TaskConfiguration.cs`）

Worker 类用 `[Service(ServiceLifetime.Transient)]`，需要DB时注入 `IDbContextFactory<ArtDbContext>`（禁止注入 `ArtDbContext`），无 `RequestContext`。

```csharp
// Worker 定义
[Service(ServiceLifetime.Transient)]
public class XxxWorker
{
    private readonly IDbContextFactory<ArtDbContext> _contextFactory;
    public XxxWorker(IDbContextFactory<ArtDbContext> contextFactory) { _contextFactory = contextFactory; }

    public async Task DoWork(CancellationToken cancel)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancel);
        // ...
    }
}
```

在 `TaskConfiguration` 构造函数注入并在 `ConfigureTasks` 中注册：

```csharp
// 定时任务（周期触发）
taskScheduler.AddRecurringTask(
    _xxxWorker.DoWork,
    interval: TimeSpan.FromMinutes(30),      // 调度间隔
    allowedHours: [2, 3],                    // 可选：只在这些小时执行
    preventDuplicateInterval: TimeSpan.FromHours(12), // 可选：窗口内只执行一次（Redis去重）
    useDistributedLock: true,                // 可选：多Pod分布式锁（默认true）
    taskName: "xxx.daily");                  // 可选：任务名

// 消息队列/延迟队列消费（两层节奏）
taskScheduler.AddLongRunningTask(
    _xxxWorker.ProcessQueue,
    interval: TimeSpan.FromSeconds(1),       // 外层：多久尝试进入一轮运行窗口
    processingInterval: TimeSpan.FromMilliseconds(100), // 内层：每次处理后的等待（节流/防空转）
    runDuration: TimeSpan.FromSeconds(30),   // 单轮最长运行时长（到期退出释放锁）
    taskName: "xxx.queue.consume");
```

> 队列 worker 的 `ProcessQueue` 每次批量消费 N 条（RPOP/ZRANGEBYSCORE+ZREM），消费完立即返回，由 `processingInterval` 控制节奏。

---

## � Redis 分布式锁（`Art.Infra.Cache.RedisLockExtensions`）

注入 `RedisClient _cache`（using alias `RedisClient = FreeRedis.RedisClient`）。

```csharp
// TryLock：立即返回，获取失败返回 null，同步场景用 using
using var locker = _cache.TryLock("biz:key", timeoutSeconds: 30);
if (locker == null)
    throw new BadRequestException("操作频繁，请稍后重试");
// ... 业务逻辑，using 块结束自动释放

// LockAsync：等待直到超时，异步场景用 await using
await using var locker = await _cache.LockAsync(
    "biz:key",
    timeout: TimeSpan.FromSeconds(30),     // 锁过期时间（兜底）
    waitTimeout: TimeSpan.FromSeconds(10), // 最长等待时间，默认等于 timeout
    retryInterval: 200,                    // 重试间隔ms，默认50
    enableWatchdog: true);                 // 自动续期，默认true
if (locker == null)
    throw new BadRequestException("获取锁超时");
// ... 业务逻辑
```

> 锁 key 自动加 `lock:` 前缀；`TryLock` 默认 `enableWatchdog: false`，`LockAsync` 默认 `true`。

---

## 📬 Redis 延迟队列（`Art.Infra.Cache.RedisDelayQueueExtensions`）

基于 Sorted Set，score = 到期 Unix 毫秒时间戳，Lua 原子消费。

```csharp
// 投递（Service 中）
_cache.DelayQueuePublish(CacheKeys.XxxQueue, payload, delay: TimeSpan.FromMinutes(5));
_cache.DelayQueuePublishBatch(CacheKeys.XxxQueue, payloads, delay: TimeSpan.FromMinutes(5));
// overwrite: true（默认）= ZAdd 覆盖同值分数；false = ZAddNx 不覆盖

// 消费（Worker 中，ProcessQueue 方法）
var messages = _cache.DelayQueueConsume(CacheKeys.XxxQueue, maxCount: 20);
// 返回已到期的消息，原子 ZRANGEBYSCORE + ZREM，多消费者安全

// 移除指定消息
_cache.DelayQueueRemove(CacheKeys.XxxQueue, payload);
```

> 消费 Worker 配合 `AddLongRunningTask` 注册；队列名统一定义在 `Art.Domain/Constants/CacheKeys.cs`。

---

## �🔧 命令

```bash
cd backend/Art.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run   # 后端 :5055
cd web-admin && pnpm dev                                              # 前端
mysql -h localhost -P 3306 -u root -p aaaaaa art < script.sql         # 执行 SQL
```
