# Quick Start

## Requirements

| Dependency | Version |
| --- | --- |
| .NET SDK | 10.0+ |
| Node.js | 20+ |
| pnpm | 10+ |
| MySQL | 8.0+ |
| Redis | 6.0+ |

## 1. Clone the Project

```bash
git clone https://github.com/rain7788/dotnet10-vue3-admin-clean-architecture.git
cd dotnet10-vue3-admin-clean-architecture
```

## 2. Initialize Database

```bash
mysql -u root -p -e "CREATE DATABASE art DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
mysql -u root -p art < database/schemas/01_core_tables.sql
mysql -u root -p art < database/seeds/01_sys_user.sql

for f in database/migrations/*.sql; do
  mysql -u root -p art < "$f"
done
```

## 3. Start Backend

```bash
cd backend/Art.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

Backend runs at `http://localhost:5055`, Swagger UI at `http://localhost:5055/swagger`.

## 4. Start Frontend

```bash
cd web-admin
pnpm install
pnpm dev
```

Frontend runs at `http://localhost:5173`.

## 5. Login

Default admin credentials:

| Username | Password |
| --- | --- |
| `admin` | `123456` |
