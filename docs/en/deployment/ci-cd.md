# CI/CD

Art Admin uses **GitHub Actions** for automated build and deployment.

## Workflow Overview

```
git push → GitHub Actions → Build → Push Image → Deploy to K8s
```

## Backend CI/CD

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
```

## Frontend CI/CD

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

      - uses: actions/setup-node@v4
        with:
          node-version: '22'

      - uses: pnpm/action-setup@v4
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

## Image Registry

Images are stored in **GitHub Container Registry (GHCR)**:

```
ghcr.io/rain7788/art-api:latest
ghcr.io/rain7788/art-web-admin:latest
```

## Environment Separation

| Environment | Branch | Config |
| --- | --- | --- |
| Development | `dev` | `appsettings.json` |
| Production | `main` | `appsettings.Production.json` |

Production connection strings are injected via K8s Secrets, never stored in the repository.

## Database Migrations

Migrations are **not** auto-executed in CI/CD. Run SQL files manually:

```bash
mysql -h <host> -u root -p art < database/migrations/yyyyMMdd_xxx.sql
```
