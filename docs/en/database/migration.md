# Database Migration

Art Admin uses **plain SQL files** for database changes, not EF Core Migrations.

## Three-Way Sync Principle

Every database change must update three places:

```
database/
├── schemas/01_core_tables.sql    # 1. Full schema (keep up-to-date)
├── seeds/01_sys_user.sql         # 2. Seed data (if changed)
└── migrations/yyyyMMdd_desc.sql  # 3. Incremental migration script
```

## Migration File Naming

Format: `yyyyMMdd_description.sql`

```
migrations/
├── 20260117_snowflake_id.sql
├── 20260217_add_delay_queue_demo_menu.sql
├── 20260217_add_distributed_lock_demo_menu.sql
├── 20260217_add_sys_user_last_active_time.sql
└── 20260217_restore_demo_menus.sql
```

## Migration Examples

### Adding a Menu

```sql
INSERT INTO `sys_menu` (
  `id`, `parent_id`, `name`, `code`, `path`,
  `component`, `icon`, `sort`, `is_visible`, `status`
)
VALUES (
  1891234567890123459, 1891234567890123456,
  'Delay Queue', 'DelayQueue', '/demo/delay-queue',
  '/examples/delay-queue/index', 'ri:timer-line',
  3, 1, 1
);
```

### Adding a Column

```sql
ALTER TABLE `sys_user`
ADD COLUMN `last_active_time` datetime(6) DEFAULT NULL
COMMENT 'Last active time' AFTER `last_login_time`;
```

## New Pages Require Menu Records

::: warning Important
When adding new pages, you **must** insert `sys_menu` records in `database/migrations/`. The frontend uses backend routing mode — menus come from the database.
:::

## Running Migrations

```bash
mysql -h localhost -P 3306 -u root -p aaaaaa art < database/migrations/20260217_xxx.sql
```

## Guidelines

1. **No foreign keys** — never add `FOREIGN KEY` constraints in migration scripts
2. **Idempotent** — use `IF NOT EXISTS` or `INSERT IGNORE` for re-runnability
3. **Schema sync** — always update `schemas/01_core_tables.sql` after writing migrations
4. **Seeds sync** — update `seeds/` files if initial data changes
