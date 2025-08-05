<template>
  <div class="w-full">
    <!-- Navigation semaine -->
    <div class="flex items-center justify-between mb-4">
      <h3 class="text-lg">{{ formatCurrentWeek }}</h3>
      <div class="flex items-center space-x-2">
        <Button @click="previousWeek" variant="outline" size="sm">
          <i class="material-symbols-outlined text-lg">chevron_left</i>
        </Button>
        <Button @click="goToToday" variant="outline" size="sm">
          Aujourd'hui
        </Button>
        <Button @click="nextWeek" variant="outline" size="sm">
          <i class="material-symbols-outlined text-lg">chevron_right</i>
        </Button>
      </div>
    </div>

    <!-- Calendrier semaine -->
    <div class="grid grid-cols-7 gap-1 mb-6">
      <div v-for="day in weekDays" :key="day.dateString" class="flex flex-col items-center">
        <!-- Nom du jour -->
        <div class="text-xs text-gray-500 mb-1 font-medium">
          {{ day.dayName }}
        </div>

        <!-- Date cliquable -->
        <button
            @click="selectDate(day.date)"
            class="relative w-12 h-12 rounded-xl flex flex-col items-center justify-center transition-all duration-200 group"
            :class="getDateButtonClasses(day)">

          <!-- Numéro du jour -->
          <span class="text-sm font-medium" :class="getDateTextClasses(day)">
            {{ day.day }}
          </span>

          <!-- Indicateur de bilan -->
          <div v-if="day.bilan"
               class="absolute bottom-1 w-1.5 h-1.5 rounded-full"
               :class="getScoreIndicatorColor(day.score)">
          </div>

          <!-- Indicateur sélection -->
          <div v-if="isSelected(day)"
               class="absolute inset-0 border-2 border-blue-500 rounded-xl">
          </div>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { format, startOfWeek, addDays, addWeeks, subWeeks, isSameDay, isToday } from 'date-fns';
import { fr } from 'date-fns/locale';
import { Button } from "@/components/ui/button";
import type {BilanQuotidien} from "@/interfaces/bilan-quotidien";

// Interface pour un jour
interface WeekDay {
  date: Date;
  dateString: string;
  day: number;
  dayName: string;
  bilan?: BilanQuotidien;
  score: number;
}

// Props et Emits
const props = defineProps<{
  bilans: BilanQuotidien[];
  selectedDate?: Date;
}>();

const emit = defineEmits<{
  'update:selectedDate': [date: Date];
}>();

// État local
const currentWeekStart = ref(startOfWeek(props.selectedDate || new Date(), { weekStartsOn: 1 }));

// Calcul du score d'un bilan
const calculateBilanScore = (bilan: BilanQuotidien): number => {
  const getMoodScore = (mood: string): number => {
    const mapping: Record<string, number> = {'Heureuse': 20, 'Neutre': 10, 'Triste': 0};
    return mapping[mood] || 10;
  };

  const moodScore = getMoodScore(bilan.mood);
  const stressScore = (5 - Math.min(Math.max((bilan.stressPro + bilan.stressPerso) / 2, 0), 5)) / 5 * 20;
  const fatigueScore = (5 - Math.min(Math.max(bilan.fatigue, 0), 5)) / 5 * 20;
  const painScore = (10 - Math.min(Math.max(bilan.douleurMoyenne, 0), 10)) / 10 * 20;
  const activityScore = Math.min(bilan.pas / 10000, 1) * 20;

  return Math.min(Math.max(Math.round(moodScore + stressScore + fatigueScore + painScore + activityScore), 0), 100);
};

// Jours de la semaine
const weekDays = computed((): WeekDay[] => {
  const days: WeekDay[] = [];

  for (let i = 0; i < 7; i++) {
    const date = addDays(currentWeekStart.value, i);
    const dateString = format(date, 'yyyy-MM-dd');
    const bilan = props.bilans.find(b =>
        format(new Date(b.date), 'yyyy-MM-dd') === dateString
    );

    days.push({
      date,
      dateString,
      day: date.getDate(),
      dayName: format(date, 'EEE', { locale: fr }),
      bilan,
      score: bilan ? calculateBilanScore(bilan) : 0
    });
  }

  return days;
});

// Semaine actuelle formatée
const formatCurrentWeek = computed(() => {
  const start = currentWeekStart.value;
  const end = addDays(start, 6);

  if (start.getMonth() === end.getMonth()) {
    return `${start.getDate()}-${end.getDate()} ${format(start, 'MMMM yyyy', { locale: fr })}`;
  } else {
    return `${format(start, 'd MMM', { locale: fr })} - ${format(end, 'd MMM yyyy', { locale: fr })}`;
  }
});

// Navigation
const previousWeek = () => {
  currentWeekStart.value = subWeeks(currentWeekStart.value, 1);
};

const nextWeek = () => {
  currentWeekStart.value = addWeeks(currentWeekStart.value, 1);
};

const goToToday = () => {
  const today = new Date();
  currentWeekStart.value = startOfWeek(today, { weekStartsOn: 1 });
  emit('update:selectedDate', today);
};

const selectDate = (date: Date) => {
  emit('update:selectedDate', date);
};

// Helpers pour les classes CSS
const isSelected = (day: WeekDay): boolean => {
  return props.selectedDate ? isSameDay(day.date, props.selectedDate) : false;
};

const getDateButtonClasses = (day: WeekDay): string => {
  let classes = 'hover:bg-gray-100';

  if (isToday(day.date)) {
    classes += ' bg-blue-50 border border-blue-200';
  } else if (day.bilan) {
    classes += ' bg-white border border-gray-200 hover:shadow-md hover:scale-105';
  } else {
    classes += ' bg-gray-50 border border-gray-100';
  }

  if (isSelected(day)) {
    classes += ' bg-blue-100';
  }

  return classes;
};

const getDateTextClasses = (day: WeekDay): string => {
  if (isToday(day.date)) {
    return 'text-blue-600';
  } else if (day.bilan) {
    return 'text-gray-900';
  } else {
    return 'text-gray-400';
  }
};

const getScoreIndicatorColor = (score: number): string => {
  if (score >= 70) return 'bg-green-500';
  if (score >= 40) return 'bg-yellow-500';
  return 'bg-red-500';
};

// Watcher pour mettre à jour la semaine si selectedDate change externellement
watch(() => props.selectedDate, (newDate) => {
  if (newDate) {
    currentWeekStart.value = startOfWeek(newDate, { weekStartsOn: 1 });
  }
});
</script>

<style scoped>
.material-symbols-outlined {
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}
</style>