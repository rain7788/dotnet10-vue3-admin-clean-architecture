# Art Admin Repository Instructions

本文件适用于整个仓库；进入子目录后同时遵循距离目标文件最近的 `AGENTS.md`。

## 工作方式

- 修改前先定位现有定义、调用点和同类生产实现，不猜测 API、类型、路径或依赖。
- 已有公共组件、扩展方法、Service 或基础设施时必须复用；能力不足时优先扩展原实现并补测试，不新增职责重复的 Helper、封装或依赖。
- 多种写法并存时，以目标目录最近的生产代码和子目录 `AGENTS.md` 中的推荐实现为准；Demo 只用于学习其明确演示的能力。
- 改动保持在任务范围内，不夹带无关重构，不修改生成物或依赖目录，不写入真实凭据。

## 目录规则

| 路径 | 职责 | 规则 |
| --- | --- | --- |
| `backend/` | .NET API、业务和基础设施 | `backend/AGENTS.md` |
| `web-admin/` | Vue 3 管理端 | `web-admin/AGENTS.md` |
| `docs/` | VitePress 中英文文档 | `docs/AGENTS.md` |
| `database/` | MySQL schema、seed 和 migration | 本文件 |
| `deploy/`、`.github/workflows/` | 部署和 CI/CD | 本文件 |

## 跨模块同步

- 数据库结构变更同步 `database/schemas/`、相关 `database/seeds/` 和 `database/migrations/yyyyMMdd_desc.sql`；项目不使用外键。
- 新增页面、菜单或权限时，在 migration 中增加对应 `sys_menu` 记录。
- API 路由或模型变化时，同步后端、`web-admin/src/api/`、TypeScript 类型及中英文文档。
- 用户可见功能或配置变化时，保持 `docs/zh/` 与 `docs/en/` 一致。
- 行为变更和缺陷修复增加验证外部可观察行为的测试。

## 验证

| 范围 | 最小完整验证 |
| --- | --- |
| 后端 | `dotnet test backend/Art.sln --configuration Release` |
| 前端 | `pnpm --dir web-admin exec vue-tsc --noEmit`、改动文件 ESLint；生产代码尽可能执行 build |
| 文档 | `pnpm --dir docs build` |
| Docker | 构建对应 Dockerfile |
| Workflow/YAML | 解析 YAML，并验证引用的脚本和命令 |

不要为通过验证而批量修复历史问题；被基线问题阻断时，说明本次结果与剩余风险。
