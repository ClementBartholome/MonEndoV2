<script lang="ts" setup>
import {type HTMLAttributes, computed} from 'vue'
import {CalendarRoot, type CalendarRootEmits, type CalendarRootProps, useForwardPropsEmits} from 'radix-vue'
import {
  CalendarCell,
  CalendarCellTrigger,
  CalendarGrid,
  CalendarGridBody,
  CalendarGridHead,
  CalendarGridRow,
  CalendarHeadCell,
} from '.'
import {cn} from '@/lib/utils'

const props = defineProps<CalendarRootProps & {
  class?: HTMLAttributes['class'],
  joursRegles?: Date[],
  joursSpotting?: Date[],
  joursFertiles?: Date[]
}>()

const emits = defineEmits<{
  'update:modelValue': [value: string]
}>()

const delegatedProps = computed(() => {
  const {class: _, joursRegles, joursSpotting, joursFertiles, ...delegated} = props
  return delegated
})

const handleInput = (event: Event) => {
  const value = (event.target as HTMLInputElement).value
  emits('update:modelValue', value)
}

const forwarded = useForwardPropsEmits(delegatedProps, emits)

const isJourRegle = (date: Date) => {
  const dateObj = new Date(date)
  return props.joursRegles?.some(jour => jour.toDateString() === dateObj.toDateString());
}

const isJourSpotting = (date: Date) => {
  const dateObj = new Date(date)
  return props.joursSpotting?.some(jour => jour.toDateString() === dateObj.toDateString());
}

const isJourFertilite = (date: Date) => {
  const dateObj = new Date(date)
  return props.joursFertiles?.some(jour => jour.toDateString() === dateObj.toDateString());
}
</script>

<template>
  <CalendarRoot
      v-slot="{ grid, weekDays }"
      :class="cn('p-3', props.class)"
      v-bind="forwarded"
      locale="fr"
  >
<!--    <CalendarHeader>-->
<!--      <CalendarPrevButton/>-->
<!--      <CalendarHeading/>-->
<!--      <CalendarNextButton/>-->
<!--    </CalendarHeader>-->

    <div class="flex flex-col gap-y-4 mt-4 sm:flex-row sm:gap-x-4 sm:gap-y-0">
      <CalendarGrid v-for="month in grid" :key="month.value.toString()">
        <input type="hidden" name="selectedMonth" 
               :value="`${String(month.value).split('-')[0]}-${String(month.value).split('-')[1]}`"
               @input="handleInput">
        <CalendarGridHead>
          <CalendarGridRow>
            <CalendarHeadCell
                v-for="day in weekDays" :key="day"
            >
              {{ day }}
            </CalendarHeadCell>
          </CalendarGridRow>
        </CalendarGridHead>
        <CalendarGridBody>
          <CalendarGridRow v-for="(weekDates, index) in month.rows" :key="`weekDate-${index}`" class="mt-2 w-full">
            <CalendarCell
                v-for="weekDate in weekDates"
                :key="weekDate.toString()"
                :date="weekDate"
                :isJourRegle="isJourRegle(weekDate)"
                :isJourSpotting="isJourSpotting(weekDate)"
                :isJourFertilite="isJourFertilite(weekDate)"
            >
              <CalendarCellTrigger
                  :day="weekDate"
                  :month="month.value"
              />
            </CalendarCell>
          </CalendarGridRow>
        </CalendarGridBody>
      </CalendarGrid>
    </div>
  </CalendarRoot>
</template>