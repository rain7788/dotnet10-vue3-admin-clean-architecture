/**
 * 应用运行时配置
 * 优先从 window.__APP_CONFIG__ 读取（容器部署时注入）
 * 回退到 import.meta.env（本地开发时使用）
 */

declare global {
    interface Window {
        __APP_CONFIG__?: { VITE_API_URL?: string }
    }
}

/**
 * 获取 API 基础地址
 */
export function getApiUrl(): string {
    const runtimeValue = window.__APP_CONFIG__?.VITE_API_URL
    // 如果运行时配置存在且不是未替换的占位符，则使用运行时配置
    if (runtimeValue && !runtimeValue.startsWith('${')) {
        return runtimeValue
    }
    // 回退到 Vite 构建时环境变量
    return import.meta.env.VITE_API_URL || ''
}
