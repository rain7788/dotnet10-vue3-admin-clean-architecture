<!-- 修改密码弹窗 -->
<template>
    <ElDialog v-model="visible" title="修改密码" width="450px" :close-on-click-modal="false" @close="handleClose">
        <ElForm ref="formRef" :model="form" :rules="rules" label-width="100px" label-position="right">
            <ElFormItem label="当前密码" prop="oldPassword">
                <ElInput v-model="form.oldPassword" type="password" placeholder="请输入当前密码" show-password clearable />
            </ElFormItem>
            <ElFormItem label="新密码" prop="newPassword">
                <ElInput v-model="form.newPassword" type="password" placeholder="请输入新密码" show-password clearable />
            </ElFormItem>
            <ElFormItem label="确认新密码" prop="confirmPassword">
                <ElInput v-model="form.confirmPassword" type="password" placeholder="请再次输入新密码" show-password
                    clearable />
            </ElFormItem>
        </ElForm>
        <template #footer>
            <div class="dialog-footer">
                <ElButton @click="handleClose">取消</ElButton>
                <ElButton type="primary" :loading="loading" @click="handleSubmit" v-ripple>
                    确定
                </ElButton>
            </div>
        </template>
    </ElDialog>
</template>

<script setup lang="ts">
import { fetchChangePassword } from '@/api/system-manage'
import type { FormInstance, FormRules } from 'element-plus'

defineOptions({ name: 'ChangePasswordDialog' })

const visible = defineModel<boolean>('visible', { default: false })

const formRef = ref<FormInstance>()
const loading = ref(false)

const form = reactive({
    oldPassword: '',
    newPassword: '',
    confirmPassword: ''
})

const validateConfirmPassword = (
    _rule: any,
    value: string,
    callback: (error?: Error) => void
) => {
    if (!value) {
        callback(new Error('请再次输入新密码'))
        return
    }
    if (value !== form.newPassword) {
        callback(new Error('两次输入的新密码不一致'))
        return
    }
    callback()
}

const rules = reactive<FormRules>({
    oldPassword: [{ required: true, message: '请输入当前密码', trigger: 'blur' }],
    newPassword: [
        { required: true, message: '请输入新密码', trigger: 'blur' },
        { min: 6, message: '密码长度不能小于6位', trigger: 'blur' }
    ],
    confirmPassword: [{ required: true, validator: validateConfirmPassword, trigger: 'blur' }]
})

const handleSubmit = async () => {
    if (!formRef.value) return
    const valid = await formRef.value.validate()
    if (!valid) return

    try {
        loading.value = true
        await fetchChangePassword({
            oldPassword: form.oldPassword,
            newPassword: form.newPassword
        })
        ElMessage.success('密码修改成功')
        handleClose()
    } catch (error) {
        // 错误已由 http 拦截器处理
    } finally {
        loading.value = false
    }
}

const handleClose = () => {
    visible.value = false
    formRef.value?.resetFields()
    form.oldPassword = ''
    form.newPassword = ''
    form.confirmPassword = ''
}
</script>

<style scoped>
.dialog-footer {
    display: flex;
    justify-content: flex-end;
    gap: 10px;
}
</style>
