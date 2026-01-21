<template>
  <ElDialog v-model="visible" :title="dialogType === 'add' ? '新增角色' : '编辑角色'" width="500px" align-center
    @close="handleClose">
    <ElForm ref="formRef" :model="form" :rules="rules" label-width="120px">
      <ElFormItem label="角色名称" prop="name">
        <ElInput v-model="form.name" placeholder="请输入角色名称" />
      </ElFormItem>
      <ElFormItem label="角色编码" prop="code">
        <ElInput v-model="form.code" placeholder="请输入角色编码" :disabled="dialogType === 'edit'" />
      </ElFormItem>
      <ElFormItem label="描述" prop="description">
        <ElInput v-model="form.description" type="textarea" :rows="3" placeholder="请输入角色描述" />
      </ElFormItem>
      <ElFormItem label="状态" prop="status">
        <ElSelect v-model="form.status" placeholder="请选择状态" style="width: 100%">
          <ElOption v-for="item in statusOptions" :key="item.value" :value="item.value" :label="item.label" />
        </ElSelect>
      </ElFormItem>
    </ElForm>
    <template #footer>
      <ElButton @click="handleClose">取消</ElButton>
      <ElButton type="primary" :loading="submitLoading" @click="handleSubmit">提交</ElButton>
    </template>
  </ElDialog>
</template>

<script setup lang="ts">
import { fetchUpdateRole } from '@/api/system-manage'
import { getEnumOptions, type EnumOption } from '@/utils/dict'
import type { FormInstance, FormRules } from 'element-plus'

interface Props {
  modelValue: boolean
  dialogType: 'add' | 'edit'
  roleData?: any
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void
  (e: 'success'): void
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: false,
  dialogType: 'add',
  roleData: undefined
})

const emit = defineEmits<Emits>()

const formRef = ref<FormInstance>()
const submitLoading = ref(false)
const statusOptions = ref<EnumOption[]>([])

/**
 * 弹窗显示状态双向绑定
 */
const visible = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

/**
 * 表单验证规则
 */
const rules = reactive<FormRules>({
  name: [
    { required: true, message: '请输入角色名称', trigger: 'blur' },
    { min: 2, max: 20, message: '长度在 2 到 20 个字符', trigger: 'blur' }
  ],
  code: [
    { required: true, message: '请输入角色编码', trigger: 'blur' },
    { min: 2, max: 50, message: '长度在 2 到 50 个字符', trigger: 'blur' }
  ]
})

/**
 * 表单数据
 */
const form = reactive<any>({
  id: '',
  name: '',
  code: '',
  description: '',
  status: 1
})

/**
 * 监听弹窗打开，初始化表单数据
 */
watch(
  () => props.modelValue,
  (newVal) => {
    if (newVal) initForm()
  }
)

/**
 * 初始化表单数据
 */
const initForm = () => {
  if (props.dialogType === 'edit' && props.roleData) {
    Object.assign(form, {
      id: props.roleData.id || '',
      name: props.roleData.name || '',
      code: props.roleData.code || '',
      description: props.roleData.description || '',
      status: props.roleData.status ?? 1
    })
  } else {
    Object.assign(form, {
      id: '',
      name: '',
      code: '',
      description: '',
      status: 1
    })
  }
}

/**
 * 关闭弹窗
 */
const handleClose = () => {
  visible.value = false
  formRef.value?.resetFields()
}

/**
 * 提交表单
 */
const handleSubmit = async () => {
  if (!formRef.value) return

  try {
    await formRef.value.validate()
    submitLoading.value = true

    const submitData = {
      id: form.id || undefined,
      name: form.name,
      code: form.code,
      description: form.description || undefined,
      status: form.status
    }

    await fetchUpdateRole(submitData)
    const message = props.dialogType === 'add' ? '新增成功' : '修改成功'
    ElMessage.success(message)
    emit('success')
    handleClose()
  } catch (error) {
    console.log('提交失败:', error)
  } finally {
    submitLoading.value = false
  }
}

// 初始化
onMounted(async () => {
  statusOptions.value = await getEnumOptions('ActiveStatus')
})
</script>
