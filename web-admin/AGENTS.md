# Web Admin Instructions

本文件适用于 `web-admin/`，同时遵循仓库根目录 `AGENTS.md`。

## 实现方式

- 使用 Vue 3 `<script setup>`、Element Plus、Pinia、Tailwind CSS 4 和现有 Art 组件；先检查自动导入和路径别名，不重复配置。
- 页面放 `src/views/{module}/{page}/index.vue`，搜索、弹窗等页面专用组件放相邻 `modules/`。
- 开发前先阅读同类生产页面及其公共组件或 Hook；已有能力必须复用，不能为单个页面复制表格、表单、请求或状态基础设施。
- 生产页面决定结构和业务流程；`views/examples/` 只用于查询组件能力，不整页照搬。

## 管理页面基础设施

| 场景 | 必须使用 | 参考实现 |
| --- | --- | --- |
| 分页列表 | `useTable + ArtTableHeader + ArtTable` | `views/system/user/index.vue` |
| 搜索条件 | `ArtSearchBar`、`useTable.searchParams` | `views/system/user/modules/user-search.vue` |
| 树表/无分页表格 | `ArtTable + useTableColumns` | `views/system/menu/index.vue` |
| 行操作 | `ArtButtonTable` 或 `ArtButtonMore` | `views/system/user/index.vue`、`views/system/role/index.vue` |
| 普通 CRUD 弹窗 | `ElDialog + ElForm` | `views/system/role/modules/role-edit-dialog.vue` |
| 配置驱动表单 | `ArtForm` | `views/system/menu/modules/menu-dialog.vue` |
| 组件完整能力 | 现有组件和 Hook | `views/examples/tables/`、`views/examples/forms/` |

- 分页、加载、搜索参数和刷新由 `useTable` 管理；不要在页面重复实现。搜索时更新 `searchParams`，重置使用 `resetSearchParams`。
- 表格列沿用 `columnsFactory`；操作按钮使用现有类型和图标，并通过 `v-auth` 或 `useAuth` 校验权限。
- 字段较少或交互定制较强的表单沿用 `ElForm`；多字段、响应式配置表单使用 `ArtForm`。不要为了统一外观强行改写另一种成熟模式。
- 表单覆盖初始化、校验、提交中、成功关闭、失败恢复和关闭后重置；异步状态在 `finally` 中可靠恢复。
- 固定枚举放 `src/enums/`；后端枚举使用 `getEnumOptions()` / `getEnumLabel()`，不重复缓存或映射。

## API 与契约

- 后端调用只放在 `src/api/`，使用 `@/utils/http` 的 `request`，View 和组件不直接创建请求。
- API 函数使用 `fetch + 动作 + 资源` 命名；请求和响应定义明确类型并与后端字段一致，不用 `any` 掩盖契约差异。
- HTTP 层已统一展示失败信息；业务 `catch` 只恢复本地状态，不重复显示通用错误。
- API 地址通过 `getApiUrl()` 或 HTTP 封装读取，不硬编码域名。

## 路由、菜单与样式

- 菜单由后端 `sys_menu` 驱动；业务页面不修改 `asyncRoutes.ts` 或 `routesAlias.ts`。新增页面或权限同步数据库 migration。
- 保持 art-design-pro 的布局密度和交互，复用设计 token 和工具类，不引入独立页面风格或重复硬编码样式。
- 修改公共组件时检查所有调用方，保持默认 props、事件和插槽语义。

## 验证

- 始终运行 `pnpm --dir web-admin exec vue-tsc --noEmit`。
- 对改动的 TypeScript、Vue、JavaScript 文件运行 `pnpm --dir web-admin exec eslint <changed-files...>`。
- 涉及生产代码、Vite、依赖或构建行为时尽可能运行 `pnpm --dir web-admin build`；被基线问题阻断时保留证据并说明。
