# Database Schema

Art Admin uses **MySQL 8.0** with 9 core tables. Primary keys use `BIGINT` Snowflake IDs (except Token tables).

## Table Overview

| Table | Description | PK Strategy |
| --- | --- | --- |
| `sys_user` | System users | Snowflake ID |
| `sys_role` | System roles | Snowflake ID |
| `sys_menu` | System menus | Snowflake ID |
| `sys_permission` | Permission identifiers | Snowflake ID |
| `sys_user_role` | User-Role mapping | Composite PK |
| `sys_role_menu` | Role-Menu mapping | Composite PK |
| `sys_role_permission` | Role-Permission mapping | Composite PK |
| `token_access` | Access tokens | Auto-increment |
| `token_refresh` | Refresh tokens | Auto-increment |

## Core Tables

### sys_user

```sql
CREATE TABLE `sys_user` (
  `id` bigint NOT NULL COMMENT 'Primary key (Snowflake ID)',
  `username` varchar(50) NOT NULL,
  `password` varchar(1024) NOT NULL COMMENT 'Encrypted',
  `real_name` varchar(50) DEFAULT NULL,
  `is_super` tinyint(1) NOT NULL DEFAULT 0,
  `phone` varchar(20) DEFAULT NULL,
  `avatar` varchar(200) DEFAULT NULL,
  `status` int NOT NULL DEFAULT 1 COMMENT '0=disabled, 1=active',
  `last_login_time` datetime(6) DEFAULT NULL,
  `last_active_time` datetime(6) DEFAULT NULL,
  `created_time` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`id`),
  UNIQUE KEY `idx_sys_user_username` (`username`)
) ENGINE=InnoDB;
```

### sys_menu

```sql
CREATE TABLE `sys_menu` (
  `id` BIGINT NOT NULL,
  `parent_id` BIGINT NULL,
  `name` VARCHAR(50) NOT NULL,
  `code` VARCHAR(50) NOT NULL COMMENT 'Menu code / permission identifier',
  `path` VARCHAR(200) NULL COMMENT 'Route path',
  `component` VARCHAR(200) NULL COMMENT 'Component path',
  `icon` VARCHAR(100) NULL,
  `sort` INT NOT NULL DEFAULT 0,
  `is_visible` TINYINT(1) NOT NULL DEFAULT 1,
  `keep_alive` TINYINT(1) NOT NULL DEFAULT 1,
  `status` INT NOT NULL DEFAULT 1,
  `created_time` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`id`)
) ENGINE=InnoDB;
```

### RBAC Junction Tables

```sql
-- User-Role (many-to-many)
CREATE TABLE `sys_user_role` (
  `user_id` BIGINT NOT NULL,
  `role_id` BIGINT NOT NULL,
  PRIMARY KEY (`user_id`, `role_id`)
);

-- Role-Menu (many-to-many)
CREATE TABLE `sys_role_menu` (
  `role_id` BIGINT NOT NULL,
  `menu_id` BIGINT NOT NULL,
  PRIMARY KEY (`role_id`, `menu_id`)
);

-- Role-Permission (many-to-many)
CREATE TABLE `sys_role_permission` (
  `role_id` BIGINT NOT NULL,
  `permission_id` BIGINT NOT NULL,
  PRIMARY KEY (`role_id`, `permission_id`)
);
```

## Design Principles

### No Foreign Key Constraints

::: warning
The database uses **no foreign key constraints**. Relationships are declared only in EF Core via `[ForeignKey]` for navigation properties.

Benefits:
- Flexible migrations, no FK ordering issues
- No cascading deletes/updates
- Better performance (less lock contention)
:::

### Snowflake IDs

All business entity PKs use `BIGINT` + Snowflake ID (`IdGen.NextId()`). Token tables use auto-increment.

### Time Precision

All timestamp fields use `datetime(6)` for microsecond precision.

### Snake Case Naming

EF Core is configured with snake_case naming. C# `PascalCase` properties map to `snake_case` columns automatically.

## File Layout

```
database/
├── schemas/
│   └── 01_core_tables.sql    # Complete schema
├── seeds/
│   └── 01_sys_user.sql       # Initial data
└── migrations/
    └── yyyyMMdd_desc.sql     # Incremental migrations
```
