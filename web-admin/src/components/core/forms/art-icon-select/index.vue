<template>
    <div class="icon-select-wrapper">
        <ElInput v-model="displayValue" :placeholder="placeholder" readonly class="icon-select-input"
            @click="dialogVisible = true">
            <template #prefix>
                <ArtSvgIcon v-if="modelValue" :icon="modelValue" class="text-lg" />
            </template>
            <template #suffix>
                <ElIcon v-if="modelValue && clearable" class="cursor-pointer" @click.stop="handleClear">
                    <CircleClose />
                </ElIcon>
            </template>
        </ElInput>

        <!-- 图标选择弹窗 -->
        <ElDialog v-model="dialogVisible" title="选择图标" width="800px" align-center :close-on-click-modal="false"
            @opened="handleDialogOpened" @closed="handleDialogClosed">
            <!-- 搜索框 -->
            <ElInput ref="searchInputRef" v-model="searchText" placeholder="搜索图标名称..." clearable class="mb-3">
                <template #prefix>
                    <ArtSvgIcon icon="ri:search-line" />
                </template>
                <template #suffix>
                    <span class="text-xs text-gray-400">共 {{ totalCount }} 个图标</span>
                </template>
            </ElInput>

            <!-- 图标列表 -->
            <ElScrollbar height="400px">
                <div v-if="filteredIcons.length" class="icon-grid">
                    <div v-for="icon in filteredIcons" :key="icon" class="icon-item"
                        :class="{ active: modelValue === `ri:${icon}` }" :title="icon" @click="handleSelect(icon)">
                        <ArtSvgIcon :icon="`ri:${icon}`" class="icon-svg" />
                    </div>
                    <!-- 占位元素，补齐最后一行 -->
                    <div v-for="n in placeholderCount" :key="`placeholder-${n}`" class="icon-placeholder"></div>
                </div>
                <ElEmpty v-else description="未找到匹配的图标" :image-size="60" />
            </ElScrollbar>

            <!-- 分页信息 -->
            <div class="mt-3 text-xs text-gray-400 flex justify-between items-center">
                <span v-if="searchText">找到 {{ filteredIcons.length }} 个匹配图标</span>
                <span v-else>显示前 300 个常用图标，输入关键词搜索更多</span>
            </div>
        </ElDialog>
    </div>
</template>

<script setup lang="ts">
import { CircleClose } from '@element-plus/icons-vue'
import riIcons from '@iconify-json/ri/icons.json'

interface Props {
    modelValue?: string
    placeholder?: string
    clearable?: boolean
}

interface Emits {
    (e: 'update:modelValue', value: string): void
    (e: 'change', value: string): void
}

const props = withDefaults(defineProps<Props>(), {
    modelValue: '',
    placeholder: '请选择图标',
    clearable: true
})

const emit = defineEmits<Emits>()

const dialogVisible = ref(false)
const searchText = ref('')
const searchInputRef = ref()

// 获取所有图标名称
const allIcons = computed(() => {
    return Object.keys(riIcons.icons || {})
})

// 线条风格图标（更清爽）
const lineIcons = computed(() => {
    return allIcons.value.filter((name) => name.endsWith('-line'))
})

// 图标总数
const totalCount = computed(() => allIcons.value.length)

// 每行显示数量
const columnsPerRow = 10

// 过滤后的图标列表
const filteredIcons = computed(() => {
    const keyword = searchText.value.toLowerCase().trim()
    if (!keyword) {
        // 没有搜索时，显示线条风格图标（前300个）
        return lineIcons.value.slice(0, 300)
    }
    // 搜索时优先显示线条风格，再显示填充风格
    const matchedLine = lineIcons.value.filter((name) => name.toLowerCase().includes(keyword))
    const matchedFill = allIcons.value.filter((name) => name.endsWith('-fill') && name.toLowerCase().includes(keyword))
    return [...matchedLine, ...matchedFill]
})

// 计算最后一行需要的占位元素数量
const placeholderCount = computed(() => {
    const remainder = filteredIcons.value.length % columnsPerRow
    return remainder === 0 ? 0 : columnsPerRow - remainder
})

// 显示值
const displayValue = computed(() => {
    if (!props.modelValue) return ''
    return props.modelValue.replace('ri:', '')
})

/**
 * 选择图标
 */
const handleSelect = (iconName: string): void => {
    const fullName = `ri:${iconName}`
    emit('update:modelValue', fullName)
    emit('change', fullName)
    dialogVisible.value = false
}

/**
 * 清空选择
 */
const handleClear = (): void => {
    emit('update:modelValue', '')
    emit('change', '')
}

/**
 * 弹窗打开后聚焦搜索框
 */
const handleDialogOpened = (): void => {
    nextTick(() => {
        searchInputRef.value?.focus()
    })
}

/**
 * 弹窗关闭后清空搜索
 */
const handleDialogClosed = (): void => {
    searchText.value = ''
}
</script>

<style scoped lang="scss">
.icon-select-wrapper {
    width: 100%;
}

.icon-select-input {
    cursor: pointer;

    :deep(.el-input__wrapper) {
        cursor: pointer;
    }
}

.icon-grid {
    display: grid;
    grid-template-columns: repeat(10, 1fr);
    gap: 0;
    border-left: 1px solid var(--el-border-color);
    border-top: 1px solid var(--el-border-color);
    width: 100%;
}

.icon-item {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 12px;
    cursor: pointer;
    transition: all 0.15s;
    color: var(--el-text-color-primary);
    border-right: 1px solid var(--el-border-color);
    border-bottom: 1px solid var(--el-border-color);
    aspect-ratio: 1;
    background-color: transparent;

    &:hover {
        background-color: var(--el-color-primary-light-9);
        color: var(--el-color-primary);
        z-index: 1;
        position: relative;
    }

    &.active {
        background-color: var(--el-color-primary-light-8);
        color: var(--el-color-primary);
        z-index: 2;
        position: relative;
    }

    .icon-svg {
        font-size: 24px;
        flex-shrink: 0;
    }
}

.icon-placeholder {
    border-right: 1px solid var(--el-border-color);
    border-bottom: 1px solid var(--el-border-color);
    aspect-ratio: 1;
}
</style>
