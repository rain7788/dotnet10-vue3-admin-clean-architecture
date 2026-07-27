# Web Admin Instructions

本文件适用于 `web-admin/`。同时遵循仓库根目录 `AGENTS.md`。

## 技术栈与结构

- Vue 3 `<script setup>`、Vite、Element Plus、Pinia、Tailwind CSS 4 和 Axios。
- Vue/VueUse 常用 API 已自动导入；先检查现有配置，不重复添加 import。
- 路径别名：`@` 指向 `src/`，`@views` 指向 `src/views/`。
- 页面放在 `src/views/{module}/{page}/index.vue`，页面专用搜索、弹窗等组件放相邻 `modules/`。
- 优先复用现有 Art 组件、composable、布局和交互模式，避免为单个页面复制基础设施。

## API 与错误处理

- 所有后端调用封装在 `src/api/`，View 和组件不得直接创建 Axios 请求。
- 使用 `@/utils/http` 的 `request`；列表查询调用 `request.post`，筛选和分页通过 `params` 传入。
- API 函数采用 `fetch` + 动作 + 资源的命名方式，例如 `fetchGetUserList`、`fetchUpdateUser`。
- 优先定义明确的请求和响应类型；只有在后端契约尚未稳定时才局部使用 `any`，不要扩散到公共类型。
- HTTP 层已经统一展示失败信息；业务 `catch` 通常只恢复状态，不重复调用 `ElMessage.error`。
- API 基础地址通过 `getApiUrl()`/HTTP 封装读取运行时配置，不在业务代码中硬编码域名或直接依赖生产环境变量。

## 路由、菜单与权限

- 菜单由后端 `sys_menu` 驱动。业务页面不要修改 `asyncRoutes.ts` 或 `routesAlias.ts`，除非任务明确涉及路由框架本身。
- 新增页面或权限点时，同步提供数据库 migration 中的菜单/权限记录。
- 按钮权限使用 `v-auth`，权限字符串必须与后端和 `sys_menu` 保持一致。
- 表格操作优先使用 `ArtButtonTable` 已有的 `add`、`edit`、`delete`、`view`、`more` 类型及图标。

## 页面与数据约定

- 列表页面优先使用现有 `useTable` 模式；分页参数为 `pageIndex`、`pageSize`。
- 固定枚举放在 `src/enums/`；后端动态枚举通过 `getEnumOptions()`/`getEnumLabel()` 获取，不重复实现缓存。
- 加载、空数据、禁用、错误和提交中状态必须完整，异步操作结束时可靠恢复状态。
- 修改公共组件时检查已有调用方，避免改变默认 props、事件或插槽语义。

## 样式与代码质量

- 保持现有 art-design-pro 视觉和布局密度，不引入脱离当前设计系统的页面风格。
- 优先使用已有设计 token 和工具类，不硬编码重复颜色、间距或层级值。
- 避免无意义的 `any`、重复状态和可由 computed 派生的状态；复杂逻辑下沉到 composable 或模块组件。

## 验证

从仓库根目录始终执行类型检查：

```bash
pnpm --dir web-admin exec vue-tsc --noEmit
```

对本次修改的 TypeScript、Vue 和 JavaScript 文件执行 ESLint，不要用全仓库自动修复清理无关历史格式问题：

```bash
pnpm --dir web-admin exec eslint <changed-files...>
```

涉及生产代码、Vite 配置、依赖或构建行为时还应执行 `pnpm --dir web-admin build`。如果完整构建或全量 lint 被既有基线问题阻断，保留错误证据并在交付说明中明确指出，不得将其误报为本次改动通过。
