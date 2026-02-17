# useTable Hook

`useTable` is Art Admin's core hook, encapsulating data fetching, pagination, search, caching, and column configuration.

## Basic Usage

```ts
const {
  columns, columnChecks, data, loading, pagination,
  getData, searchParams, resetSearchParams,
  handleSizeChange, handleCurrentChange, refreshData,
} = useTable({
  core: {
    apiFn: fetchGetUserList,
    apiParams: { ...searchForm.value },
    columnsFactory: () => [
      { type: 'selection', width: 50 },
      { type: 'index', width: 60, label: '#' },
      { prop: 'username', label: 'Username', minWidth: 120 },
      { prop: 'status', label: 'Status', width: 100,
        formatter: (row) => h(ElTag, {
          type: row.status === 1 ? 'success' : 'danger'
        }, () => row.status === 1 ? 'Active' : 'Disabled')
      },
      { prop: 'operation', label: 'Actions', width: 120, fixed: 'right',
        formatter: (row) => h('div', [
          h(ArtButtonTable, { type: 'edit', onClick: () => handleEdit(row) }),
          h(ArtButtonTable, { type: 'delete', onClick: () => handleDelete(row) })
        ])
      }
    ]
  }
})
```

## Configuration

```ts
interface UseTableConfig {
  core: {
    apiFn: Function           // API function
    apiParams?: object        // Default search params
    immediate?: boolean       // Load immediately (default: true)
    columnsFactory?: () => ColumnOption[]
    paginationKey?: {
      current?: string        // Page number param (default: 'pageIndex')
      size?: string           // Page size param (default: 'pageSize')
    }
  }
  transform?: {
    dataTransformer?: (data) => data
    responseAdapter?: (response) => data
  }
  performance?: {
    enableCache?: boolean
    cacheTime?: number
    debounceTime?: number
    maxCacheSize?: number
  }
  hooks?: {
    onSuccess?: (data) => void
    onError?: (error) => void
    resetFormCallback?: () => void
  }
}
```

## 5 Refresh Strategies

```ts
refreshData()    // Full refresh — clears all cache
refreshSoft()    // Soft refresh — keeps pagination state
refreshCreate()  // After create — goes to page 1
refreshUpdate()  // After update — stays on current page
refreshRemove()  // After delete — smart empty page handling
```

### Usage Examples

```ts
// After creating
async function handleDialogSubmit(data) {
  await fetchSaveUser(data)
  dialogVisible.value = false
  refreshCreate()  // Go to page 1 to see new record
}

// After deleting
async function handleDelete(row) {
  await fetchDeleteUser(row.id)
  refreshRemove()  // Smart: goes to previous page if current is empty
}
```

## Pagination Convention

```ts
// Request params
{ pageIndex: 1, pageSize: 20, ...searchFilters }

// Response auto-detection
{ total: 100, items: [...] }    // ✅
{ total: 100, list: [...] }     // ✅
{ total: 100, data: [...] }     // ✅
{ count: 100, records: [...] }  // ✅
```
