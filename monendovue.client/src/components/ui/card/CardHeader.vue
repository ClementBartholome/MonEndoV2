<script setup lang="ts">
import type { HTMLAttributes } from 'vue'
import { cn } from '@/lib/utils'

const props = defineProps<{
  class?: HTMLAttributes['class']
}>()
</script>

<template>
  <h3 :class="cn('flex items-center gap-2 p-2', props.class)">
    <slot />
  </h3>
</template>
