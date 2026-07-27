# ============================================
# Art API - .NET 10 多阶段构建
# ============================================

# Stage 1: 构建阶段
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# 复制生产项目的 csproj 文件，利用 Docker 缓存层
COPY Art.Api/Art.Api.csproj Art.Api/
COPY Art.Core/Art.Core.csproj Art.Core/
COPY Art.Domain/Art.Domain.csproj Art.Domain/
COPY Art.Infra/Art.Infra.csproj Art.Infra/

# 只还原 API 发布所需依赖，测试项目不参与生产镜像构建
RUN dotnet restore Art.Api/Art.Api.csproj

# 复制所有源代码
COPY . .

# 发布 Release 版本
RUN dotnet publish Art.Api/Art.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:PublishTrimmed=false \
    /p:PublishSingleFile=false

# Stage 2: 运行阶段（使用 Alpine 最小镜像）
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# 安装时区数据（可选，如需要时区支持）
RUN apk add --no-cache tzdata

# 设置时区为上海
ENV TZ=Asia/Shanghai

# 绑定到所有网络接口（容器内必须，否则外部无法访问）
ENV ASPNETCORE_URLS=http://+:80

# 创建非 root 用户运行应用
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

# 复制发布文件
COPY --from=build /app/publish .

# 切换到非 root 用户
USER appuser

# 暴露端口
EXPOSE 80

# 健康检查
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:80/common/health || exit 1

# 启动应用
ENTRYPOINT ["dotnet", "Art.Api.dll"]
