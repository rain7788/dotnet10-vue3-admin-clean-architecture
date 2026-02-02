<template>
  <ElDialog v-model="visible" title="菜单权限" width="520px" align-center class="el-dialog-border" @close="handleClose">
    <ElScrollbar height="70vh">
      <ElTree ref="treeRef" :data="processedMenuList" show-checkbox node-key="id" :default-expand-all="isExpandAll"
        :default-checked-keys="checkedKeys" :props="defaultProps" @check="handleTreeCheck">
        <template #default="{ data }">
          <div style="display: flex; align-items: center">
            <span v-if="data.isAuth">
              {{ data.label }}
            </span>
            <span v-else>{{ data.name || data.label }}</span>
          </div>
        </template>
      </ElTree>
    </ElScrollbar>
    <template #footer>
      <ElButton @click="toggleExpandAll">{{ isExpandAll ? '全部收起' : '全部展开' }}</ElButton>
      <ElButton @click="toggleSelectAll" style="margin-left: 8px">{{
        isSelectAll ? '取消全选' : '全部选择'
        }}</ElButton>
      <ElButton type="primary" :loading="submitLoading" @click="savePermission">保存</ElButton>
    </template>
  </ElDialog>
</template>

<script setup lang="ts">
import { fetchGetMenuTree, fetchUpdateRole } from '@/api/system-manage'

interface Props {
  modelValue: boolean
  roleData?: any
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void
  (e: 'success'): void
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: false,
  roleData: undefined
})

const emit = defineEmits<Emits>()

const treeRef = ref()
const isExpandAll = ref(true)
const isSelectAll = ref(false)
const submitLoading = ref(false)
const menuList = ref<any[]>([])
const checkedKeys = ref<string[]>([])

/**
 * 弹窗显示状态双向绑定
 */
const visible = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

/**
 * 菜单节点类型
 */
interface MenuNode {
  id?: string
  name?: string
  label?: string
  permissions?: any[]
  children?: MenuNode[]
  [key: string]: any
}

/**
 * 处理菜单数据，将权限列表转换为子节点
 */
const processedMenuList = computed(() => {
  const processNode = (node: MenuNode): MenuNode => {
    const processed = { ...node }

    // 如果有权限列表，将其转换为子节点
    if (node.permissions?.length) {
      const authNodes = node.permissions.map((perm: any) => ({
        id: perm.id,
        label: perm.name,
        code: perm.code,
        isAuth: true
      }))

      processed.children = processed.children ? [...processed.children, ...authNodes] : authNodes
    }

    // 递归处理子节点
    if (processed.children) {
      processed.children = processed.children.map(processNode)
    }

    return processed
  }

  return menuList.value.map(processNode)
})

/**
 * 树形组件配置
 */
const defaultProps = {
  children: 'children',
  label: (data: any) => data.name || data.label || ''
}

/**
 * 加载菜单树
 */
const loadMenuTree = async () => {
  try {
    menuList.value = await fetchGetMenuTree()
  } catch (error) {
    console.error('获取菜单树失败:', error)
  }
}

/**
 * 监听弹窗打开，初始化权限数据
 */
watch(
  () => props.modelValue,
  async (newVal) => {
    if (newVal) {
      await loadMenuTree()
      if (props.roleData) {
        // 设置当前角色已选中的菜单和权限
        const menuIds = props.roleData.menuIds || []
        const permissionIds = props.roleData.permissionIds || []

        // 等待树渲染后设置选中状态
        // 重要：只设置叶子节点的 key，避免父节点级联选中所有子节点
        nextTick(() => {
          const tree = treeRef.value
          if (!tree) return

          // 获取所有叶子节点的 key（从 menuIds 和 permissionIds 中筛选）
          const allKeys = [...menuIds, ...permissionIds]
          const leafKeys = allKeys.filter((key) => {
            const node = tree.getNode(key)
            // 只保留叶子节点（没有子节点或子节点为空）
            return node && (!node.childNodes || node.childNodes.length === 0)
          })

          tree.setCheckedKeys(leafKeys)
        })
      }
    }
  }
)

/**
 * 关闭弹窗
 */
const handleClose = () => {
  visible.value = false
  checkedKeys.value = []
  treeRef.value?.setCheckedKeys([])
}

/**
 * 保存权限配置
 */
const savePermission = async () => {
  if (!props.roleData) return

  submitLoading.value = true
  try {
    const tree = treeRef.value
    if (!tree) return

    // 获取选中的节点（包含半选）
    const checkedNodes = tree.getCheckedNodes()
    const halfCheckedNodes = tree.getHalfCheckedNodes()
    const allNodes = [...checkedNodes, ...halfCheckedNodes]

    // 分离菜单和权限
    const menuIds: string[] = []
    const permissionIds: string[] = []

    allNodes.forEach((node: any) => {
      if (node.isAuth) {
        permissionIds.push(node.id)
      } else {
        menuIds.push(node.id)
      }
    })

    // 更新角色权限
    await fetchUpdateRole({
      id: props.roleData.id,
      menuIds,
      permissionIds
    })

    ElMessage.success('权限保存成功')
    emit('success')
    handleClose()
  } catch (error) {
    console.error('保存权限失败:', error)
  } finally {
    submitLoading.value = false
  }
}

/**
 * 切换全部展开/收起状态
 */
const toggleExpandAll = () => {
  const tree = treeRef.value
  if (!tree) return

  const nodes = tree.store.nodesMap
  Object.values(nodes).forEach((node: any) => {
    node.expanded = !isExpandAll.value
  })

  isExpandAll.value = !isExpandAll.value
}

/**
 * 切换全选/取消全选状态
 */
const toggleSelectAll = () => {
  const tree = treeRef.value
  if (!tree) return

  if (!isSelectAll.value) {
    const allKeys = getAllNodeKeys(processedMenuList.value)
    tree.setCheckedKeys(allKeys)
  } else {
    tree.setCheckedKeys([])
  }

  isSelectAll.value = !isSelectAll.value
}

/**
 * 递归获取所有节点的 key
 */
const getAllNodeKeys = (nodes: MenuNode[]): string[] => {
  const keys: string[] = []
  const traverse = (nodeList: MenuNode[]): void => {
    nodeList.forEach((node) => {
      if (node.id) keys.push(node.id)
      if (node.children?.length) traverse(node.children)
    })
  }
  traverse(nodes)
  return keys
}

/**
 * 处理树节点选中状态变化
 */
const handleTreeCheck = () => {
  const tree = treeRef.value
  if (!tree) return

  const checkedKeys = tree.getCheckedKeys()
  const allKeys = getAllNodeKeys(processedMenuList.value)

  isSelectAll.value = checkedKeys.length === allKeys.length && allKeys.length > 0
}
</script>
