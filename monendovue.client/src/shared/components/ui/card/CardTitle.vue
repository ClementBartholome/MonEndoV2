<script setup lang="ts">
import type { HTMLAttributes } from 'vue'
import { cn } from '@/lib/utils'

const props = defineProps<{
  class?: HTMLAttributes['class']
}>()
</script>

<template>
  <!-- h2 : une carte est posée sous le titre de sa page (h1) ; un h3 sautait un niveau (règle heading-order d'axe). -->
  <h2
    :class="
      cn('text-xl leading-none m-2', props.class)
    "
  >
    <slot />
  </h2>
</template>
