import request from '@/utils/http'
import { AppRouteRecord } from '@/types/router'

// ===================== 用户管理 =====================

// 获取用户列表
export function fetchGetUserList(params: any) {
  return request.post<any>({
    url: '/admin/system/user/list',
    params
  })
}

// 新增/更新用户
export function fetchUpdateUser(data: any) {
  return request.post<any>({
    url: '/admin/system/user/update',
    data
  })
}

// 删除用户
export function fetchDeleteUser(id: string) {
  return request.del<any>({
    url: `/admin/system/user/${id}`
  })
}

// ===================== 角色管理 =====================

// 获取角色列表
export function fetchGetRoleList(params: any) {
  return request.post<any>({
    url: '/admin/system/role/list',
    params
  })
}

// 获取角色选择列表
export function fetchGetRoleSelect() {
  return request.get<any[]>({
    url: '/admin/system/role/select'
  })
}

// 新增/更新角色
export function fetchUpdateRole(data: any) {
  return request.post<any>({
    url: '/admin/system/role/update',
    data
  })
}

// 删除角色
export function fetchDeleteRole(id: string) {
  return request.del<any>({
    url: `/admin/system/role/${id}`
  })
}

// ===================== 菜单管理 =====================

// 获取菜单树（管理端）
export function fetchGetMenuTree() {
  return request.get<any[]>({
    url: '/admin/system/menu/tree'
  })
}

// 获取用户菜单列表
export function fetchGetMenuList() {
  return request.get<AppRouteRecord[]>({
    url: '/admin/system/menu/user'
  })
}

// 新增/更新菜单
export function fetchUpdateMenu(data: any) {
  return request.post<any>({
    url: '/admin/system/menu/update',
    data
  })
}

// 删除菜单
export function fetchDeleteMenu(id: string) {
  return request.del<any>({
    url: `/admin/system/menu/${id}`
  })
}

// 新增/更新权限
export function fetchUpdatePermission(data: any) {
  return request.post<any>({
    url: '/admin/system/menu/permission/update',
    data
  })
}

// 删除权限
export function fetchDeletePermission(id: string) {
  return request.del<any>({
    url: `/admin/system/menu/permission/${id}`
  })
}
