import request from '@/utils/http'

// ===================== 系统日志管理 =====================

export interface SysLogListRequest {
  /** 查询日期（必填） */
  queryDate: string
  /** 日志级别 */
  level?: string
  /** 开始时间 */
  startTime?: string
  /** 结束时间 */
  endTime?: string
  /** 关键字搜索 */
  keyword?: string
  /** 请求路径 */
  requestPath?: string
  /** 用户ID */
  userId?: string
  /** IP地址 */
  ipAddress?: string
  /** 状态码 */
  statusCode?: number
  /** 页码 */
  pageIndex?: number
  /** 每页数量 */
  pageSize?: number
}

export interface SysLogItem {
  id: number
  timestamp: string
  level: string
  message: string
  exception?: string
  requestPath?: string
  requestMethod?: string
  statusCode?: number
  requestId?: string
  userId?: string
  userName?: string
  ipAddress?: string
  elapsed?: number
  request?: string
  response?: string
}

export interface SysLogListResponse {
  total: number
  page: number
  pageSize: number
  data: SysLogItem[]
}

/**
 * 获取日志列表
 */
export function fetchLogList(params: SysLogListRequest) {
  return request.post<SysLogListResponse>({
    url: '/admin/system/log/list',
    data: params
  })
}
