/**
 * 枚举字典工具
 *
 * 提供枚举选项的动态加载与缓存机制：
 * - 按需加载：首次使用时请求接口
 * - sessionStorage 缓存：窗口内有效，刷新页面重新加载
 * - 统一接口：GET /common/dict/enums?name=枚举名
 */

import request from '@/utils/http'

/** 枚举选项类型 */
export interface EnumOption {
    /** 枚举值（数值） */
    value: number
    /** 显示标签 */
    label: string
}

/** 缓存 key 前缀 */
const CACHE_PREFIX = 'enum_dict_'

/** 内存缓存（避免重复请求） */
const memoryCache = new Map<string, EnumOption[]>()

/** 正在加载的 Promise 缓存（防止并发重复请求） */
const pendingRequests = new Map<string, Promise<EnumOption[]>>()

/**
 * 获取枚举选项列表
 * @param enumName 枚举名称，如 ActiveStatus
 * @returns 枚举选项数组
 */
export async function getEnumOptions(enumName: string): Promise<EnumOption[]> {
    // 1. 优先从内存缓存读取
    if (memoryCache.has(enumName)) {
        return memoryCache.get(enumName)!
    }

    // 2. 从 sessionStorage 读取
    const cacheKey = CACHE_PREFIX + enumName
    const cached = sessionStorage.getItem(cacheKey)
    if (cached) {
        try {
            const options = JSON.parse(cached) as EnumOption[]
            memoryCache.set(enumName, options)
            return options
        } catch {
            sessionStorage.removeItem(cacheKey)
        }
    }

    // 3. 防止并发重复请求
    if (pendingRequests.has(enumName)) {
        return pendingRequests.get(enumName)!
    }

    // 4. 请求接口
    const requestPromise = fetchEnumOptions(enumName)
    pendingRequests.set(enumName, requestPromise)

    try {
        const options = await requestPromise
        // 缓存到 sessionStorage 和内存
        sessionStorage.setItem(cacheKey, JSON.stringify(options))
        memoryCache.set(enumName, options)
        return options
    } finally {
        pendingRequests.delete(enumName)
    }
}

/**
 * 请求枚举选项接口
 */
async function fetchEnumOptions(enumName: string): Promise<EnumOption[]> {
    try {
        const res = await request.get<EnumOption[]>({
            url: '/common/dict/enums',
            params: { name: enumName }
        })
        return res || []
    } catch (error) {
        console.error(`获取枚举 ${enumName} 失败:`, error)
        return []
    }
}

/**
 * 清除指定枚举的缓存
 * @param enumName 枚举名称，不传则清除所有
 */
export function clearEnumCache(enumName?: string): void {
    if (enumName) {
        memoryCache.delete(enumName)
        sessionStorage.removeItem(CACHE_PREFIX + enumName)
    } else {
        memoryCache.clear()
        // 清除所有枚举缓存
        Object.keys(sessionStorage)
            .filter(key => key.startsWith(CACHE_PREFIX))
            .forEach(key => sessionStorage.removeItem(key))
    }
}

/**
 * 根据 value 获取 label
 * @param options 枚举选项数组
 * @param value 枚举值（数值）
 * @returns 显示标签
 */
export function getEnumLabel(options: EnumOption[], value: number | string): string {
    const numValue = typeof value === 'string' ? parseInt(value, 10) : value
    return options.find(opt => opt.value === numValue)?.label || String(value)
}
