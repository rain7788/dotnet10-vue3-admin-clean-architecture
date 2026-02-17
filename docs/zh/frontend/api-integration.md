# API 封装

所有 API 请求统一封装在 `src/api/` 目录下，页面禁止直接写请求。

## 命名规范

**`fetch` + 动作 + 资源名**：

```ts
import request from '@/utils/http'

// 列表查询 — 使用 POST
export function fetchGetUserList(params: any) {
  return request.post<any>({ url: '/admin/system/user/list', params })
}

// 新增/编辑 — 使用 POST + data
export function fetchUpdateUser(data: any) {
  return request.post<any>({ url: '/admin/system/user/update', data })
}

// 删除 — 使用 DELETE
export function fetchDeleteUser(id: string) {
  return request.del<any>({ url: `/admin/system/user/${id}` })
}

// GET 查询
export function fetchGetRoleSelect() {
  return request.get<any[]>({ url: '/admin/system/role/select' })
}
```

## 带类型的 API

```ts
// 请求类型
export interface SysLogListRequest {
  queryDate: string
  level?: string
  pageIndex?: number
  pageSize?: number
}

// 响应类型
export interface SysLogListResponse {
  total: number
  data: SysLogItem[]
}

// API 函数
export function fetchLogList(params: SysLogListRequest) {
  return request.post<SysLogListResponse>({
    url: '/admin/system/log/list',
    params
  })
}
```

::: tip 类型可用 any
对接期间可以直接使用 `any` 类型，避免阻塞开发：
```ts
export function fetchGetUserList(params: any) {
  return request.post<any>({ url: '/admin/system/user/list', params })
}
```
:::

## 文件组织

```
src/api/
├── system-manage.ts    # 系统管理（用户、角色、菜单）
├── system-log.ts       # 系统日志
├── demo.ts             # 演示模块
└── ...
```

## URL 规则

API URL 结构遵循后端路由规范：

```
/admin/module/resource/action
│       │       │        │
│       │       │        └── 操作（list/save/...）
│       │       └────────── 资源名
│       └────────────────── 业务模块
└────────────────────────── 前缀（admin/app/common）
```
