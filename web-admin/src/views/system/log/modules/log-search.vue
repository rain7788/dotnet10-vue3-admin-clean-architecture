<template>
  <ArtSearchBar
    ref="searchBarRef"
    v-model="formData"
    :items="formItems"
    :is-expand="true"
    @reset="handleReset"
    @search="handleSearch"
  >
  </ArtSearchBar>
</template>

<script setup lang="ts">
  import { LogLevelOptions, StatusCodeOptions } from '@/enums/systemEnum'

  interface Props {
    modelValue: Record<string, any>
  }

  interface Emits {
    (e: 'update:modelValue', value: Record<string, any>): void
    (e: 'search', params: Record<string, any>): void
    (e: 'reset'): void
  }

  const props = defineProps<Props>()
  const emit = defineEmits<Emits>()

  // 表单数据双向绑定
  const searchBarRef = ref()
  const formData = computed({
    get: () => props.modelValue,
    set: (val) => emit('update:modelValue', val)
  })

  // 日志级别选项（排除"全部"选项，使用空字符串作为默认值）
  const levelOptions = computed(() =>
    LogLevelOptions.slice(1).map((opt) => ({ label: opt.label, value: opt.value }))
  )

  // 状态码选项（排除"全部"选项）
  const statusCodeOpts = computed(() =>
    StatusCodeOptions.slice(1).map((opt) => ({ label: opt.label, value: opt.value }))
  )

  // 表单配置
  const formItems = computed(() => [
    {
      label: '日志级别',
      key: 'level',
      type: 'select',
      props: {
        placeholder: '请选择日志级别',
        clearable: true,
        options: levelOptions.value
      }
    },
    {
      label: '状态码',
      key: 'statusCode',
      type: 'select',
      props: {
        placeholder: '请选择状态码',
        clearable: true,
        options: statusCodeOpts.value
      }
    },
    {
      label: '开始时间',
      key: 'startTime',
      type: 'datetime',
      props: {
        type: 'datetime',
        format: 'YYYY-MM-DD HH:mm:ss',
        valueFormat: 'YYYY-MM-DD HH:mm:ss',
        placeholder: '开始时间',
        clearable: true,
        style: 'width: 100%'
      }
    },
    {
      label: '结束时间',
      key: 'endTime',
      type: 'datetime',
      props: {
        type: 'datetime',
        format: 'YYYY-MM-DD HH:mm:ss',
        valueFormat: 'YYYY-MM-DD HH:mm:ss',
        placeholder: '结束时间',
        clearable: true,
        style: 'width: 100%'
      }
    },
    {
      label: '关键词',
      key: 'keyword',
      type: 'input',
      props: {
        placeholder: '搜索消息内容',
        clearable: true
      }
    },
    {
      label: '请求路径',
      key: 'requestPath',
      type: 'input',
      props: {
        placeholder: '请求路径',
        clearable: true
      }
    },
    {
      label: '用户名',
      key: 'userName',
      type: 'input',
      props: {
        placeholder: '用户名',
        clearable: true
      }
    },
    {
      label: 'IP地址',
      key: 'ipAddress',
      type: 'input',
      props: {
        placeholder: 'IP地址',
        clearable: true
      }
    },
    {
      label: '请求ID',
      key: 'requestId',
      type: 'input',
      props: {
        placeholder: '请求ID',
        clearable: true
      }
    }
  ])

  // 重置表单
  function handleReset() {
    emit('reset')
  }

  // 搜索
  async function handleSearch() {
    // 时间跨天验证
    if (formData.value.startTime && formData.value.endTime) {
      const startDate = formData.value.startTime.split(' ')[0]
      const endDate = formData.value.endTime.split(' ')[0]
      if (startDate !== endDate) {
        ElMessage.warning('开始时间和结束时间必须在同一天')
        return
      }
    }
    emit('search', formData.value)
  }
</script>
