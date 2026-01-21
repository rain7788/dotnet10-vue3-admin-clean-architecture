#!/bin/sh
set -e

CONFIG_FILE="/usr/share/nginx/html/config.js"

# 使用 envsubst 替换占位符（VITE_API_URL 默认为空，使用构建时的值）
export VITE_API_URL="${VITE_API_URL:-}"

if [ -f "$CONFIG_FILE" ]; then
    envsubst < "$CONFIG_FILE" > "$CONFIG_FILE.tmp"
    mv "$CONFIG_FILE.tmp" "$CONFIG_FILE"
    echo "✅ VITE_API_URL: ${VITE_API_URL:-<使用构建时默认值>}"
fi

exec "$@"
