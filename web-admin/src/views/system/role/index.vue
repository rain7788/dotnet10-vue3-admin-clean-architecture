<!-- 角色管理页面 -->
<template>
  <div class="art-full-height">
    <RoleSearch v-show="showSearchBar" v-model="searchForm" @search="handleSearch" @reset="resetSearchParams">
    </RoleSearch>

    <ElCard class="art-table-card" shadow="never" :style="{ 'margin-top': showSearchBar ? '12px' : '0' }">
      <ArtTableHeader v-model:columns="columnChecks" v-model:showSearchBar="showSearchBar" :loading="loading"
        @refresh="refreshData">
        <template #left>
          <ElSpace wrap>
            <ElButton @click="showDialog('add')" v-ripple v-auth="'system:role:add'">新增角色</ElButton>
          </ElSpace>
        </template>
      </ArtTableHeader>

      <!-- 表格 -->
      <ArtTable :loading="loading" :data="data" :columns="columns" :pagination="pagination"
        @pagination:size-change="handleSizeChange" @pagination:current-change="handleCurrentChange">
      </ArtTable>
    </ElCard>

    <!-- 角色编辑弹窗 -->
    <RoleEditDialog v-model="dialogVisible" :dialog-type="dialogType" :role-data="currentRoleData"
      @success="refreshData" />

    <!-- 菜单权限弹窗 -->
    <RolePermissionDialog v-model="permissionDialog" :role-data="currentRoleData" @success="handlePermissionSuccess" />
  </div>
</template>

<script setup lang="ts">
import { ButtonMoreItem } from '@/components/core/forms/art-button-more/index.vue'
import { useTable } from '@/hooks/core/useTable'
import { fetchGetRoleList, fetchDeleteRole } from '@/api/system-manage'
import ArtButtonMore from '@/components/core/forms/art-button-more/index.vue'
import RoleSearch from './modules/role-search.vue'
import RoleEditDialog from './modules/role-edit-dialog.vue'
import RolePermissionDialog from './modules/role-permission-dialog.vue'
import { ElTag, ElMessageBox } from 'element-plus'
import { getEnumLabel, getEnumOptions, type EnumOption } from '@/utils/dict'
import { formatDateTime } from '@/utils/format'

defineOptions({ name: 'Role' })

// 状态选项
const statusOptions = ref<EnumOption[]>([])

// 搜索表单
const searchForm = ref({
  name: undefined,
  code: undefined,
  status: undefined
})

const showSearchBar = ref(false)

const dialogVisible = ref(false)
const permissionDialog = ref(false)
const currentRoleData = ref<any>(undefined)

// 获取状态显示配置
const getStatusConfig = (status: number) => {
  const label = getEnumLabel(statusOptions.value, status)
  const typeMap: Record<number, 'success' | 'danger' | 'info'> = {
    1: 'success',
    0: 'danger'
  }
  return { type: typeMap[status] || 'info', text: label }
}

const {
  columns,
  columnChecks,
  data: tableData,
  loading,
  pagination,
  getData,
  searchParams,
  resetSearchParams,
  handleSizeChange,
  handleCurrentChange,
  refreshData
} = useTable({
  // 核心配置
  core: {
    apiFn: fetchGetRoleList,
    apiParams: {
      ...searchForm.value
    },
    columnsFactory: () => [
      {
        type: 'index',
        label: '序号',
        width: 70
      },
      {
        prop: 'name',
        label: '角色名称',
        minWidth: 120
      },
      {
        prop: 'code',
        label: '角色编码',
        minWidth: 120
      },
      {
        prop: 'description',
        label: '角色描述',
        minWidth: 150,
        showOverflowTooltip: true
      },
      {
        prop: 'status',
        label: '角色状态',
        width: 100,
        formatter: (row: any) => {
          const statusConfig = getStatusConfig(row.status)
          return h(
            ElTag,
            { type: statusConfig.type as 'success' | 'danger' | 'info' },
            () => statusConfig.text
          )
        }
      },
      {
        prop: 'createdTime',
        label: '创建日期',
        width: 180,
        sortable: true,
        formatter: (row: any) => formatDateTime(row.createdTime)
      },
      {
        prop: 'operation',
        label: '操作',
        width: 80,
        fixed: 'right',
        formatter: (row: any) =>
          h('div', [
            h(ArtButtonMore, {
              list: [
                {
                  key: 'permission',
                  label: '菜单权限',
                  icon: 'ri:user-3-line',
                  auth: 'system:role:edit'
                },
                {
                  key: 'edit',
                  label: '编辑角色',
                  icon: 'ri:edit-2-line',
                  auth: 'system:role:edit'
                },
                {
                  key: 'delete',
                  label: '删除角色',
                  icon: 'ri:delete-bin-4-line',
                  color: '#f56c6c',
                  auth: 'system:role:delete'
                }
              ],
              onClick: (item: ButtonMoreItem) => buttonMoreClick(item, row)
            })
          ])
      }
    ]
  }
})

const data = computed(() => tableData.value as Record<string, any>[])

const dialogType = ref<'add' | 'edit'>('add')

const showDialog = (type: 'add' | 'edit', row?: any) => {
  dialogVisible.value = true
  dialogType.value = type
  currentRoleData.value = row
}

/**
 * 搜索处理
 * @param params 搜索参数
 */
const handleSearch = (params: Record<string, any>) => {
  Object.assign(searchParams, params)
  getData()
}

const buttonMoreClick = (item: ButtonMoreItem, row: any) => {
  switch (item.key) {
    case 'permission':
      showPermissionDialog(row)
      break
    case 'edit':
      showDialog('edit', row)
      break
    case 'delete':
      deleteRole(row)
      break
  }
}

const showPermissionDialog = (row?: any) => {
  permissionDialog.value = true
  currentRoleData.value = row
}

/**
 * 权限保存成功后的处理
 * 刷新数据并清除当前选中的角色数据引用
 */
const handlePermissionSuccess = async () => {
  await refreshData()
  // 清除引用，确保下次打开使用最新数据
  currentRoleData.value = undefined
}

const deleteRole = (row: any) => {
  ElMessageBox.confirm(`确定删除角色"${row.name}"吗？此操作不可恢复！`, '删除确认', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  })
    .then(async () => {
      try {
        await fetchDeleteRole(row.id)
        ElMessage.success('删除成功')
        refreshData()
      } catch (error) {
        console.error('删除失败:', error)
      }
    })
    .catch(() => {
      ElMessage.info('已取消删除')
    })
}

// 初始化
onMounted(async () => {
  statusOptions.value = await getEnumOptions('ActiveStatus')
})
</script>
