<template>
  <ElDialog v-model="dialogVisible" :title="dialogType === 'add' ? '添加用户' : '编辑用户'" width="500px" align-center
    @close="handleClose">
    <ElForm ref="formRef" :model="formData" :rules="rules" label-width="80px">
      <ElFormItem label="用户名" prop="username">
        <ElInput v-model="formData.username" placeholder="请输入用户名" :disabled="dialogType === 'edit'" />
      </ElFormItem>
      <ElFormItem v-if="dialogType === 'add'" label="密码" prop="password">
        <ElInput v-model="formData.password" type="password" placeholder="请输入密码" show-password />
      </ElFormItem>
      <ElFormItem label="姓名" prop="realName">
        <ElInput v-model="formData.realName" placeholder="请输入真实姓名" />
      </ElFormItem>
      <ElFormItem label="手机号" prop="phone">
        <ElInput v-model="formData.phone" placeholder="请输入手机号" />
      </ElFormItem>
      <ElFormItem label="角色" prop="roleIds">
        <ElSelect v-model="formData.roleIds" multiple placeholder="请选择角色" style="width: 100%">
          <ElOption v-for="role in roleList" :key="role.id" :value="role.id" :label="role.name" />
        </ElSelect>
      </ElFormItem>
      <ElFormItem label="状态" prop="status">
        <ElSelect v-model="formData.status" placeholder="请选择状态" style="width: 100%">
          <ElOption v-for="item in statusOptions" :key="item.value" :value="item.value" :label="item.label" />
        </ElSelect>
      </ElFormItem>
    </ElForm>
    <template #footer>
      <div class="dialog-footer">
        <ElButton @click="handleClose">取消</ElButton>
        <ElButton type="primary" :loading="submitLoading" @click="handleSubmit">提交</ElButton>
      </div>
    </template>
  </ElDialog>
</template>

<script setup lang="ts">
import { fetchGetRoleSelect } from '@/api/system-manage'
import { getEnumOptions, type EnumOption } from '@/utils/dict'
import type { FormInstance, FormRules } from 'element-plus'

interface Props {
  visible: boolean
  type: string
  userData?: any
}

interface Emits {
  (e: 'update:visible', value: boolean): void
  (e: 'submit', data: any): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

// 角色列表数据
const roleList = ref<any[]>([])
// 状态选项
const statusOptions = ref<EnumOption[]>([])
// 提交loading
const submitLoading = ref(false)

// 对话框显示控制
const dialogVisible = computed({
  get: () => props.visible,
  set: (value) => emit('update:visible', value)
})

const dialogType = computed(() => props.type)

// 表单实例
const formRef = ref<FormInstance>()

// 表单数据
const formData = reactive({
  id: '',
  username: '',
  password: '',
  realName: '',
  phone: '',
  roleIds: [] as string[],
  status: 1
})

// 表单验证规则
const rules = computed<FormRules>(() => ({
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 2, max: 20, message: '长度在 2 到 20 个字符', trigger: 'blur' }
  ],
  password: dialogType.value === 'add' ? [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, max: 20, message: '长度在 6 到 20 个字符', trigger: 'blur' }
  ] : [],
  phone: [
    { pattern: /^1[3-9]\d{9}$/, message: '请输入正确的手机号格式', trigger: 'blur' }
  ]
}))

/**
 * 初始化表单数据
 */
const initFormData = () => {
  const isEdit = props.type === 'edit' && props.userData
  const row = props.userData

  Object.assign(formData, {
    id: isEdit && row ? row.userId || '' : '',
    username: isEdit && row ? row.username || '' : '',
    password: '',
    realName: isEdit && row ? row.realName || '' : '',
    phone: isEdit && row ? row.phone || '' : '',
    roleIds: isEdit && row ? (Array.isArray(row.roleIds) ? row.roleIds : []) : [],
    status: isEdit && row ? row.status : 1
  })
}

/**
 * 加载角色列表
 */
const loadRoleList = async () => {
  try {
    roleList.value = await fetchGetRoleSelect()
  } catch (error) {
    console.error('获取角色列表失败:', error)
  }
}

/**
 * 监听对话框状态变化
 */
watch(
  () => [props.visible, props.type, props.userData],
  async ([visible]) => {
    if (visible) {
      await loadRoleList()
      initFormData()
      nextTick(() => {
        formRef.value?.clearValidate()
      })
    }
  },
  { immediate: true }
)

/**
 * 关闭弹窗
 */
const handleClose = () => {
  dialogVisible.value = false
}

/**
 * 提交表单
 */
const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitLoading.value = true
      try {
        const submitData = {
          id: formData.id || undefined,
          username: formData.username,
          password: formData.password || undefined,
          realName: formData.realName || undefined,
          phone: formData.phone || undefined,
          roleIds: formData.roleIds,
          status: formData.status
        }
        emit('submit', submitData)
      } finally {
        submitLoading.value = false
      }
    }
  })
}

// 初始化状态选项
onMounted(async () => {
  statusOptions.value = await getEnumOptions('ActiveStatus')
})
</script>
