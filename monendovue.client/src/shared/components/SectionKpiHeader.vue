<script setup lang="ts">
export interface KpiItem {
  label: string
  value: string | number
  helper?: string
}

defineProps<{
  items: KpiItem[]
}>()
</script>

<template>
  <div class="grid grid-cols-2 md:grid-cols-3 gap-2 mt-4 mb-3">
    <div
      v-for="item in items"
      :key="item.label"
      class="bg-white border border-gray-100 rounded-xl p-3 text-center shadow-sm"
    >
      <p class="text-xs text-muted-foreground">{{ item.label }}</p>
      <p class="text-xl font-bold text-headline">{{ item.value }}</p>
      <p v-if="item.helper" class="text-[11px] text-muted-foreground mt-0.5">{{ item.helper }}</p>
    </div>
  </div>
</template>

