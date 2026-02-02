/**
 * 运行时配置文件
 * 容器启动时会通过 envsubst 替换 ${VAR} 占位符
 */
window.__APP_CONFIG__ = {
    VITE_API_URL: '${VITE_API_URL}'
}
