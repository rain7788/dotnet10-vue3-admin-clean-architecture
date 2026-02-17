# deploy (GitOps)

本目录用于 Argo CD GitOps 部署（不分仓方案）。

## 安全原则

- **不把任何真实密码/PAT/密钥提交到 Git**。
- 数据库连接串等敏感配置仅存在集群 `Secret` 中（通过 `kubectl`/SSH 创建）。

## 组件

- `apps/`：应用清单（Namespace、MySQL、Redis、API、Web、Ingress）

## 依赖（本项目约定）

- 命名空间：`apps`
- 镜像：
  - `ghcr.io/rain7788/art-api:{latest|sha-...}`
  - `ghcr.io/rain7788/art-web-admin:{latest|sha-...}`
- Ingress：Traefik
- 证书：cert-manager + `ClusterIssuer/letsencrypt-prod`

## 集群侧 Secrets（不在 Git）

本仓库清单假设集群里已存在：

- `Secret/mysql-secret`（键：`MYSQL_ROOT_PASSWORD`、`MYSQL_DATABASE`）
- `Secret/art-api-conn`（键：`ConnectionStrings__DefaultConnection`、`ConnectionStrings__Redis`）

你可以用：

```bash
kubectl -n apps get secret mysql-secret art-api-conn
```

## Argo CD

Argo CD Application 会指向本仓库的 `deploy/apps` 路径。
