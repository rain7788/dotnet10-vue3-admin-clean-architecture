# API Integration

All API requests are centralized in `src/api/`. Pages must not contain inline requests.

## Naming Convention

**`fetch` + Action + Resource**:

```ts
import request from '@/utils/http'

// List query — POST
export function fetchGetUserList(params: any) {
  return request.post<any>({ url: '/admin/system/user/list', params })
}

// Create/Update — POST with data
export function fetchUpdateUser(data: any) {
  return request.post<any>({ url: '/admin/system/user/update', data })
}

// Delete — DELETE
export function fetchDeleteUser(id: string) {
  return request.del<any>({ url: `/admin/system/user/${id}` })
}

// GET query
export function fetchGetRoleSelect() {
  return request.get<any[]>({ url: '/admin/system/role/select' })
}
```

## Typed APIs

```ts
export interface SysLogListRequest {
  queryDate: string
  level?: string
  pageIndex?: number
  pageSize?: number
}

export interface SysLogListResponse {
  total: number
  data: SysLogItem[]
}

export function fetchLogList(params: SysLogListRequest) {
  return request.post<SysLogListResponse>({
    url: '/admin/system/log/list',
    params
  })
}
```

::: tip Types Can Use `any`
During rapid development, `any` is acceptable to avoid blocking:
```ts
export function fetchGetUserList(params: any) {
  return request.post<any>({ url: '/admin/system/user/list', params })
}
```
:::

## URL Convention

```
/admin/module/resource/action
│       │       │        │
│       │       │        └── Action (list/save/...)
│       │       └────────── Resource name
│       └────────────────── Business module
└────────────────────────── Prefix (admin/app/common)
```
