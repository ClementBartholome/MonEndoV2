<template>
  <div class="week-picker w-full">
    <label v-if="label" :for="id">{{ label }}</label>
    <div class="input-with-icon">
      <input
          :id="id"
          :value="formattedValue"
          @focus="showCalendar = true"
          readonly
          class="week-input text-sm"
      />
      <span class="material-symbols-outlined">
        calendar_month
    </span>
    </div>
    <div v-if="showCalendar" class="calendar">
      <div class="calendar-header">
        <button @click="changeMonth(-1)" class="btn">&lt;</button>
        <span>{{ currentMonthName }} {{ currentYear }}</span>
        <button @click="changeMonth(1)" class="btn">&gt;</button>
      </div>
      <div class="weekdays">
        <span v-for="day in weekdays" :key="day">{{ day }}</span>
      </div>
      <div class="days">
        <div
            v-for="({ date, isCurrentMonth, isSelected, weekNumber }, index) in calendarDays"
            :key="date.toISOString()"
            :class="[
            'day',
            { 'current-month': isCurrentMonth },
            { 'selected': isSelected }
          ]"
            @click="selectWeek(date, weekNumber)"
        >
          {{ date.getDate() }}
          <span v-if="index % 7 === 0" class="week-number">S{{ weekNumber }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue';

const props = defineProps({
  modelValue: String,
  label: String,
  id: {
    type: String,
    default: () => `week-picker-${Math.random().toString(36).substr(2, 9)}`
  }
});

const emit = defineEmits(['update:modelValue', 'update:years']);

const showCalendar = ref(false);
const currentDate = ref(new Date());
const selectedDate = ref(null);

const weekdays = ['Lun', 'Mar', 'Mer', 'Jeu', 'Ven', 'Sam', 'Dim'];

const currentYear = computed(() => currentDate.value.getFullYear());
const currentMonth = computed(() => currentDate.value.getMonth());
const currentMonthName = computed(() => {
  return new Intl.DateTimeFormat('fr-FR', { month: 'long' }).format(currentDate.value);
});

const formattedValue = computed(() => {
  if (!selectedDate.value) return '';
  const startOfWeek = new Date(selectedDate.value);
  startOfWeek.setDate(selectedDate.value.getDate() - selectedDate.value.getDay() + 1);
  const endOfWeek = new Date(startOfWeek);
  endOfWeek.setDate(startOfWeek.getDate() + 6);
  const year = endOfWeek.getFullYear();
  const weekNumber = getWeekNumber(selectedDate.value);
  return `Semaine ${weekNumber}, ${year}`;
});

const internalValue = computed(() => {
  if (!selectedDate.value) return '';
  const year = selectedDate.value.getFullYear();
  const weekNumber = getWeekNumber(selectedDate.value);
  return `${year}-W${weekNumber.toString().padStart(2, '0')}`;
});

watch(internalValue, (newValue) => {
  emit('update:modelValue', newValue);
});

const calendarDays = computed(() => {
  const year = currentYear.value;
  const month = currentMonth.value;
  const firstDay = new Date(year, month, 1);
  const lastDay = new Date(year, month + 1, 0);
  const days = [];

  // Add days from previous month
  for (let i = 1; i < firstDay.getDay() || i === 7; i++) {
    const date = new Date(year, month, 1 - i);
    days.unshift({
      date,
      isCurrentMonth: false,
      isSelected: isSelectedWeek(date),
      weekNumber: getWeekNumber(date)
    });
  }

  // Add days from current month
  for (let i = 1; i <= lastDay.getDate(); i++) {
    const date = new Date(year, month, i);
    days.push({
      date,
      isCurrentMonth: true,
      isSelected: isSelectedWeek(date),
      weekNumber: getWeekNumber(date)
    });
  }

  // Add days from next month
  const daysToAdd = 7 - (days.length % 7);
  for (let i = 1; i <= daysToAdd; i++) {
    const date = new Date(year, month + 1, i);
    days.push({
      date,
      isCurrentMonth: false,
      isSelected: isSelectedWeek(date),
      weekNumber: getWeekNumber(date)
    });
  }

  return days;
});

function changeMonth(delta) {
  currentDate.value = new Date(currentYear.value, currentMonth.value + delta, 1);
}

function selectWeek(date, weekNumber) {
  selectedDate.value = date;
  showCalendar.value = false;

  const startOfWeek = new Date(date);
  startOfWeek.setDate(date.getDate() - date.getDay() + 1);
  const endOfWeek = new Date(startOfWeek);
  endOfWeek.setDate(startOfWeek.getDate() + 6);

  emit('update:years', { startYear: startOfWeek.getFullYear(), endYear: endOfWeek.getFullYear() });
}

function getWeekNumber(date) {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
  const dayNum = d.getUTCDay() || 7;
  d.setUTCDate(d.getUTCDate() + 4 - dayNum);
  const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
  return Math.ceil((((d - yearStart) / 86400000) + 1) / 7);
}

function isSelectedWeek(date) {
  if (!selectedDate.value) return false;
  const selectedWeek = getWeekNumber(selectedDate.value);
  const selectedYear = selectedDate.value.getFullYear();
  return getWeekNumber(date) === selectedWeek;
}

function handleClickOutside(event) {
  if (!event.target.closest('.week-picker')) {
    showCalendar.value = false;
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside);
  if (props.modelValue) {
    const [year, week] = props.modelValue.split('-W');
    const date = new Date(year, 0, 1 + (week - 1) * 7);
    selectedDate.value = date;
    currentDate.value = new Date(date);
  }
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
});
</script>

<style scoped>
.week-picker {
  position: relative;
  display: inline-block;
}

.week-input {
  padding: 5px;
  border: 1px solid #ccc;
  border-radius: 4px;
}

.calendar {
  position: absolute;
  top: 100%;
  max-width: 100%;
  font-size: 0.8rem;
  left: 0;
  z-index: 1000;
  background: white;
  border: 1px solid #ccc;
  border-radius: 4px;
  padding: 10px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
}

.calendar-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 10px;
}

.btn {
  background: none;
  border: none;
  cursor: pointer;
  font-size: 18px;
}

.weekdays {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 5px;
  text-align: center;
  font-weight: bold;
  margin-bottom: 5px;
}

.days {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 5px;
}

.day {
  text-align: center;
  padding: 5px;
  cursor: pointer;
  position: relative;
}

.day:hover {
  background-color: #f0f0f0;
}

.current-month {
  font-weight: bold;
}

.selected {
  background-color: #e0e0e0;
}

.week-number {
  position: absolute;
  top: -8px;
  left: -9px;
  font-size: 0.3rem;
  background-color: #007bff;
  color: white;
  padding: 2px 2px;
  border-radius: 50%;
  width: 22px;
}

.input-with-icon {
  position: relative;
  display: inline-block;
}

.input-with-icon .material-symbols-outlined {
  position: absolute;
  right: 10px;
  top: 50%;
  transform: translateY(-50%);
  pointer-events: none;
}
</style>