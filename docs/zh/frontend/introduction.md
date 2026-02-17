# 前端概述

Art Admin 前端基于 **[art-design-pro](https://www.artd.pro)** 3.0.1 构建，是一套企业级后台管理模板。

## 技术栈

| 技术 | 版本 | 说明 |
| --- | --- | --- |
| Vue 3 | 3.5+ | `<script setup>` 组合式 API |
| Vite | 7.x | 极速构建工具 |
| Element Plus | 2.x | UI 组件库 |
| TailwindCSS | 4.x | 原子化 CSS |
| Pinia | 2.x | 状态管理 |
| Axios | 1.x | HTTP 客户端 |
| TypeScript | 5.x | 类型系统 |

## 自动导入

以下 API 无需手动 `import`，由 `unplugin-auto-import` 自动处理：

```ts
// Vue 核心
ref, reactive, computed, watch, watchEffect
onMounted, onUnmounted, nextTick

// Vue Router
useRouter, useRoute

// VueUse
useStorage, useDebounceFn, useThrottleFn
```

## 路径别名

| 别名 | 实际路径 |
| --- | --- |
| `@` | `src/` |
| `@views` | `src/views/` |

```ts
import request from '@/utils/http'
import { fetchGetUserList } from '@/api/system-manage'
```

## 目录结构

```
src/
├── api/            # API 封装
├── assets/         # 静态资源
├── components/     # 全局组件（ArtTable, ArtSearchBar...）
├── composables/    # 组合式函数
├── config/         # 应用配置
├── directives/     # 自定义指令（v-auth）
├── enums/          # 固定枚举
├── hooks/          # 核心 hooks（useTable, useAuth）
├── locales/        # 国际化
├── plugins/        # 插件注册
├── router/         # 路由配置
├── store/          # Pinia 状态管理
├── types/          # 类型定义
├── utils/          # 工具函数（http, dict...）
└── views/          # 页面视图
```

## 后端路由模式

::: warning 重要
Art Admin 使用**后端路由模式**——菜单由 `sys_menu` 表驱动，**禁止修改前端静态路由文件**（`asyncRoutes.ts`、`routesAlias.ts`）。新增页面必须在数据库 `sys_menu` 表中插入菜单记录。
:::

## 更多文档

art-design-pro 的通用功能（主题、布局、国际化等）请参考官方文档：

- [art-design-pro 文档](https://www.artd.pro)
- [GitHub 仓库](https://github.com/Jeremystu/art-design-pro)
