<script setup lang="ts">
defineProps<{
  title: string
  description?: string
  actionLabel?: string
}>()

const emit = defineEmits<{
  action: []
}>()
</script>

<template>
  <div class="flex flex-col justify-center items-center text-center h-40 px-4 rounded-xl border border-dashed border-gray-300 bg-white/50">
    <p class="text-lg text-headline font-medium">{{ title }}</p>
    <p v-if="description" class="text-sm text-muted-foreground mt-1">{{ description }}</p>
    <button
      v-if="actionLabel"
      type="button"
      class="mt-3 px-4 py-2 rounded-lg bg-button text-button text-sm font-medium hover:opacity-90 transition-opacity"
      @click="emit('action')"
    >
      {{ actionLabel }}
    </button>
  </div>
</template>

