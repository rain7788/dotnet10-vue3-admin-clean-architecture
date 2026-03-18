import { ref } from 'vue'

interface UpgradeLog {
  version: string // 版本号
  title: string // 更新标题
  date: string // 更新日期
  detail?: string[] // 更新内容
  requireReLogin?: boolean // 是否需要重新登录
  remark?: string // 备注
}

export const upgradeLogList = ref<UpgradeLog[]>([
  {
    version: 'v3.0.2',
    title: '问题修复、表单与路由体验优化',
    date: '2026-03-15',
    detail: [
      '修复：富文本编辑器样式异常问题',
      '修复：菜单区域无法滚动的问题',
      '修复：特殊路由打开后显示空白页面的问题',
      '修复：WebSocket 重连异常问题',
      '修复：特殊动态路由参数处理异常问题',
      '优化：ArtForm、ArtSearchBar 表单提交前增加数据清洗，避免无效字段提交到后端',
      '修复：ArtTable 与 ElForm 组合使用时动态表单校验错误的问题',
      '修复：隐藏子菜单时父级菜单被一并隐藏的问题',
      '修复：静态路由刷新后跳回首页的问题',
      '修复：art-table 属性继承异常问题',
      '修复：PC 端切换到移动端后再切回 PC 端，菜单无法恢复原桌面布局的问题'
    ]
  }
])
