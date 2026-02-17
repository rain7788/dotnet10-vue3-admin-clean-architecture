# 权限控制

Art Admin 使用 `v-auth` 指令实现按钮级权限控制，权限标识由后端 `sys_menu` 表的 `auth_mark` 字段管理。

## v-auth 指令

```vue
<template>
  <!-- 有权限才显示 -->
  <ElButton v-auth="'system:user:add'">新增用户</ElButton>
  <ElButton v-auth="'system:user:edit'">编辑</ElButton>
  <ElButton v-auth="'system:role:delete'" type="danger">删除角色</ElButton>
</template>
```

### 工作原理

```ts
function checkAuthPermission(el: HTMLElement, binding: AuthBinding): void {
  const authList = router.currentRoute.value.meta.authList || []
  const hasPermission = authList.some(
    (item) => item.authMark === binding.value
  )
  // 无权限 → 直接从 DOM 中移除元素
  if (!hasPermission)
    el.parentNode?.removeChild(el)
}
```

权限列表从路由元信息（`route.meta.authList`）中获取，由后端在菜单接口中返回。

## useAuth Hook

在 JS/TS 中判断权限：

```ts
const { hasAuth } = useAuth()

// 条件判断
if (hasAuth('system:user:edit')) {
  // 有编辑权限
}

// 动态渲染
const showEditButton = computed(() => hasAuth('system:user:edit'))
```

## 权限标识规范

格式：`模块:资源:动作`

```
system:user:add         # 系统-用户-新增
system:user:edit        # 系统-用户-编辑
system:user:delete      # 系统-用户-删除
system:role:assign      # 系统-角色-分配权限
order:list:export       # 订单-列表-导出
```

## 数据库配置

权限标识存储在 `sys_menu` 表中：

```sql
INSERT INTO sys_menu (id, parent_id, name, type, auth_mark)
VALUES (1001, 100, '新增用户', 3, 'system:user:add');
-- type = 3 表示按钮级权限
```

::: tip
新增按钮权限时，需在 `database/migrations/` 中添加迁移脚本插入 `sys_menu` 记录，前端只需使用对应的 `auth_mark` 字符串即可。
:::
