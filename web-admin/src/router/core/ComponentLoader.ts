/**
 * 组件加载器
 *
 * 负责动态加载 Vue 组件，包含错误处理和容错机制
 *
 * @module router/core/ComponentLoader
 * @author Art Design Pro Team
 */

import { h, type Component } from 'vue'

export class ComponentLoader {
  private modules: Record<string, () => Promise<any>>

  constructor() {
    // 动态导入 views 目录下所有 .vue 组件
    this.modules = import.meta.glob('../../views/**/*.vue')
  }

  /**
   * 加载组件（带错误处理）
   */
  load(componentPath: string): () => Promise<Component> {
    if (!componentPath) {
      // 组件路径为空，返回错误占位组件
      return this.createErrorComponent('(空路径)')
    }

    // 构建可能的路径
    const fullPath = `../../views${componentPath}.vue`
    const fullPathWithIndex = `../../views${componentPath}/index.vue`

    // 先尝试直接路径，再尝试添加/index的路径
    const module = this.modules[fullPath] || this.modules[fullPathWithIndex]

    if (!module) {
      console.error(
        `[ComponentLoader] 未找到组件: ${componentPath}，尝试过的路径: ${fullPath} 和 ${fullPathWithIndex}`
      )
      return this.createErrorComponent(componentPath)
    }

    // 包装模块加载，捕获运行时错误
    const errorComponent = this.createErrorComponentDefinition(componentPath)
    return () =>
      module().catch((error: Error) => {
        console.error(`[ComponentLoader] 加载组件失败: ${componentPath}`, error)
        return errorComponent
      })
  }

  /**
   * 加载布局组件
   */
  loadLayout(): () => Promise<any> {
    return () => import('@/views/index/index.vue')
  }

  /**
   * 加载 iframe 组件
   */
  loadIframe(): () => Promise<any> {
    return () => import('@/views/outside/Iframe.vue')
  }

  /**
   * 创建空组件
   */
  private createEmptyComponent(): () => Promise<any> {
    return () =>
      Promise.resolve({
        render() {
          return h('div', {})
        }
      })
  }

  /**
   * 创建错误提示组件（用于路由配置时的静态检查）
   */
  private createErrorComponent(componentPath: string): () => Promise<any> {
    const definition = this.createErrorComponentDefinition(componentPath)
    return () => Promise.resolve(definition)
  }

  /**
   * 创建错误组件定义（可复用）
   */
  private createErrorComponentDefinition(componentPath: string): Component {
    return {
      render() {
        return h(
          'div',
          {
            style: {
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              height: '100%',
              minHeight: '300px',
              color: '#666',
              fontSize: '14px',
              padding: '20px'
            }
          },
          [
            h(
              'div',
              {
                style: {
                  fontSize: '48px',
                  marginBottom: '16px'
                }
              },
              '🔍'
            ),
            h(
              'div',
              {
                style: {
                  fontWeight: 'bold',
                  marginBottom: '8px',
                  color: '#f56c6c'
                }
              },
              '组件未找到'
            ),
            h(
              'div',
              {
                style: {
                  color: '#909399',
                  wordBreak: 'break-all',
                  textAlign: 'center'
                }
              },
              `路径: ${componentPath}`
            ),
            h(
              'div',
              {
                style: {
                  marginTop: '16px',
                  color: '#909399',
                  fontSize: '12px'
                }
              },
              '请检查菜单配置中的组件路径是否正确'
            )
          ]
        )
      }
    }
  }
}
