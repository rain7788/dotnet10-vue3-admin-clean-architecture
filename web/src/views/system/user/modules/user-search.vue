<template>
  <ArtSearchBar ref="searchBarRef" v-model="formData" :items="formItems" :rules="rules" @reset="handleReset"
    @search="handleSearch">
  </ArtSearchBar>
</template>

<script setup lang="ts">
import { getEnumOptions, type EnumOption } from '@/utils/dict'

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

// 校验规则
const rules = {
  // userName: [{ required: true, message: '请输入用户名', trigger: 'blur' }]
}

// 动态 options - 从字典加载
const statusOptions = ref<{ label: string; value: number }[]>([])

// 加载状态枚举
onMounted(async () => {
  const options = await getEnumOptions('ActiveStatus')
  statusOptions.value = options.map(opt => ({ label: opt.label, value: opt.value }))
})

// 表单配置 - 使用 getter 函数让 options 保持响应式
const formItems = computed(() => [
  {
    label: '用户名',
    key: 'username',
    type: 'input',
    placeholder: '请输入用户名',
    clearable: true
  },
  {
    label: '姓名',
    key: 'realName',
    type: 'input',
    props: { placeholder: '请输入姓名' }
  },
  {
    label: '状态',
    key: 'status',
    type: 'select',
    props: {
      placeholder: '请选择状态',
      options: statusOptions.value
    }
  }
])

// 事件
function handleReset() {
  console.log('重置表单')
  emit('reset')
}

async function handleSearch() {
  await searchBarRef.value.validate()
  emit('search', formData.value)
  console.log('表单数据', formData.value)
}
</script>
