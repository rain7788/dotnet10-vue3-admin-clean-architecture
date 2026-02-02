<!-- 菜单管理页面 -->
<template>
  <div class="menu-page art-full-height">
    <!-- 搜索栏 -->
    <ArtSearchBar v-model="formFilters" :items="formItems" :showExpand="false" @reset="handleReset"
      @search="handleSearch" />

    <ElCard class="art-table-card" shadow="never">
      <!-- 表格头部 -->
      <ArtTableHeader :showZebra="false" :loading="loading" v-model:columns="columnChecks" @refresh="handleRefresh">
        <template #left>
          <ElButton v-auth="'system:menu:add'" @click="handleAddMenu" v-ripple> 添加菜单 </ElButton>
          <ElButton @click="toggleExpand" v-ripple>
            {{ isExpanded ? '收起' : '展开' }}
          </ElButton>
        </template>
      </ArtTableHeader>

      <ArtTable ref="tableRef" rowKey="id" :loading="loading" :columns="columns" :data="filteredTableData"
        :stripe="false" :tree-props="{ children: 'children', hasChildren: 'hasChildren' }"
        :default-expand-all="false" />

      <!-- 菜单弹窗 -->
      <MenuDialog v-model:visible="dialogVisible" :type="dialogType" :editData="editData"
        :parentMenu="currentParentMenu" :lockType="lockMenuType" @submit="handleSubmit" />
    </ElCard>
  </div>
</template>

<script setup lang="ts">
import ArtButtonTable from '@/components/core/forms/art-button-table/index.vue'
import { useTableColumns } from '@/hooks/core/useTableColumns'
import MenuDialog from './modules/menu-dialog.vue'
import {
  fetchGetMenuTree,
  fetchUpdateMenu,
  fetchDeleteMenu,
  fetchUpdatePermission,
  fetchDeletePermission
} from '@/api/system-manage'
import { ElTag, ElMessageBox } from 'element-plus'
import { formatDateTime } from '@/utils/format'

defineOptions({ name: 'Menus' })

// 状态管理
const loading = ref(false)
const isExpanded = ref(false)
const tableRef = ref()

// 弹窗相关
const dialogVisible = ref(false)
const dialogType = ref<'menu' | 'button'>('menu')
const editData = ref<any>(null)
const currentParentMenu = ref<any>(null)
const lockMenuType = ref(false)

// 搜索相关
const initialSearchState = {
  name: '',
  route: ''
}

const formFilters = reactive({ ...initialSearchState })
const appliedFilters = reactive({ ...initialSearchState })

const formItems = computed(() => [
  {
    label: '菜单名称',
    key: 'name',
    type: 'input',
    props: { clearable: true }
  },
  {
    label: '路由地址',
    key: 'route',
    type: 'input',
    props: { clearable: true }
  }
])

onMounted(() => {
  getMenuList()
})

/**
 * 获取菜单列表数据
 */
const getMenuList = async (): Promise<void> => {
  loading.value = true

  try {
    const list = await fetchGetMenuTree()
    tableData.value = list
  } catch (error) {
    console.error('获取菜单失败:', error)
  } finally {
    loading.value = false
  }
}

/**
 * 获取菜单类型标签颜色
 */
const getMenuTypeTag = (row: any): 'primary' | 'success' | 'warning' | 'info' | 'danger' => {
  if (row.isPermission) return 'danger'
  if (row.children?.length) return 'info'
  if (row.type === '内嵌') return 'success'
  if (row.type === '外链') return 'warning'
  return 'primary'
}

/**
 * 获取菜单类型文本
 */
const getMenuTypeText = (row: any): string => {
  if (row.isPermission) return '按钮'
  if (row.children?.length) return '目录'
  return row.type || '菜单'
}

// 表格列配置
const { columnChecks, columns } = useTableColumns(() => [
  {
    prop: 'name',
    label: '菜单名称',
    minWidth: 120
  },
  {
    prop: 'type',
    label: '菜单类型',
    formatter: (row: any) => {
      return h(ElTag, { type: getMenuTypeTag(row) }, () => getMenuTypeText(row))
    }
  },
  {
    prop: 'path',
    label: '路由',
    formatter: (row: any) => {
      if (row.isPermission) return ''
      return row.link || row.path || ''
    }
  },
  {
    prop: 'permissions',
    label: '权限标识',
    formatter: (row: any) => {
      if (row.isPermission) {
        return row.code || ''
      }
      if (!row.permissions?.length) return ''
      return `${row.permissions.length} 个权限标识`
    }
  },
  {
    prop: 'updatedTime',
    label: '编辑时间',
    formatter: (row: any) => formatDateTime(row.updatedTime || row.createdTime)
  },
  {
    prop: 'isVisible',
    label: '状态',
    formatter: (row: any) => {
      if (row.isPermission) return ''
      return h(ElTag, { type: row.isVisible ? 'success' : 'info' }, () =>
        row.isVisible ? '显示' : '隐藏'
      )
    }
  },
  {
    prop: 'operation',
    label: '操作',
    width: 180,
    align: 'right',
    formatter: (row: any) => {
      const buttonStyle = { style: 'text-align: right' }

      if (row.isPermission) {
        return h('div', buttonStyle, [
          h(ArtButtonTable, {
            type: 'edit',
            onClick: () => handleEditAuth(row)
          }),
          h(ArtButtonTable, {
            type: 'delete',
            onClick: () => handleDeleteAuth(row)
          })
        ])
      }

      return h('div', buttonStyle, [
        h(ArtButtonTable, {
          type: 'add',
          onClick: () => handleAddAuth(row),
          title: '新增权限'
        }),
        h(ArtButtonTable, {
          type: 'edit',
          onClick: () => handleEditMenu(row)
        }),
        h(ArtButtonTable, {
          type: 'delete',
          onClick: () => handleDeleteMenu(row)
        })
      ])
    }
  }
])

// 数据相关
const tableData = ref<any[]>([])

/**
 * 重置搜索条件
 */
const handleReset = (): void => {
  Object.assign(formFilters, { ...initialSearchState })
  Object.assign(appliedFilters, { ...initialSearchState })
  getMenuList()
}

/**
 * 执行搜索
 */
const handleSearch = (): void => {
  Object.assign(appliedFilters, { ...formFilters })
  getMenuList()
}

/**
 * 刷新菜单列表
 */
const handleRefresh = (): void => {
  getMenuList()
}

/**
 * 深度克隆对象
 */
const deepClone = <T,>(obj: T): T => {
  if (obj === null || typeof obj !== 'object') return obj
  if (obj instanceof Date) return new Date(obj) as T
  if (Array.isArray(obj)) return obj.map((item) => deepClone(item)) as T

  const cloned = {} as T
  for (const key in obj) {
    if (Object.prototype.hasOwnProperty.call(obj, key)) {
      cloned[key] = deepClone(obj[key])
    }
  }
  return cloned
}

/**
 * 将权限列表转换为子节点
 */
const convertPermissionsToChildren = (items: any[]): any[] => {
  return items.map((item) => {
    const clonedItem = deepClone(item)

    if (clonedItem.children?.length) {
      clonedItem.children = convertPermissionsToChildren(clonedItem.children)
    }

    if (item.permissions?.length) {
      const permChildren = item.permissions.map((perm: any) => ({
        id: perm.id,
        name: perm.name,
        code: perm.code,
        isPermission: true,
        menuId: item.id
      }))

      clonedItem.children = clonedItem.children?.length
        ? [...clonedItem.children, ...permChildren]
        : permChildren
    }

    return clonedItem
  })
}

/**
 * 搜索菜单
 */
const searchMenu = (items: any[]): any[] => {
  const results: any[] = []

  for (const item of items) {
    const searchName = appliedFilters.name?.toLowerCase().trim() || ''
    const searchRoute = appliedFilters.route?.toLowerCase().trim() || ''
    const menuTitle = (item.name || '').toLowerCase()
    const menuPath = (item.path || '').toLowerCase()
    const nameMatch = !searchName || menuTitle.includes(searchName)
    const routeMatch = !searchRoute || menuPath.includes(searchRoute)

    if (item.children?.length) {
      const matchedChildren = searchMenu(item.children)
      if (matchedChildren.length > 0) {
        const clonedItem = deepClone(item)
        clonedItem.children = matchedChildren
        results.push(clonedItem)
        continue
      }
    }

    if (nameMatch && routeMatch) {
      results.push(deepClone(item))
    }
  }

  return results
}

// 过滤后的表格数据
const filteredTableData = computed(() => {
  const searchedData = searchMenu(tableData.value)
  return convertPermissionsToChildren(searchedData)
})

/**
 * 添加菜单
 */
const handleAddMenu = (): void => {
  dialogType.value = 'menu'
  editData.value = null
  currentParentMenu.value = null
  lockMenuType.value = true
  dialogVisible.value = true
}

/**
 * 添加权限按钮
 */
const handleAddAuth = (row: any): void => {
  dialogType.value = 'button'
  editData.value = null
  currentParentMenu.value = row
  lockMenuType.value = false
  dialogVisible.value = true
}

/**
 * 编辑菜单
 */
const handleEditMenu = (row: any): void => {
  dialogType.value = 'menu'
  editData.value = row
  currentParentMenu.value = null
  lockMenuType.value = true
  dialogVisible.value = true
}

/**
 * 编辑权限按钮
 */
const handleEditAuth = (row: any): void => {
  dialogType.value = 'button'
  editData.value = row
  currentParentMenu.value = null
  lockMenuType.value = false
  dialogVisible.value = true
}

/**
 * 提交表单数据
 */
const handleSubmit = async (formData: any): Promise<void> => {
  try {
    if (dialogType.value === 'menu') {
      await fetchUpdateMenu(formData)
      ElMessage.success(formData.id ? '菜单更新成功' : '菜单添加成功')
    } else {
      // 权限按钮
      const permData = {
        ...formData,
        menuId: currentParentMenu.value?.id || editData.value?.menuId
      }
      await fetchUpdatePermission(permData)
      ElMessage.success(formData.id ? '权限更新成功' : '权限添加成功')
    }
    getMenuList()
  } catch (error) {
    console.error('保存失败:', error)
  }
}

/**
 * 删除菜单
 */
const handleDeleteMenu = async (row: any): Promise<void> => {
  try {
    await ElMessageBox.confirm('确定要删除该菜单吗？删除后无法恢复', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await fetchDeleteMenu(row.id)
    ElMessage.success('删除成功')
    getMenuList()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除失败:', error)
    }
  }
}

/**
 * 删除权限按钮
 */
const handleDeleteAuth = async (row: any): Promise<void> => {
  try {
    await ElMessageBox.confirm('确定要删除该权限吗？删除后无法恢复', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await fetchDeletePermission(row.id)
    ElMessage.success('删除成功')
    getMenuList()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除失败:', error)
    }
  }
}

/**
 * 切换展开/收起所有菜单
 */
const toggleExpand = (): void => {
  isExpanded.value = !isExpanded.value
  nextTick(() => {
    if (tableRef.value?.elTableRef && filteredTableData.value) {
      const processRows = (rows: any[]) => {
        rows.forEach((row) => {
          if (row.children?.length) {
            tableRef.value.elTableRef.toggleRowExpansion(row, isExpanded.value)
            processRows(row.children)
          }
        })
      }
      processRows(filteredTableData.value)
    }
  })
}
</script>
