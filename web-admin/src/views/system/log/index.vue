<!-- 系统日志管理页面 -->
<template>
  <div class="log-page art-full-height">
    <!-- 搜索栏 -->
    <LogSearch v-model="searchForm" @search="handleSearch" @reset="resetSearchParams"></LogSearch>

    <ElCard class="art-table-card" shadow="never">
      <!-- 表格头部 -->
      <ArtTableHeader v-model:columns="columnChecks" :loading="loading" @refresh="refreshData">
        <template #left>
          <ElSpace wrap>
            <ElTag type="info">共 {{ pagination.total }} 条记录</ElTag>
          </ElSpace>
        </template>
      </ArtTableHeader>

      <!-- 表格 -->
      <ArtTable
        :loading="loading"
        :data="data as Record<string, any>[]"
        :columns="columns"
        :pagination="pagination"
        :table-config="{ rowKey: 'id' }"
        @pagination:size-change="handleSizeChange"
        @pagination:current-change="handleCurrentChange"
      >
      </ArtTable>

      <!-- 日志详情抽屉 -->
      <LogDetailDrawer v-model:visible="detailVisible" :log-data="currentLogData" />
    </ElCard>
  </div>
</template>

<script setup lang="ts">
  import { ElTag } from 'element-plus'
  import ArtButtonTable from '@/components/core/forms/art-button-table/index.vue'
  import { useTable } from '@/hooks/core/useTable'
  import { fetchLogList, type SysLogItem, type SysLogListRequest } from '@/api/system-log'
  import LogSearch from './modules/log-search.vue'
  import LogDetailDrawer from './modules/log-detail-drawer.vue'
  import {
    LogLevelDisplayMap,
    LogLevelTypeMap,
    getStatusCodeType,
    getMethodType
  } from '@/enums/systemEnum'

  defineOptions({ name: 'SystemLog' })

  // 详情弹窗相关
  const detailVisible = ref(false)
  const currentLogData = ref<SysLogItem | null>(null)

  // 搜索表单
  const searchForm = ref<Record<string, any>>({
    level: undefined,
    startTime: undefined,
    endTime: undefined,
    keyword: undefined,
    requestPath: undefined,
    userName: undefined,
    ipAddress: undefined,
    requestId: undefined,
    statusCode: undefined
  })

  // 使用 useTable Hook
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
    core: {
      apiFn: (params: any) => {
        // 转换参数格式
        const { current, size, startTime, endTime, statusCode, ...rest } = params

        // 处理 QueryDate 和时间范围
        let queryDate = new Date().toISOString().split('T')[0] // 默认今天
        let finalStartTime: string | undefined
        let finalEndTime: string | undefined

        if (startTime) {
          // 从开始时间提取日期作为 QueryDate
          queryDate = startTime.split(' ')[0]
          finalStartTime = startTime

          // 如果有结束时间，验证是否在同一天
          if (endTime) {
            const startDate = startTime.split(' ')[0]
            const endDate = endTime.split(' ')[0]
            if (startDate !== endDate) {
              ElMessage.warning('开始时间和结束时间必须在同一天')
              return Promise.reject(new Error('时间跨天'))
            }
            finalEndTime = endTime
          }
        }

        const transformedParams: SysLogListRequest = {
          ...rest,
          queryDate,
          pageSize: size,
          pageIndex: current
        }

        // 添加时间范围
        if (finalStartTime) {
          transformedParams.startTime = finalStartTime
        }
        if (finalEndTime) {
          transformedParams.endTime = finalEndTime
        }

        // 添加状态码（包括0表示空）
        if (statusCode !== undefined && statusCode !== '') {
          transformedParams.statusCode = Number(statusCode)
        }

        return fetchLogList(transformedParams)
      },
      apiParams: {
        ...searchForm.value
      },
      columnsFactory: () => [
        { type: 'index', width: 60, label: '序号' },
        {
          prop: 'timestamp',
          label: '时间',
          width: 170,
          formatter: (row: any) => formatTimestamp(row.timestamp)
        },
        {
          prop: 'level',
          label: '级别',
          width: 90,
          formatter: (row: any) => {
            const level = row.level || 'Information'
            const displayLevel = LogLevelDisplayMap[level] || level
            const levelType = LogLevelTypeMap[level] || 'info'
            return h(
              ElTag,
              {
                type: levelType as 'primary' | 'success' | 'warning' | 'info' | 'danger',
                size: 'small'
              },
              () => displayLevel
            )
          }
        },
        {
          prop: 'statusCode',
          label: '状态码',
          width: 90,
          formatter: (row: any) => {
            const statusCode = row.statusCode
            return h(
              ElTag,
              {
                type: getStatusCodeType(statusCode),
                size: 'small'
              },
              () => statusCode?.toString() || '无'
            )
          }
        },
        {
          prop: 'requestMethod',
          label: '方法',
          width: 80,
          formatter: (row: any) => {
            const method = row.requestMethod || '无'
            return h(
              ElTag,
              {
                type: getMethodType(method),
                size: 'small'
              },
              () => method
            )
          }
        },
        {
          prop: 'requestPath',
          label: '请求路径',
          minWidth: 200,
          showOverflowTooltip: true,
          formatter: (row: any) => row.requestPath || '-'
        },
        {
          prop: 'userName',
          label: '用户',
          width: 100,
          formatter: (row: any) => row.userName || '-'
        },
        {
          prop: 'ipAddress',
          label: 'IP地址',
          width: 130,
          formatter: (row: any) => row.ipAddress || '-'
        },
        {
          prop: 'elapsed',
          label: '耗时',
          width: 80,
          formatter: (row: any) => {
            const elapsed = Math.round(row.elapsed || 0)
            const color =
              elapsed > 1000
                ? 'var(--el-color-danger)'
                : elapsed > 500
                  ? 'var(--el-color-warning)'
                  : 'var(--el-color-success)'
            return h('span', { style: { color } }, `${elapsed}ms`)
          }
        },
        {
          prop: 'message',
          label: '消息',
          minWidth: 250,
          showOverflowTooltip: true,
          formatter: (row: any) => row.message || '-'
        },
        {
          prop: 'operation',
          label: '操作',
          width: 80,
          fixed: 'right',
          formatter: (row: any) =>
            h('div', [
              h(ArtButtonTable, {
                type: 'view',
                onClick: () => showLogDetail(row)
              })
            ])
        }
      ]
    },
    // 数据处理
    transform: {
      dataTransformer: (data: unknown) => {
        return Array.isArray(data) ? data : []
      },
      responseAdapter: (response: any) => {
        return {
          data: Array.isArray(response?.data) ? response.data : [],
          total: response?.total || 0
        }
      }
    },
    hooks: {
      onError: (error) => {
        console.error('获取日志失败:', error)
        ElMessage.error(error.message || '获取日志列表失败')
      }
    }
  })

  /**
   * 格式化时间戳
   */
  const formatTimestamp = (timestamp: string | Date | undefined): string => {
    if (!timestamp) return '-'
    const date = new Date(timestamp)
    return date.toLocaleString('zh-CN', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    })
  }

  /**
   * 搜索处理
   */
  const handleSearch = (params: Record<string, any>) => {
    // 搜索参数赋值
    Object.assign(searchParams, params)
    getData()
  }

  /**
   * 显示日志详情
   */
  const showLogDetail = (row: SysLogItem) => {
    currentLogData.value = row
    detailVisible.value = true
  }
</script>

<style lang="scss" scoped>
  .log-page {
    :deep(.el-tag) {
      border-radius: 4px;
    }
  }
</style>
