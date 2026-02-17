# CI/CD

Art Admin 使用 **GitHub Actions** 实现自动化构建和部署。

## 工作流概览

```
git push → GitHub Actions → Build → Push Image → Deploy to K8s
```

## 后端 CI/CD

```yaml
name: Build & Deploy API

on:
  push:
    branches: [main]
    paths: ['backend/**']

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Build
        run: dotnet build backend/Art.sln -c Release

      - name: Login to GHCR
        uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Build & Push Image
        uses: docker/build-push-action@v5
        with:
          context: ./backend
          push: true
          tags: ghcr.io/${{ github.repository_owner }}/art-api:latest

      - name: Deploy to K8s
        run: |
          kubectl set image deployment/art-api \
            art-api=ghcr.io/${{ github.repository_owner }}/art-api:latest \
            -n apps
```

## 前端 CI/CD

```yaml
name: Build & Deploy Admin

on:
  push:
    branches: [main]
    paths: ['web-admin/**']

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '22'

      - name: Install pnpm
        uses: pnpm/action-setup@v4
        with:
          version: latest

      - name: Install & Build
        working-directory: ./web-admin
        run: |
          pnpm install --frozen-lockfile
          pnpm vite build

      - name: Build & Push Image
        uses: docker/build-push-action@v5
        with:
          context: ./web-admin
          push: true
          tags: ghcr.io/${{ github.repository_owner }}/art-web-admin:latest
```

## 镜像仓库

项目使用 **GitHub Container Registry (GHCR)** 存储镜像：

```
ghcr.io/rain7788/art-api:latest
ghcr.io/rain7788/art-web-admin:latest
```

## 环境分离

| 环境 | 分支 | 配置文件 |
| --- | --- | --- |
| 开发 | `dev` | `appsettings.json` |
| 生产 | `main` | `appsettings.Production.json` |

生产环境的连接字符串通过 K8s Secret 注入，不在代码仓库中存储。

## 数据库迁移

数据库迁移**不**在 CI/CD 中自动执行，需手动执行 SQL 文件：

```bash
mysql -h <host> -u root -p art < database/migrations/yyyyMMdd_xxx.sql
```

::: tip
建议使用独立的迁移 Job 或在部署前手动执行，避免自动迁移导致的数据风险。
:::
