<script lang="ts" setup>
import { type HTMLAttributes, computed } from 'vue'
import { CalendarCellTrigger, type CalendarCellTriggerProps, useForwardProps } from 'radix-vue'
import { buttonVariants } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const props = defineProps<CalendarCellTriggerProps & { class?: HTMLAttributes['class'] }>()

const delegatedProps = computed(() => {
  const { class: _, ...delegated } = props

  return delegated
})

const forwardedProps = useForwardProps(delegatedProps)
</script>

<template>
  <CalendarCellTrigger
    :class="cn(
      buttonVariants({ variant: 'ghost' }),
      'h-10 w-10 p-0 font-normal',
      '[&[data-today]:not([data-selected])] [&[data-today]:not([data-selected])]:text-accent-foreground',
      // Selected
      'data-[selected]:text-primary-foreground data-[selected]:opacity-100 data-[selected]:hover:text-primary-foreground  data-[selected]:focus:text-primary-foreground',
      // Disabled
      'data-[disabled]:text-muted-foreground data-[disabled]:opacity-50',
      // Unavailable
      'data-[unavailable]:text-destructive-foreground data-[unavailable]:line-through',
      // Outside months
      'data-[outside-month]:pointer-events-none data-[outside-month]:text-muted-foreground data-[outside-month]:opacity-50 [&[data-outside-month] [&[data-outside-month][data-selected]]:text-muted-foreground [&[data-outside-month][data-selected]]:opacity-30',
      props.class,
    )"
    v-bind="forwardedProps"
  >
    <slot />
  </CalendarCellTrigger>
</template>
