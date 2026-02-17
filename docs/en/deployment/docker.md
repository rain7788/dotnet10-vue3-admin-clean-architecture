# Docker Deployment

Art Admin uses **multi-stage builds** for both backend API and frontend Admin.

## Backend Docker

```dockerfile
# Stage 1: Build
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

# Stage 2: Runtime (Alpine minimal)
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

### Build & Run

```bash
cd backend && docker build -t art-api:latest .

docker run -d -p 5055:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="server=host.docker.internal;..." \
  -e ConnectionStrings__Redis="host.docker.internal:6379" \
  --name art-api art-api:latest
```

## Frontend Docker

```dockerfile
FROM node:22-alpine AS build
WORKDIR /app
RUN corepack enable && corepack prepare pnpm@latest --activate
COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY . .
RUN pnpm vite build

FROM nginx:alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

### Build & Run

```bash
cd web-admin && docker build -t art-web-admin:latest .
docker run -d -p 8080:80 --name art-web-admin art-web-admin:latest
```

## Docker Compose

```yaml
version: '3.8'
services:
  api:
    build: ./backend
    ports: ["5055:80"]
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=server=mysql;...
      - ConnectionStrings__Redis=redis:6379
    depends_on: [mysql, redis]

  web-admin:
    build: ./web-admin
    ports: ["8080:80"]
    depends_on: [api]

  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: aaaaaa
      MYSQL_DATABASE: art
    volumes: [mysql-data:/var/lib/mysql]

  redis:
    image: redis:7-alpine

volumes:
  mysql-data:
```

## Security Practices

- Non-root user for running apps
- Connection strings via environment variables or K8s Secrets
- Health checks for container liveness
- Alpine base images for minimal attack surface
