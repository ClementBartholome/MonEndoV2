<script setup lang="ts">
import type { HTMLAttributes } from 'vue'
import { cn } from '@/lib/utils'

const props = defineProps<{
  class?: HTMLAttributes['class']
}>()
</script>

<template>
  <article
    :class="
      cn(
        'h-full rounded-carte border-0 bg-surface text-texte shadow-elevation',
        props.class,
      )
    "
  >
    <slot />
  </article>
</template>
