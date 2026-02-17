# Docker 部署

Art Admin 采用**多阶段构建**，分别打包后端 API 和前端 Admin。

## 后端 Docker

```dockerfile
# Stage 1: 构建
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY Art.sln .
COPY Art.Api/Art.Api.csproj Art.Api/
COPY Art.Core/Art.Core.csproj Art.Core/
COPY Art.Domain/Art.Domain.csproj Art.Domain/
COPY Art.Infra/Art.Infra.csproj Art.Infra/

RUN dotnet restore
COPY . .
RUN dotnet publish Art.Api/Art.Api.csproj -c Release -o /app/publish

# Stage 2: 运行（Alpine 最小镜像）
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
ENV TZ=Asia/Shanghai
ENV ASPNETCORE_URLS=http://+:80

COPY --from=build /app/publish .
EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "Art.Api.dll"]
```

### 构建 & 运行

```bash
# 构建镜像
cd backend
docker build -t art-api:latest .

# 运行（注入连接字符串）
docker run -d \
  -p 5055:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="server=host.docker.internal;..." \
  -e ConnectionStrings__Redis="host.docker.internal:6379" \
  --name art-api \
  art-api:latest
```

## 前端 Docker

```dockerfile
# Stage 1: 构建
FROM node:22-alpine AS build
WORKDIR /app
RUN corepack enable && corepack prepare pnpm@latest --activate
COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY . .
RUN pnpm vite build

# Stage 2: Nginx 运行
FROM nginx:alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh
EXPOSE 80
ENTRYPOINT ["/docker-entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]
```

### 构建 & 运行

```bash
# 构建镜像
cd web-admin
docker build -t art-web-admin:latest .

# 运行
docker run -d -p 8080:80 --name art-web-admin art-web-admin:latest
```

## Docker Compose

```yaml
version: '3.8'
services:
  api:
    build: ./backend
    ports:
      - "5055:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=server=mysql;...
      - ConnectionStrings__Redis=redis:6379
    depends_on:
      - mysql
      - redis

  web-admin:
    build: ./web-admin
    ports:
      - "8080:80"
    depends_on:
      - api

  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: aaaaaa
      MYSQL_DATABASE: art
    volumes:
      - mysql-data:/var/lib/mysql
    ports:
      - "3306:3306"

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

volumes:
  mysql-data:
```

## 安全实践

- 使用非 root 用户运行应用
- 连接字符串通过环境变量或 K8s Secret 注入，不写入镜像
- 健康检查确保容器存活状态
- Alpine 基础镜像减小攻击面
