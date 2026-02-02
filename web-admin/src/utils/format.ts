/**
 * 通用格式化工具函数
 *
 * @module utils/format
 * @author Art Design Pro Team
 */

/**
 * 格式化日期时间字符串
 * @param dateStr 日期字符串，支持 ISO 格式或常见时间格式
 * @param format 格式化模式，默认 'YYYY-MM-DD HH:mm'
 * @returns 格式化后的字符串，无效日期返回 '-'
 *
 * @example
 * formatDateTime('2024-01-17T10:30:45.123Z')  // '2024-01-17 10:30'
 * formatDateTime('2024-01-17 10:30:45', 'YYYY/MM/DD')  // '2024/01/17'
 * formatDateTime('2024-01-17 10:30:45', 'MM-DD HH:mm')  // '01-17 10:30'
 */
export function formatDateTime(
    dateStr: string | Date | null | undefined,
    format: string = 'YYYY-MM-DD HH:mm'
): string {
    if (!dateStr) return '-'

    try {
        const date = typeof dateStr === 'string' ? new Date(dateStr) : dateStr

        // 检查是否为有效日期
        if (isNaN(date.getTime())) return '-'

        const year = date.getFullYear()
        const month = String(date.getMonth() + 1).padStart(2, '0')
        const day = String(date.getDate()).padStart(2, '0')
        const hours = String(date.getHours()).padStart(2, '0')
        const minutes = String(date.getMinutes()).padStart(2, '0')
        const seconds = String(date.getSeconds()).padStart(2, '0')

        return format
            .replace('YYYY', String(year))
            .replace('MM', month)
            .replace('DD', day)
            .replace('HH', hours)
            .replace('mm', minutes)
            .replace('ss', seconds)
    } catch {
        return '-'
    }
}

/**
 * 格式化日期（不含时间）
 * @param dateStr 日期字符串
 * @returns 格式化后的日期字符串 YYYY-MM-DD
 */
export function formatDate(dateStr: string | Date | null | undefined): string {
    return formatDateTime(dateStr, 'YYYY-MM-DD')
}

/**
 * 相对时间格式化（如：刚刚、5分钟前、昨天等）
 * @param dateStr 日期字符串
 * @returns 相对时间描述
 */
export function formatRelativeTime(dateStr: string | Date | null | undefined): string {
    if (!dateStr) return '-'

    try {
        const date = typeof dateStr === 'string' ? new Date(dateStr) : dateStr
        if (isNaN(date.getTime())) return '-'

        const now = new Date()
        const diff = now.getTime() - date.getTime()

        const seconds = Math.floor(diff / 1000)
        const minutes = Math.floor(seconds / 60)
        const hours = Math.floor(minutes / 60)
        const days = Math.floor(hours / 24)

        if (seconds < 60) return '刚刚'
        if (minutes < 60) return `${minutes}分钟前`
        if (hours < 24) return `${hours}小时前`
        if (days === 1) return '昨天'
        if (days < 7) return `${days}天前`
        if (days < 30) return `${Math.floor(days / 7)}周前`

        // 超过30天显示具体日期
        return formatDateTime(date, 'YYYY-MM-DD HH:mm')
    } catch {
        return '-'
    }
}
