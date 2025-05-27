<script setup lang="ts">
import {type HTMLAttributes, computed, ref} from 'vue'
import type {SliderRootEmits, SliderRootProps} from 'radix-vue'
import {SliderRange, SliderRoot, SliderThumb, SliderTrack, useForwardPropsEmits} from 'radix-vue'
import {cn} from '@/lib/utils'

const props = defineProps<SliderRootProps & { class?: HTMLAttributes['class'] }>()
const emits = defineEmits<SliderRootEmits>()

const delegatedProps = computed(() => {
  const {class: _, ...delegated} = props

  return delegated
})

const forwarded = useForwardPropsEmits(delegatedProps, emits)
const modelValue = ref([5]);
</script>

<template>
  <div class="relative">
    <div class="absolute left-0 right-0 top-4 text-center">
      {{ modelValue.toString() }}
    </div>
    <SliderRoot
        :class="cn(
      'relative flex w-full touch-none select-none items-center',
      props.class,
    )"
        v-bind="forwarded"
        v-model="modelValue"
    >
      <SliderTrack class="relative h-2 w-full grow overflow-hidden rounded-full bg-white">
        <SliderRange class="absolute h-full slider"/>
      </SliderTrack>
      <SliderThumb 
          class="block h-5 w-5 rounded-full border-2 slider-ring bg-background ring-offset-background transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50"
      />
    </SliderRoot>
  </div>
</template>
