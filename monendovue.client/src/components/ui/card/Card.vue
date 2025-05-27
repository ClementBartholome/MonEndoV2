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
        'min-h-36 rounded-3xl border-0 bg-card text-card-foreground shadow-xl h-full transition-shadow duration-300 cursor-pointer hover:shadow-lg hover:shadow-gray-400',
        props.class,
      )
    "
  >
    <slot />
  </article>
</template>
