/**
 * 系统模块枚举定义
 *
 * ## 主要功能
 *
 * - 日志级别枚举（Information, Warning, Error, Debug, Fatal）
 * - 日志级别显示映射
 * - 日志级别样式映射
 *
 * @module enums/systemEnum
 * @author Art Admin Team
 */

/**
 * 日志级别选项
 */
export const LogLevelOptions = [
  { label: '全部', value: '' },
  { label: '信息', value: 'Information' },
  { label: '调试', value: 'Debug' },
  { label: '警告', value: 'Warning' },
  { label: '错误', value: 'Error' },
  { label: '严重', value: 'Fatal' }
]

/**
 * 日志级别显示文本映射
 */
export const LogLevelDisplayMap: Record<string, string> = {
  Information: '信息',
  Debug: '调试',
  Warning: '警告',
  Error: '错误',
  Fatal: '严重'
}

/**
 * 日志级别标签类型映射
 */
export const LogLevelTypeMap: Record<string, string> = {
  Information: 'primary',
  Debug: 'info',
  Warning: 'warning',
  Error: 'danger',
  Fatal: 'danger'
}

/**
 * HTTP 状态码选项
 */
export const StatusCodeOptions = [
  { label: '全部', value: '' },
  { label: '空', value: 0 },
  { label: '200 成功', value: 200 },
  { label: '400 请求错误', value: 400 },
  { label: '401 未授权', value: 401 },
  { label: '403 禁止访问', value: 403 },
  { label: '404 未找到', value: 404 },
  { label: '500 服务器错误', value: 500 }
]

/**
 * 获取 HTTP 状态码对应的标签类型
 * @param statusCode HTTP 状态码
 * @returns Element Plus 标签类型
 */
export function getStatusCodeType(
  statusCode: number | null | undefined
): 'success' | 'warning' | 'danger' | 'info' {
  if (!statusCode) return 'info'
  if (statusCode >= 200 && statusCode < 300) return 'success'
  if (statusCode >= 400 && statusCode < 500) return 'warning'
  if (statusCode >= 500) return 'danger'
  return 'info'
}

/**
 * 获取 HTTP 请求方法对应的标签类型
 * @param method HTTP 方法
 * @returns Element Plus 标签类型
 */
export function getMethodType(
  method: string | null | undefined
): 'primary' | 'success' | 'warning' | 'danger' | 'info' {
  switch (method?.toUpperCase()) {
    case 'GET':
      return 'primary'
    case 'POST':
      return 'success'
    case 'PUT':
      return 'warning'
    case 'DELETE':
      return 'danger'
    default:
      return 'info'
  }
}
