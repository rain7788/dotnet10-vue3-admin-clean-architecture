<!-- 用户管理页面 -->
<!-- art-full-height 自动计算出页面剩余高度 -->
<!-- art-table-card 一个符合系统样式的 class，同时自动撑满剩余高度 -->
<!-- 更多 useTable 使用示例请移步至 功能示例 下面的高级表格示例或者查看官方文档 -->
<!-- useTable 文档：请查看项目文档 -->
<template>
  <div class="user-page art-full-height">
    <!-- 搜索栏 -->
    <UserSearch v-model="searchForm" @search="handleSearch" @reset="resetSearchParams"></UserSearch>

    <ElCard class="art-table-card" shadow="never">
      <!-- 表格头部 -->
      <ArtTableHeader v-model:columns="columnChecks" :loading="loading" @refresh="refreshData">
        <template #left>
          <ElSpace wrap>
            <ElButton @click="showDialog('add')" v-ripple v-auth="'system:user:add'">新增用户</ElButton>
          </ElSpace>
        </template>
      </ArtTableHeader>

      <!-- 表格 -->
      <ArtTable :loading="loading" :data="(data as Record<string, any>[])" :columns="columns" :pagination="pagination"
        @selection-change="handleSelectionChange" @pagination:size-change="handleSizeChange"
        @pagination:current-change="handleCurrentChange">
      </ArtTable>

      <!-- 用户弹窗 -->
      <UserDialog v-model:visible="dialogVisible" :type="dialogType" :user-data="currentUserData"
        @submit="handleDialogSubmit" />
    </ElCard>
  </div>
</template>

<script setup lang="ts">
import ArtButtonTable from '@/components/core/forms/art-button-table/index.vue'
import { useTable } from '@/hooks/core/useTable'
import { fetchGetUserList, fetchDeleteUser, fetchUpdateUser } from '@/api/system-manage'
import UserSearch from './modules/user-search.vue'
import UserDialog from './modules/user-dialog.vue'
import { ElTag, ElMessageBox } from 'element-plus'
import { DialogType } from '@/types'
import { getEnumOptions, getEnumLabel, type EnumOption } from '@/utils/dict'
import { useAuth } from '@/hooks/core/useAuth'
import { formatDateTime } from '@/utils/format'

defineOptions({ name: 'User' })

// 权限控制
const { hasAuth } = useAuth()

// 弹窗相关
const dialogType = ref<DialogType>('add')
const dialogVisible = ref(false)
const currentUserData = ref<any>({})

// 选中行
const selectedRows = ref<any[]>([])

// 搜索表单
const searchForm = ref<any>({
  username: undefined,
  realName: undefined,
  status: undefined
})

// 状态枚举选项（从字典加载）
const statusOptions = ref<EnumOption[]>([])

/**
 * 获取用户状态配置（基于字典）
 */
const getUserStatusConfig = (status: number) => {
  const label = getEnumLabel(statusOptions.value, status)
  // 根据枚举数值设置样式：0=不可用, 1=正常
  const typeMap: Record<number, 'success' | 'danger' | 'info'> = {
    1: 'success',
    0: 'danger'
  }
  return { type: typeMap[status] || 'info', text: label }
}

const {
  columns,
  columnChecks,
  data,
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
    apiFn: fetchGetUserList,
    apiParams: {
      ...searchForm.value
    },
    // 分页参数已在全局 tableConfig.ts 配置，无需重复设置
    columnsFactory: () => [
      { type: 'selection', width: 50 }, // 勾选列
      { type: 'index', width: 60, label: '序号' }, // 序号
      {
        prop: 'username',
        label: '用户名',
        minWidth: 120
      },
      {
        prop: 'realName',
        label: '姓名',
        minWidth: 120
      },
      {
        prop: 'roles',
        label: '角色',
        minWidth: 150,
        formatter: (row: any) => {
          if (!row.roleNames || row.roleNames.length === 0) {
            return h('span', { class: 'text-gray-400' }, '未分配')
          }
          return h('div', { class: 'flex flex-wrap gap-1' },
            row.roleNames.map((name: string) =>
              h(ElTag, { size: 'small', type: 'info' }, () => name)
            )
          )
        }
      },
      {
        prop: 'isSuper',
        label: '超管',
        width: 80,
        formatter: (row: any) => {
          return h(ElTag, { type: row.isSuper ? 'success' : 'info' }, () => row.isSuper ? '是' : '否')
        }
      },
      {
        prop: 'status',
        label: '状态',
        width: 100,
        formatter: (row: any) => {
          const statusConfig = getUserStatusConfig(row.status)
          return h(ElTag, { type: statusConfig.type }, () => statusConfig.text)
        }
      },
      {
        prop: 'lastLoginTime',
        label: '最后登录',
        minWidth: 140,
        formatter: (row: any) => formatDateTime(row.lastLoginTime)
      },
      {
        prop: 'createdTime',
        label: '创建日期',
        minWidth: 140,
        sortable: true,
        formatter: (row: any) => formatDateTime(row.createdTime)
      },
      {
        prop: 'operation',
        label: '操作',
        width: 120,
        fixed: 'right', // 固定列
        formatter: (row: any) => {
          const buttons = []
          if (hasAuth('system:user:edit')) {
            buttons.push(
              h(ArtButtonTable, {
                type: 'edit',
                onClick: () => showDialog('edit', row)
              })
            )
          }
          if (hasAuth('system:user:delete')) {
            buttons.push(
              h(ArtButtonTable, {
                type: 'delete',
                onClick: () => deleteUser(row)
              })
            )
          }
          return h('div', buttons)
        }
      }
    ]
  }
})

/**
 * 搜索处理
 * @param params 参数
 */
const handleSearch = (params: Record<string, any>) => {
  console.log(params)
  // 搜索参数赋值
  Object.assign(searchParams, params)
  getData()
}

/**
 * 显示用户弹窗
 */
const showDialog = (type: DialogType, row?: any): void => {
  dialogType.value = type
  currentUserData.value = row || {}
  nextTick(() => {
    dialogVisible.value = true
  })
}

/**
 * 删除用户
 */
const deleteUser = (row: any): void => {
  console.log('删除用户:', row)
  ElMessageBox.confirm(`确定要删除用户"${row.username}"吗？`, '删除用户', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'error'
  }).then(async () => {
    try {
      await fetchDeleteUser(row.userId)
      ElMessage.success('删除成功')
      getData()
    } catch (error) {
      console.error('删除失败:', error)
    }
  })
}

/**
 * 处理弹窗提交事件
 */
const handleDialogSubmit = async (formData: any) => {
  try {
    await fetchUpdateUser(formData)
    ElMessage.success(dialogType.value === 'add' ? '添加成功' : '更新成功')
    dialogVisible.value = false
    currentUserData.value = {}
    getData()
  } catch (error) {
    console.error('提交失败:', error)
  }
}

/**
 * 处理表格行选择变化
 */
const handleSelectionChange = (selection: any[]): void => {
  selectedRows.value = selection
}

// 页面初始化：加载枚举字典
onMounted(async () => {
  statusOptions.value = await getEnumOptions('ActiveStatus')
})
</script>
