# 快速开始

## 环境要求

| 依赖 | 版本 |
| --- | --- |
| .NET SDK | 10.0+ |
| Node.js | 20+ |
| pnpm | 10+ |
| MySQL | 8.0+ |
| Redis | 6.0+ |

## 1. 克隆项目

```bash
git clone https://github.com/rain7788/dotnet10-vue3-admin-clean-architecture.git
cd dotnet10-vue3-admin-clean-architecture
```

## 2. 初始化数据库

```bash
# 创建数据库
mysql -u root -p -e "CREATE DATABASE art DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"

# 导入表结构
mysql -u root -p art < database/schemas/01_core_tables.sql

# 导入种子数据
mysql -u root -p art < database/seeds/01_sys_user.sql

# 执行迁移（按顺序）
for f in database/migrations/*.sql; do
  mysql -u root -p art < "$f"
done
```

## 3. 启动后端

```bash
cd backend/Art.Api

# 修改数据库连接（可选，默认 localhost:3306）
# vim appsettings.json

# 启动
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

后端运行在 `http://localhost:5055`，Swagger UI 地址 `http://localhost:5055/swagger`。

## 4. 启动前端

```bash
cd web-admin

# 安装依赖
pnpm install

# 启动开发服务器
pnpm dev
```

前端运行在 `http://localhost:5173`。

## 5. 登录系统

默认管理员账号：

| 账号 | 密码 |
| --- | --- |
| `admin` | `123456` |

## 目录结构速览

```
art-admin/
├── backend/                # 后端 .NET 项目
│   ├── Art.Api/            # API 入口层（路由、中间件配置）
│   ├── Art.Core/           # 核心业务层（Service、Worker）
│   ├── Art.Domain/         # 领域层（实体、枚举、异常，零依赖）
│   └── Art.Infra/          # 基础设施层（DbContext、缓存、框架）
├── web-admin/              # 前端 Vue 3 项目
├── database/               # 数据库脚本
│   ├── schemas/            # 表结构
│   ├── seeds/              # 种子数据
│   └── migrations/         # 增量迁移
├── deploy/                 # 部署配置（Docker、K8s）
└── docs/                   # 文档站（本站）
```
