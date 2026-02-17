---
layout: home
hero: false
---

<script setup>
import { onMounted } from 'vue'
import { useRouter } from 'vitepress'

onMounted(() => {
  const router = useRouter()
  const lang = navigator.language?.startsWith('zh') ? '/zh/' : '/en/'
  router.go(lang)
})
</script>

<div style="display: flex; justify-content: center; align-items: center; height: 50vh;">
  <p>Redirecting...</p>
</div>
