# Art Admin AI 编码指南

## ⛔ 禁止事项 (防止幻觉)

1. **禁止猜测** - 不确定的 API、类名、方法名必须先用工具查找确认
2. **禁止编造** - 不存在的文件、配置、依赖包一律不能使用
3. **禁止假设** - 不要假设某个功能已实现，先 `grep_search` 或 `read_file` 验证
4. **禁止跳过验证** - 每次代码修改后必须验证（见下方检查清单）

---

## ✅ 修改后检查清单

| 修改类型   | 必须执行                                       |
| ---------- | ---------------------------------------------- |
| 后端代码   | `dotnet build` 确认编译通过                    |
| 前端代码   | `get_errors` 检查 TS 错误                      |
| 新增 API   | `curl` 自测接口返回                            |
| 数据库变更 | 同步更新 `schemas/` + `seeds/` + `migrations/` |
| 新增页面   | 插入 `sys_menu` 菜单记录                       |

---

## 📁 关键文件索引 (先查这里)

```
backend/
├── Art.Api/
│   ├── Program.cs                    # 启动配置、中间件注册
│   ├── Routes/Admin/                 # 后台管理路由
│   ├── Routes/App/                   # 客户端应用路由
│   └── Hosting/TaskConfiguration.cs  # 后台任务注册
├── Art.Core/
│   ├── Services/Admin/               # 后台业务服务
│   ├── Services/App/                 # 应用端业务服务
│   ├── Workers/                      # 定时任务
│   └── Shared/                       # 复用业务逻辑
├── Art.Domain/
│   ├── Entities/                     # 数据库实体 (对应表)
│   ├── Enums/                        # 枚举定义
│   └── Exceptions/                   # 自定义异常
├── Art.Infra/
│   ├── Data/ArtDbContext.cs          # EF DbContext
│   ├── Framework/RequestContext.cs   # 当前用户上下文
│   └── Common/                       # 工具类

web-admin/src/
├── api/                              # API 封装
├── views/                            # 页面组件
├── router/                           # 路由配置
└── utils/dict.ts                     # 枚举字典工具

database/
├── schemas/                          # 表结构全量 SQL
├── seeds/                            # 初始数据
└── migrations/                       # 增量变更 SQL
```

---

## 🏗️ 后端架构

### 分层依赖

```
Api → Core → Domain
         ↘ Infra
```

- **Api**: 路由入口，不写业务逻辑
- **Core**: 业务逻辑层
- **Domain**: 实体、枚举、异常（纯定义，无依赖）
- **Infra**: 基础设施（数据库、缓存、工具类）

### 雪花 ID 类型约定

**ID 字段用 `long` 或 `long?`（不用 `string`），框架自动处理前后端精度转换**

### 目录与职责对应

| 目录                  | 职责         | 可否注入 RequestContext |
| --------------------- | ------------ | ----------------------- |
| `Core/Services/Admin` | 后台管理业务 | ✅ 用 `_user.Id`        |
| `Core/Services/App`   | 客户端业务   | ✅ 用 `_user.Id`        |
| `Core/Workers`        | 定时任务     | ❌ 无用户上下文         |
| `Core/Shared`         | 复用业务逻辑 | ❌ 通过参数传入         |
| `Infra/Common`        | 纯工具类     | ❌ 无业务依赖           |

### 服务自动注入

使用 `[Service]` 特性，无需在 `Program.cs` 注册：

```csharp
[Service(ServiceLifetime.Scoped)]
public class SysUserService { }
```

### 路由定义

实现接口自动应用鉴权：

| 接口                | 路径前缀    | 鉴权           |
| ------------------- | ----------- | -------------- |
| `IAdminRouterBase`  | `/admin/*`  | 需平台 Token   |
| `IAppRouterBase`    | `/app/*`    | 需客户端 Token |
| `ICommonRouterBase` | `/common/*` | 公开           |

```csharp
public class SysUserRouter : IAdminRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var g = group.MapGroup("system/user");
        g.MapPost("login", Login).AllowAnonymous();  // 覆盖鉴权
        g.MapGet("info", GetInfo);                   // 继承鉴权
    }
}
```

### 异常处理

抛出 `CustomException` 子类，中间件自动转 JSON：

```csharp
throw new BadRequestException("用户名不能为空");
// 返回: { "code": "BAD_REQUEST", "msg": "用户名不能为空" }
```

### 多条件查询 (必须用 LinqKit)

```csharp
var predicate = PredicateBuilder.New<SysUser>(true);
if (!string.IsNullOrWhiteSpace(req.Name))
    predicate = predicate.And(x => x.Username.Contains(req.Name));
if (req.Status.HasValue)
    predicate = predicate.And(x => x.Status == req.Status.Value);

var list = await _db.SysUser.AsExpandable().Where(predicate).ToListAsync();
```

---

## 💻 前端规范

### 核心约定

1. **API 必须封装** - 在 `src/api/` 下，禁止 view 里直接写请求
2. **类型可用 any** - 对接期用 `ref<any[]>([])` 避免阻塞，稳定后再补
3. **分页参数** - `{ pageIndex, pageSize }` → `{ items, total }`

### 枚举字典

```typescript
import { getEnumOptions } from "@/utils/dict";
const statusOptions = await getEnumOptions("ActiveStatus");
// 自动缓存到 SessionStorage
```

### 新增页面必须插入菜单

```sql
INSERT INTO sys_menu (id, parent_id, name, code, path, component, icon, sort, is_visible, status) VALUES
('menu_order', NULL, '订单管理', 'Order', '/order', '/index/index', 'ri:shopping-cart-line', 10, 1, 1);
```

---

## 🔧 开发命令

```bash
# 后端启动 (端口 5055，Swagger: /swagger)
cd backend/Art.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run

# 前端启动
cd web-admin && pnpm dev

# 数据库执行 SQL
mysql -h localhost -P 3306 -u root -p aaaaaa art < script.sql
```

---

## 📋 数据库变更规范

修改表结构必须同时更新 3 处：

| 文件                                    | 说明                   |
| --------------------------------------- | ---------------------- |
| `database/schemas/*.sql`                | 全量表结构（直接修改） |
| `database/seeds/*.sql`                  | 初始数据               |
| `database/migrations/yyyyMMdd_desc.sql` | 增量变更脚本           |

**注意**: 数据库层面不设外键约束
