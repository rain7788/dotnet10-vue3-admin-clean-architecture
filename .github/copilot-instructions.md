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

## 🔧 命令

```bash
cd backend/Art.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run   # 后端 :5055
cd web-admin && pnpm dev                                              # 前端
mysql -h localhost -P 3306 -u root -p aaaaaa art < script.sql         # 执行 SQL
```
