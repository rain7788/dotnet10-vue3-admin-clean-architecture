# Enums & Dictionaries

Art Admin supports two enum management approaches: **static enums** (frontend-defined) and **dynamic enums** (API + three-level cache).

## Static Enums

Defined in `src/enums/`, for options that never change:

```ts
// src/enums/systemEnum.ts
export const LogLevelOptions = [
  { label: 'Info', value: 'Information' },
  { label: 'Warning', value: 'Warning' },
  { label: 'Error', value: 'Error' },
]

export const StatusCodeOptions = [
  { label: 'Active', value: 1 },
  { label: 'Disabled', value: 0 },
]
```

### Usage

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

## Dynamic Enums

Fetched from backend API with three-level caching (Memory → sessionStorage → API request):

```ts
import { getEnumOptions, getEnumLabel } from '@/utils/dict'

// Get options (auto-cached)
const statusOptions = await getEnumOptions('ActiveStatus')
// → [{ label: 'Active', value: 1 }, { label: 'Disabled', value: 0 }]

// Get label by value
const label = getEnumLabel(statusOptions, 1)  // → 'Active'
```

### Three-Level Cache

```
getEnumOptions('ActiveStatus')
    │
    ├── 1. Memory Map → hit → return instantly
    │
    ├── 2. sessionStorage → hit → write to memory & return
    │
    ├── 3. Concurrent lock → pending request exists → wait for its result
    │
    └── 4. API request → GET /common/dict/enums?name=ActiveStatus
                        → write to memory + sessionStorage
```

### Concurrent Deduplication

Multiple components requesting the same enum simultaneously only trigger one network request.

### Clear Cache

```ts
import { clearEnumCache } from '@/utils/dict'

clearEnumCache('ActiveStatus')  // Clear specific
clearEnumCache()                // Clear all
```
