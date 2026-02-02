# ============================================
# Art Web - Vue 3 + Nginx 多阶段构建
# ============================================

# Stage 1: 构建阶段
FROM node:22-alpine AS build
WORKDIR /app

# 安装 pnpm
RUN corepack enable && corepack prepare pnpm@latest --activate

# 复制依赖文件（利用缓存层）
COPY package.json pnpm-lock.yaml ./

# 安装依赖
RUN pnpm install --frozen-lockfile

# 复制源代码
COPY . .

# 构建生产版本（跳过类型检查，直接构建）
RUN pnpm vite build

# Stage 2: 运行阶段（Nginx Alpine）
FROM nginx:alpine AS runtime

# 删除默认配置
RUN rm -rf /etc/nginx/conf.d/default.conf

# 复制自定义 Nginx 配置
COPY nginx.conf /etc/nginx/conf.d/default.conf

# 复制构建产物
COPY --from=build /app/dist /usr/share/nginx/html

# 复制 entrypoint 脚本
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

# 暴露端口
EXPOSE 80

# 健康检查
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:80/ || exit 1

# 使用自定义 entrypoint（注入环境变量）
ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]
