# 枚举与字典

Art Admin 支持两种枚举管理方式：**固定枚举**（前端定义）和**动态枚举**（后端接口 + 三级缓存）。

## 固定枚举

定义在 `src/enums/` 目录下，适用于不会变化的选项：

```ts
// src/enums/systemEnum.ts
export const LogLevelOptions = [
  { label: 'Info', value: 'Information' },
  { label: 'Warning', value: 'Warning' },
  { label: 'Error', value: 'Error' },
]

export const StatusCodeOptions = [
  { label: '启用', value: 1 },
  { label: '禁用', value: 0 },
]
```

### 样式映射

```ts
// 可配合 Element Plus Tag 组件使用
export function getStatusType(status: number) {
  return status === 1 ? 'success' : 'danger'
}
```

### 使用

```vue
<template>
  <ElSelect v-model="form.status">
    <ElOption
      v-for="item in StatusCodeOptions"
      :key="item.value"
      :label="item.label"
      :value="item.value"
    />
  </ElSelect>
</template>

<script setup>
import { StatusCodeOptions } from '@/enums/systemEnum'
</script>
```

## 动态枚举

通过后端接口获取，带三级缓存（内存 → sessionStorage → 接口请求）：

```ts
import { getEnumOptions, getEnumLabel } from '@/utils/dict'

// 获取枚举选项（自动缓存）
const statusOptions = await getEnumOptions('ActiveStatus')
// → [{ label: '活跃', value: 1 }, { label: '禁用', value: 0 }]

// 根据值获取显示文本
const label = getEnumLabel(statusOptions, 1)  // → '活跃'
```

### 三级缓存机制

```
getEnumOptions('ActiveStatus')
    │
    ├── 1. 内存 Map → 命中直接返回
    │
    ├── 2. sessionStorage → 命中写入内存并返回
    │
    ├── 3. 请求锁检查 → 有并发请求则等待其结果
    │
    └── 4. 发起 API 请求 → GET /common/dict/enums?name=ActiveStatus
                          → 写入内存 + sessionStorage
```

### 并发请求去重

多个组件同时请求同一枚举时，只发一次网络请求：

```ts
// 组件A、B、C 同时调用
await getEnumOptions('ActiveStatus')  // 只发 1 次请求
```

### 清除缓存

```ts
import { clearEnumCache } from '@/utils/dict'

clearEnumCache('ActiveStatus')  // 清除指定枚举缓存
clearEnumCache()                // 清除所有枚举缓存
```

## 在表格中使用

```ts
const statusOptions = ref<any[]>([])

onMounted(async () => {
  statusOptions.value = await getEnumOptions('ActiveStatus')
})

// 列定义中使用
{
  prop: 'status',
  label: '状态',
  formatter: (row) => getEnumLabel(statusOptions.value, row.status)
}
```
