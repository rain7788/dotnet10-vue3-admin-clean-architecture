# Permission Control

Art Admin uses the `v-auth` directive for button-level permission control. Permission identifiers are managed in the `sys_menu` table's `auth_mark` field.

## v-auth Directive

```vue
<template>
  <ElButton v-auth="'system:user:add'">Add User</ElButton>
  <ElButton v-auth="'system:user:edit'">Edit</ElButton>
  <ElButton v-auth="'system:role:delete'" type="danger">Delete Role</ElButton>
</template>
```

### How It Works

```ts
function checkAuthPermission(el: HTMLElement, binding: AuthBinding): void {
  const authList = router.currentRoute.value.meta.authList || []
  const hasPermission = authList.some(
    (item) => item.authMark === binding.value
  )
  // No permission → remove element from DOM
  if (!hasPermission)
    el.parentNode?.removeChild(el)
}
```

The permission list comes from route meta (`route.meta.authList`), provided by the backend menu API.

## useAuth Hook

For programmatic permission checks:

```ts
const { hasAuth } = useAuth()

if (hasAuth('system:user:edit')) {
  // Has edit permission
}

const showEditButton = computed(() => hasAuth('system:user:edit'))
```

## Permission Identifier Format

Pattern: `module:resource:action`

```
system:user:add        # System - User - Add
system:user:edit       # System - User - Edit
system:role:assign     # System - Role - Assign permissions
order:list:export      # Order - List - Export
```

## Database Configuration

Permission identifiers are stored in the `sys_menu` table:

```sql
INSERT INTO sys_menu (id, parent_id, name, type, auth_mark)
VALUES (1001, 100, 'Add User', 3, 'system:user:add');
-- type = 3 means button-level permission
```
