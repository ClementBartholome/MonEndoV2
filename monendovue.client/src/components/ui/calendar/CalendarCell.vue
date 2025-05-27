<script lang="ts" setup>
import { type HTMLAttributes, computed } from 'vue'
import { CalendarCell, type CalendarCellProps, useForwardProps } from 'radix-vue'
import { cn } from '@/lib/utils'

const props = defineProps<CalendarCellProps & { class?: HTMLAttributes['class'], isJourRegle?: boolean, isJourSpotting?: boolean, isJourFertilite?: boolean }>()

const delegatedProps = computed(() => {
  const { class: _, isJourRegle, isJourSpotting, ...delegated } = props

  return delegated
})

const forwardedProps = useForwardProps(delegatedProps)
</script>

<template>
  <CalendarCell
      v-bind="forwardedProps"
      :class="cn('!p-0')"
  >
    <div :class="cn('tooltip rounded-md relative h-10 w-10 p-0 text-center text-sm focus-within:relative focus-within:z-20 [&:has([data-selected])]:rounded-md', 
    props.class, { 'bg-button': props.isJourRegle, 'bg-red-300': props.isJourSpotting, 
    // 'bg-blue-300': props.isJourFertilite 
    })"
         :title="props.isJourRegle ? 'Règle' : ''"
         >
      <span v-if="props.isJourRegle" class="tooltiptext">{{props.isJourRegle ? 'Règle' : ''}}</span>
      <span v-if="props.isJourSpotting" class="tooltiptext">{{props.isJourSpotting ? 'Spotting' : ''}}</span>
      <slot />
    </div>
  </CalendarCell>
</template>

<style>
[data-focused] {
  background-color: transparent !important;
  color: inherit !important;
}

.bg-button {
  background-color: var(--button) !important;
}

.tooltip {
  position: relative;
  display: inline-block;
}

/* Tooltip text */
.tooltip .tooltiptext {
  visibility: hidden;
  width: 80px;
  background-color: #555;
  color: #fff;
  text-align: center;
  padding: 5px 0;
  border-radius: 6px;

  /* Position the tooltip text */
  position: absolute;
  z-index: 1;
  bottom: 125%;
  left: 50%;
  margin-left: -40px;

  /* Fade in tooltip */
  opacity: 0;
  transition: opacity 0.3s;
}

/* Tooltip arrow */
.tooltip .tooltiptext::after {
  content: "";
  position: absolute;
  top: 100%;
  left: 50%;
  margin-left: -5px;
  border-width: 5px;
  border-style: solid;
  border-color: #555 transparent transparent transparent;
}

/* Show the tooltip text when you mouse over the tooltip container */
.tooltip:hover .tooltiptext {
  visibility: visible;
  opacity: 1;
}
</style>