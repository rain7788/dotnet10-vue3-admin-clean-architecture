# Kubernetes 部署

Art Admin 提供完整的 K8s 部署清单，使用 Namespace 隔离 + Secret 管理敏感配置。

## 部署架构

```
Ingress (Host 路由)
├── admin.aftbay.com → art-web-admin Service → Nginx Pod
└── api.aftbay.com   → art-api Service       → .NET Pod
```

## 资源文件

```
deploy/apps/
├── 00-namespace.yaml      # 命名空间
├── 20-art-api.yaml         # 后端 API
├── 21-art-web-admin.yaml   # 前端 Admin
└── 30-ingress.yaml         # Ingress 路由
```

## 后端 Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: art-api
  namespace: apps
spec:
  replicas: 1
  selector:
    matchLabels:
      app: art-api
  template:
    spec:
      containers:
        - name: art-api
          image: ghcr.io/rain7788/art-api:latest
          ports:
            - containerPort: 80
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: Production
            - name: ConnectionStrings__DefaultConnection
              valueFrom:
                secretKeyRef:
                  name: art-api-conn
                  key: ConnectionStrings__DefaultConnection
            - name: ConnectionStrings__Redis
              valueFrom:
                secretKeyRef:
                  name: art-api-conn
                  key: ConnectionStrings__Redis
          readinessProbe:
            tcpSocket:
              port: 80
            initialDelaySeconds: 10
          livenessProbe:
            tcpSocket:
              port: 80
            initialDelaySeconds: 30
          resources:
            requests:
              cpu: 50m
              memory: 128Mi
            limits:
              cpu: 300m
              memory: 512Mi
```

## 创建 Secret

```bash
kubectl create secret generic art-api-conn -n apps \
  --from-literal=ConnectionStrings__DefaultConnection="server=mysql;..." \
  --from-literal=ConnectionStrings__Redis="redis:6379"
```

## 部署步骤

```bash
# 1. 创建命名空间
kubectl apply -f deploy/apps/00-namespace.yaml

# 2. 创建 Secret（连接字符串）
kubectl create secret generic art-api-conn -n apps ...

# 3. 部署后端 + 前端
kubectl apply -f deploy/apps/20-art-api.yaml
kubectl apply -f deploy/apps/21-art-web-admin.yaml

# 4. 配置 Ingress
kubectl apply -f deploy/apps/30-ingress.yaml
```

## 多 Pod 注意事项

当 `replicas > 1` 时：

- **定时任务** — 通过分布式锁确保只有一个 Pod 执行
- **消息队列消费** — `BRPOP` 原子性保证不会重复消费
- **延迟队列** — `ZREM` 原子操作，不会重复处理
- **WorkerId** — 每个 Pod 应有不同的雪花 ID WorkerId

## 滚动更新

```bash
# 更新镜像触发滚动更新
kubectl set image deployment/art-api art-api=ghcr.io/rain7788/art-api:v2.0 -n apps
```
