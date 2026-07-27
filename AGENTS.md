# Art Admin Repository Instructions

本文件适用于整个仓库。进入子目录工作时，还必须读取并遵循距离目标文件最近的 `AGENTS.md`；子目录规则补充或覆盖本文件中的领域规则。

## 目录与规则

| 路径 | 职责 | 领域规则 |
| --- | --- | --- |
| `backend/` | .NET 10 API、业务、领域和基础设施 | `backend/AGENTS.md` |
| `web-admin/` | Vue 3 管理端 | `web-admin/AGENTS.md` |
| `docs/` | VitePress 中英文文档 | `docs/AGENTS.md` |
| `database/` | MySQL schema、seed 和增量迁移 | 本文件 |
| `deploy/`、`.github/workflows/` | 容器、Kubernetes 和 CI/CD | 本文件 |

## 通用原则

- 修改前先搜索现有定义、调用点和相邻实现，不猜测 API、类型、路径或依赖。
- 优先沿用已有架构和公共组件；不要在功能改动中夹带无关重构。
- 行为变更和缺陷修复应同步增加或更新测试；测试必须验证外部可观察行为。
- 不编辑或提交生成物与依赖目录，例如 `bin/`、`obj/`、`dist/`、`node_modules/`、覆盖率和测试结果。
- 不在代码、文档、脚本或日志中写入密码、Token、私钥及真实生产凭据。
- 修改公共契约时同步检查所有消费者，包括后端、前端、文档、数据库脚本和部署配置。

## 跨模块同步

- 数据库结构变更必须同步 `database/schemas/`、`database/seeds/`（如涉及初始数据）和 `database/migrations/yyyyMMdd_desc.sql`；项目不使用外键约束。
- 新增页面、菜单或权限时，必须在 `database/migrations/` 中提供对应的 `sys_menu` 增量记录。
- API 路由、请求或响应模型变化时，同步更新 `web-admin/src/api/` 调用、相关 TypeScript 类型和中英文文档。
- 面向用户的功能或配置说明发生变化时，保持 `docs/zh/` 与 `docs/en/` 对应内容一致。

## 验证要求

从仓库根目录执行与改动相关的最小完整验证：

| 改动范围 | 必须执行 |
| --- | --- |
| 后端 | `dotnet test backend/Art.sln --configuration Release` |
| 前端 TypeScript/Vue | `pnpm --dir web-admin exec vue-tsc --noEmit`、对改动文件执行 ESLint，并尽可能执行 `pnpm --dir web-admin build` |
| 文档 | `pnpm --dir docs build` |
| Docker | 构建对应 Dockerfile |
| Workflow/YAML | 解析 YAML，并验证其中引用的脚本或命令 |

不要为了通过验证而批量修复无关历史问题。验证被现有基线问题阻断时，交付说明中必须区分本次回归与既有问题，并明确剩余风险。
