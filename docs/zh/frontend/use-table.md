# useTable 表格 Hook

`useTable` 是 Art Admin 核心 Hook，封装了数据获取、分页、搜索、缓存、列配置等完整的表格功能。

## 基本用法

```ts
const {
  columns,           // 列定义
  columnChecks,      // 列显隐控制
  data,              // 表格数据
  loading,           // 加载状态
  pagination,        // 分页信息
  getData,           // 获取数据
  searchParams,      // 搜索参数
  resetSearchParams, // 重置搜索
  handleSizeChange,  // 页大小变化
  handleCurrentChange, // 页码变化
  refreshData,       // 刷新数据
} = useTable({
  core: {
    apiFn: fetchGetUserList,
    apiParams: { ...searchForm.value },
    columnsFactory: () => [
      { type: 'selection', width: 50 },
      { type: 'index', width: 60, label: '序号' },
      { prop: 'username', label: '用户名', minWidth: 120 },
      { prop: 'nickname', label: '昵称', minWidth: 120 },
      { prop: 'status', label: '状态', width: 100,
        formatter: (row) => h(ElTag, {
          type: row.status === 1 ? 'success' : 'danger'
        }, () => row.status === 1 ? '启用' : '禁用')
      },
      { prop: 'operation', label: '操作', width: 120, fixed: 'right',
        formatter: (row) => h('div', [
          h(ArtButtonTable, { type: 'edit', onClick: () => handleEdit(row) }),
          h(ArtButtonTable, { type: 'delete', onClick: () => handleDelete(row) })
        ])
      }
    ]
  }
})
```

## 配置选项

```ts
interface UseTableConfig {
  core: {
    apiFn: Function           // API 请求函数
    apiParams?: object        // 默认搜索参数
    immediate?: boolean       // 是否立即加载（默认 true）
    columnsFactory?: () => ColumnOption[]  // 列定义工厂
    paginationKey?: {
      current?: string        // 分页参数名（默认 'pageIndex'）
      size?: string           // 分页大小参数名（默认 'pageSize'）
    }
  }
  transform?: {
    dataTransformer?: (data) => data    // 数据转换
    responseAdapter?: (response) => data // 响应适配
  }
  performance?: {
    enableCache?: boolean     // 启用缓存
    cacheTime?: number        // 缓存时长
    debounceTime?: number     // 防抖时长
    maxCacheSize?: number     // 最大缓存数
  }
  hooks?: {
    onSuccess?: (data) => void
    onError?: (error) => void
    onCacheHit?: () => void
    resetFormCallback?: () => void
  }
}
```

## 5 种刷新策略

```ts
refreshData()    // 全量刷新 — 清空所有缓存
refreshSoft()    // 轻量刷新 — 保持分页状态
refreshCreate()  // 新增后 — 回到第一页
refreshUpdate()  // 更新后 — 保持当前页
refreshRemove()  // 删除后 — 智能处理空页（若最后一页删空则回前一页）
```

### 使用场景

```ts
// 新增成功后
async function handleDialogSubmit(data) {
  await fetchSaveUser(data)
  dialogVisible.value = false
  refreshCreate()  // 回到第一页看新数据
}

// 删除成功后
async function handleDelete(row) {
  await fetchDeleteUser(row.id)
  refreshRemove()  // 智能处理空页
}
```

## 分页约定

分页参数名遵循后端约定：

```ts
// 请求参数
{ pageIndex: 1, pageSize: 20, ...搜索条件 }

// 响应自动识别
{ total: 100, items: [...] }    // ✅
{ total: 100, list: [...] }     // ✅
{ total: 100, data: [...] }     // ✅
{ count: 100, records: [...] }  // ✅
```

## 搜索集成

```ts
// 搜索
function handleSearch() {
  Object.assign(searchParams, searchForm.value)
  getData()
}

// 重置
function handleReset() {
  resetSearchParams()
}
```

## 列动态控制

```ts
// columnChecks 绑定到 ArtTableHeader 实现列显隐切换
<ArtTableHeader v-model:columns="columnChecks" />
```
