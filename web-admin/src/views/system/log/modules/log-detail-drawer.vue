<!-- 日志详情抽屉 -->
<template>
  <ElDrawer
    v-model="drawerVisible"
    title="日志详情"
    size="60%"
    :close-on-click-modal="true"
    :close-on-press-escape="true"
    direction="rtl"
  >
    <template #header>
      <div class="drawer-header">
        <span class="title">日志详情</span>
        <ElTag v-if="logData?.level" :type="getLevelType(logData.level)" size="small">
          {{ getLevelDisplay(logData.level) }}
        </ElTag>
      </div>
    </template>

    <div v-if="logData" class="log-detail-content">
      <!-- 基础信息 -->
      <ElDescriptions :column="2" border>
        <ElDescriptionsItem label="日志ID">{{ logData.id || '-' }}</ElDescriptionsItem>
        <ElDescriptionsItem label="时间">{{
          formatTimestamp(logData.timestamp)
        }}</ElDescriptionsItem>
        <ElDescriptionsItem label="级别">
          <ElTag :type="getLevelType(logData.level)" size="small">
            {{ getLevelDisplay(logData.level) }}
          </ElTag>
        </ElDescriptionsItem>
        <ElDescriptionsItem label="状态码">
          <ElTag :type="getStatusType(logData.statusCode)" size="small">
            {{ logData.statusCode || '无' }}
          </ElTag>
        </ElDescriptionsItem>
        <ElDescriptionsItem label="用户">{{ logData.userName || '未知用户' }}</ElDescriptionsItem>
        <ElDescriptionsItem label="IP地址">{{ logData.ipAddress || '-' }}</ElDescriptionsItem>
        <ElDescriptionsItem label="请求方法">
          <ElTag :type="getMethodTagType(logData.requestMethod)" size="small">
            {{ logData.requestMethod || 'UNKNOWN' }}
          </ElTag>
        </ElDescriptionsItem>
        <ElDescriptionsItem label="耗时">
          <span :class="getElapsedClass(logData.elapsed)">
            {{ Math.round(logData.elapsed || 0) }}ms
          </span>
        </ElDescriptionsItem>
      </ElDescriptions>

      <!-- 请求路径 -->
      <div class="detail-section">
        <div class="section-title">请求路径</div>
        <ElInput v-model="displayRequestPath" readonly class="readonly-input" />
      </div>

      <!-- 请求ID -->
      <div class="detail-section">
        <div class="section-title">请求ID</div>
        <ElInput v-model="displayRequestId" readonly class="readonly-input" />
      </div>

      <!-- 消息 -->
      <div class="detail-section">
        <div class="section-title">消息</div>
        <ElInput
          v-model="displayMessage"
          type="textarea"
          :rows="3"
          readonly
          class="readonly-input"
        />
      </div>

      <!-- 异常信息 -->
      <div v-if="logData.exception" class="detail-section">
        <div class="section-title error-title">异常信息</div>
        <ElInput
          v-model="displayException"
          type="textarea"
          :rows="6"
          readonly
          class="readonly-input error-content"
        />
      </div>

      <!-- 请求参数 -->
      <div v-if="logData.request" class="detail-section">
        <div class="section-title-row">
          <span class="section-title">请求参数</span>
          <ElButton
            type="primary"
            link
            size="small"
            @click="copyToClipboard(formatJson(logData.request))"
          >
            复制
          </ElButton>
        </div>
        <div class="json-content">
          <pre>{{ formatJson(logData.request) }}</pre>
        </div>
      </div>

      <!-- 响应参数 -->
      <div v-if="logData.response" class="detail-section">
        <div class="section-title-row">
          <span class="section-title">响应参数</span>
          <ElButton
            type="primary"
            link
            size="small"
            @click="copyToClipboard(formatJson(logData.response))"
          >
            复制
          </ElButton>
        </div>
        <div class="json-content">
          <pre>{{ formatJson(logData.response) }}</pre>
        </div>
      </div>
    </div>
  </ElDrawer>
</template>

<script setup lang="ts">
  import {
    ElDrawer,
    ElDescriptions,
    ElDescriptionsItem,
    ElInput,
    ElTag,
    ElButton
  } from 'element-plus'
  import {
    LogLevelDisplayMap,
    LogLevelTypeMap,
    getStatusCodeType,
    getMethodType
  } from '@/enums/systemEnum'
  import type { SysLogItem } from '@/api/system-log'

  interface Props {
    visible: boolean
    logData: SysLogItem | null
  }

  interface Emits {
    (e: 'update:visible', value: boolean): void
  }

  const props = defineProps<Props>()
  const emit = defineEmits<Emits>()

  const drawerVisible = computed({
    get: () => props.visible,
    set: (value: boolean) => emit('update:visible', value)
  })

  // 安全显示的计算属性
  const displayRequestPath = computed(() => props.logData?.requestPath || '-')
  const displayRequestId = computed(() => props.logData?.requestId || '-')
  const displayMessage = computed(() => props.logData?.message || '-')
  const displayException = computed(() => props.logData?.exception || '-')

  /**
   * 获取日志级别显示文本
   */
  const getLevelDisplay = (level: string) => {
    return LogLevelDisplayMap[level] || level || 'Info'
  }

  /**
   * 获取日志级别标签类型
   */
  const getLevelType = (level: string): 'primary' | 'success' | 'warning' | 'info' | 'danger' => {
    const type = LogLevelTypeMap[level] || 'info'
    return type as 'primary' | 'success' | 'warning' | 'info' | 'danger'
  }

  /**
   * 获取状态码标签类型
   */
  const getStatusType = (
    statusCode: number | null | undefined
  ): 'primary' | 'success' | 'warning' | 'info' | 'danger' => {
    return getStatusCodeType(statusCode)
  }

  /**
   * 获取请求方法标签类型
   */
  const getMethodTagType = (
    method: string | null | undefined
  ): 'primary' | 'success' | 'warning' | 'info' | 'danger' => {
    return getMethodType(method)
  }

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
   * 获取耗时样式类
   */
  const getElapsedClass = (elapsed: number | undefined): string => {
    if (!elapsed) return ''
    if (elapsed > 1000) return 'elapsed-slow'
    if (elapsed > 500) return 'elapsed-medium'
    return 'elapsed-fast'
  }

  /**
   * 格式化 JSON 字符串
   */
  const formatJson = (jsonStr: string | undefined): string => {
    if (!jsonStr) return '-'
    try {
      const obj = typeof jsonStr === 'string' ? JSON.parse(jsonStr) : jsonStr
      return JSON.stringify(obj, null, 2)
    } catch {
      return jsonStr
    }
  }

  /**
   * 复制到剪贴板
   */
  const copyToClipboard = async (text: string) => {
    try {
      await navigator.clipboard.writeText(text)
      ElMessage.success('已复制到剪贴板')
    } catch {
      ElMessage.error('复制失败')
    }
  }
</script>

<style lang="scss" scoped>
  .drawer-header {
    display: flex;
    align-items: center;
    gap: 12px;

    .title {
      font-size: 16px;
      font-weight: 600;
    }
  }

  .log-detail-content {
    padding: 0 8px;
  }

  .detail-section {
    margin-top: 20px;

    .section-title {
      font-weight: 500;
      margin-bottom: 8px;
      color: var(--el-text-color-primary);

      &.error-title {
        color: var(--el-color-danger);
      }
    }

    .section-title-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 8px;
    }
  }

  .readonly-input {
    :deep(.el-input__wrapper),
    :deep(.el-textarea__inner) {
      background-color: var(--el-fill-color-light);
    }

    &.error-content {
      :deep(.el-textarea__inner) {
        color: var(--el-color-danger);
        font-family: 'Courier New', monospace;
        font-size: 12px;
      }
    }
  }

  .json-content {
    background-color: var(--el-fill-color-light);
    border-radius: 4px;
    padding: 12px;
    max-height: 300px;
    overflow: auto;

    pre {
      margin: 0;
      font-family: 'Courier New', Consolas, monospace;
      font-size: 12px;
      line-height: 1.5;
      white-space: pre-wrap;
      word-wrap: break-word;
    }
  }

  .elapsed-fast {
    color: var(--el-color-success);
  }

  .elapsed-medium {
    color: var(--el-color-warning);
  }

  .elapsed-slow {
    color: var(--el-color-danger);
    font-weight: 600;
  }
</style>
