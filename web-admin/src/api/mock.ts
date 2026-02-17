/**
 * 本地 Mock API
 * 用于 demo/示例页面，直接返回本地静态数据，不走后端接口。
 * 模拟分页、搜索等行为，使 useTable 等 hook 能正常工作。
 */

import { ACCOUNT_TABLE_DATA, type User } from '@/mock/temp/formData'

/**
 * 模拟获取用户列表（分页）
 * 字段映射：将 mock 数据字段转为 demo 表格列期望的字段名
 */
export function fetchMockUserList(params: {
    current?: number
    size?: number
    userName?: string
    userPhone?: string
    userEmail?: string
    [key: string]: any
}) {
    return new Promise<{
        records: any[]
        total: number
        current: number
        size: number
    }>((resolve) => {
        const { current = 1, size = 20, userName, userPhone, userEmail } = params

        // 模拟搜索过滤
        let filtered = [...ACCOUNT_TABLE_DATA]
        if (userName) {
            filtered = filtered.filter((u) => u.username.toLowerCase().includes(userName.toLowerCase()))
        }
        if (userPhone) {
            filtered = filtered.filter((u) => u.mobile.includes(userPhone))
        }
        if (userEmail) {
            filtered = filtered.filter((u) => u.email.toLowerCase().includes(userEmail.toLowerCase()))
        }

        const total = filtered.length
        const start = (current - 1) * size
        const paged = filtered.slice(start, start + size)

        // 映射字段名，使其与 demo 表格列定义匹配
        const records = paged.map((u) => ({
            id: u.id,
            nickName: u.username,
            userName: u.username,
            userGender: u.gender === 1 ? '男' : '女',
            userPhone: u.mobile,
            userEmail: u.email,
            department: u.dep,
            status: u.status,
            createTime: u.create_time,
            avatar: u.avatar
        }))

        // 模拟网络延迟 200ms
        setTimeout(() => {
            resolve({ records, total, current, size })
        }, 200)
    })
}
