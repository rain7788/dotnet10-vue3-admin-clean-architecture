# Kubernetes Deployment

Art Admin provides complete K8s manifests with Namespace isolation and Secret management.

## Architecture

```
Ingress (Host routing)
├── admin.aftbay.com → art-web-admin Service → Nginx Pod
└── api.aftbay.com   → art-api Service       → .NET Pod
```

## Manifest Files

```
deploy/apps/
├── 00-namespace.yaml      # Namespace
├── 20-art-api.yaml         # Backend API
├── 21-art-web-admin.yaml   # Frontend Admin
└── 30-ingress.yaml         # Ingress routing
```

## Backend Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: art-api
  namespace: apps
spec:
  replicas: 1
  template:
    spec:
      containers:
        - name: art-api
          image: ghcr.io/rain7788/art-api:latest
          ports:
            - containerPort: 80
          env:
            - name: ConnectionStrings__DefaultConnection
              valueFrom:
                secretKeyRef:
                  name: art-api-conn
                  key: ConnectionStrings__DefaultConnection
          resources:
            requests: { cpu: 50m, memory: 128Mi }
            limits: { cpu: 300m, memory: 512Mi }
```

## Deploy Steps

```bash
# 1. Create namespace
kubectl apply -f deploy/apps/00-namespace.yaml

# 2. Create secrets
kubectl create secret generic art-api-conn -n apps \
  --from-literal=ConnectionStrings__DefaultConnection="server=mysql;..." \
  --from-literal=ConnectionStrings__Redis="redis:6379"

# 3. Deploy services
kubectl apply -f deploy/apps/

# 4. Verify
kubectl get pods -n apps
```

## Multi-Pod Considerations

When `replicas > 1`:

- **Scheduled tasks** — Distributed lock ensures single-Pod execution
- **Message queue** — `BRPOP` atomicity prevents duplicate consumption
- **Delay queue** — `ZREM` atomic operation prevents duplicate processing
- **WorkerId** — Each Pod should have a unique Snowflake ID WorkerId

## Rolling Updates

```bash
kubectl set image deployment/art-api \
  art-api=ghcr.io/rain7788/art-api:v2.0 -n apps
```
