# 项目结构

## 后端分层架构

```
backend/
├── Art.Api/                    # API 入口层
│   ├── Program.cs              # 应用启动配置
│   ├── Hosting/
│   │   └── TaskConfiguration.cs # 后台任务配置
│   └── Routes/                 # 路由定义
│       ├── Admin/              # 管理端路由 (/admin/*)
│       ├── App/                # 应用端路由 (/app/*)
│       └── Common/             # 公共路由 (/common/*)
│
├── Art.Core/                   # 核心业务层
│   ├── Services/
│   │   ├── Admin/              # 后台管理业务（注入 RequestContext）
│   │   └── App/                # 客户端业务（注入 RequestContext）
│   ├── Workers/                # 定时任务（用 IDbContextFactory）
│   └── Shared/                 # 复用逻辑（参数传入，无 RequestContext）
│
├── Art.Domain/                 # 领域层（零外部依赖）
│   ├── Entities/               # 实体定义
│   ├── Enums/                  # 枚举
│   ├── Exceptions/             # 自定义异常
│   ├── Constants/              # 常量
│   ├── Models/                 # 跨服务共享 DTO
│   ├── MultiTenancy/           # 多租户接口
│   └── IdGen.cs                # 雪花 ID 封装
│
└── Art.Infra/                  # 基础设施层
    ├── Data/                   # DbContext
    ├── Cache/                  # Redis 客户端、分布式锁、延迟队列
    ├── Common/                 # 通用工具（JSON、密码、雪花ID实现）
    ├── Framework/              # 框架核心
    │   ├── ServiceAttribute.cs # [Service] 自动注入特性
    │   ├── RequestContext.cs   # 请求上下文
    │   ├── ApiMeta.cs          # 路由元数据
    │   ├── Routes/             # 路由注册基础设施
    │   ├── Middlewares/        # 中间件
    │   └── Jobs/               # 任务调度器
    ├── Extensions/             # 扩展方法
    ├── Logging/                # Serilog 日志 Sink
    └── MultiTenancy/           # 多租户 DbContext
```

## 前端结构

```
web-admin/
├── src/
│   ├── api/                # API 封装（按模块分文件）
│   ├── views/              # 页面视图
│   ├── components/         # 公共组件
│   ├── composables/        # 组合式函数（useTable 等）
│   ├── router/             # 路由配置
│   ├── store/              # Pinia 状态管理
│   ├── utils/              # 工具函数
│   │   ├── http/           # Axios 封装
│   │   ├── table/          # 表格工具
│   │   └── dict.ts         # 枚举字典
│   ├── enums/              # 前端枚举
│   ├── directives/         # 自定义指令（v-auth 等）
│   └── locales/            # 国际化
└── vite.config.ts
```

## 数据库

```
database/
├── schemas/                  # 完整表结构（CREATE TABLE）
│   └── 01_core_tables.sql
├── seeds/                    # 种子数据（INSERT）
│   └── 01_sys_user.sql
└── migrations/               # 增量迁移（ALTER / INSERT）
    ├── 20260117_snowflake_id.sql
    └── 20260217_*.sql
```

## 依赖方向

```
Art.Api  ──────►  Art.Core  ──────►  Art.Domain（零依赖）
                     │
                     ▼
                  Art.Infra
```

- **Art.Domain** 不依赖任何外部项目，只有实体、枚举、异常等纯 POCO
- **Art.Infra** 提供 EF Core、Redis、框架等基础设施
- **Art.Core** 引用 Domain 和 Infra，编写业务逻辑
- **Art.Api** 仅定义路由映射，不含业务逻辑
