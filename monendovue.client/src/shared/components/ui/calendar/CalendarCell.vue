<script lang="ts" setup>
import { type HTMLAttributes, computed } from 'vue'
import { CalendarCell, type CalendarCellProps, useForwardProps } from 'radix-vue'
import { cn } from '@/lib/utils'

const props = defineProps<CalendarCellProps & { class?: HTMLAttributes['class'], symptomTypes?: string[] }>()

const delegatedProps = computed(() => {
  const { class: _, symptomTypes, ...delegated } = props

  return delegated
})

const forwardedProps = useForwardProps(delegatedProps)

// Define colors for each symptom type
const symptomColors: Record<string, string> = {
  regle: 'bg-red-500',
  spotting: 'bg-red-300',
  acne: 'bg-purple-500',
  // fertilite: 'bg-blue-500'
}

const symptomLabels: Record<string, string> = {
  regle: 'Règle',
  spotting: 'Spotting',
  acne: 'Acné',
  // fertilite: 'Fertilité'
}

const tooltipText = computed(() => {
  if (!props.symptomTypes || props.symptomTypes.length === 0) return ''
  return props.symptomTypes.map(type => symptomLabels[type] || type).join(', ')
})
</script>

<template>
  <CalendarCell
      v-bind="forwardedProps"
      :class="cn('!p-0')"
  >
    <div class="tooltip rounded-md relative h-10 w-10 p-0 text-center text-sm focus-within:relative focus-within:z-20 [&:has([data-selected])]:rounded-md flex items-end justify-center pb-1"
         :title="tooltipText"
         >
      <!-- Show colored dots for symptoms -->
      <div v-if="symptomTypes && symptomTypes.length > 0" class="flex gap-1 absolute bottom-1">
        <div
          v-for="symptom in symptomTypes.slice(0, 3)"
          :key="symptom"
          :class="cn('w-1.5 h-1.5 rounded-full', symptomColors[symptom] || 'bg-gray-400')"
        ></div>
      </div>

      <span v-if="tooltipText" class="tooltiptext">{{ tooltipText }}</span>
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
  width: 120px;
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
  margin-left: -60px;

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
