<template>
  <div class="w-full space-y-6">
    <!-- Indicateur de période analysée -->
    <aside class="bg-clearer border border-gray-300 rounded-2xl p-3 flex items-center justify-center shadow-sm">
      <i class="material-symbols-outlined mr-2 text-button">info</i>
      <span class="text-sm text-paragraph">
        Analyse basée sur <strong class="text-headline">{{ dataInfo.count }} {{
          dataInfo.count > 1 ? 'bilans' : 'bilan'
        }}</strong> 
        ({{ dataInfo.period }})
      </span>
    </aside>

    <!-- Score Global de Bien-être -->
    <Card class="container mt-4 mx-auto w-full bg-clearer rounded-3xl shadow-xl ml-auto flex flex-col">
      <CardHeader>
        <CardTitle class="flex items-center">
          <i class="material-symbols-outlined mr-3">health_and_safety</i>
          Score de Bien-être
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div class="flex flex-col md:flex-row md:items-center md:justify-between space-y-6 md:space-y-0">
          <div class="flex-1 text-center md:text-left">
            <div class="text-4xl md:text-6xl font-bold mb-2" :class="getScoreColor(globalScore)">
              {{ globalScore }}/100
            </div>
            <div class="text-base md:text-lg text-gray-600 mb-4">{{ getScoreLabel(globalScore) }}</div>
            <div class="flex flex-wrap items-center justify-center md:justify-start gap-2 md:space-x-4 md:gap-0">
              <div class="flex items-center text-xs md:text-sm" v-for="metric in scoreBreakdown" :key="metric.name">
                <i class="material-symbols-outlined mr-1 text-sm">{{ metric.icon }}</i>
                <span>{{ metric.name }}: {{ metric.value }}/20</span>
              </div>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>

    <!-- Objectifs et Insights & Actions -->
    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
      <!-- Objectifs et Progrès -->
      <Card class="container mt-4 mx-auto w-full bg-clearer rounded-3xl shadow-xl ml-auto flex flex-col">
        <CardHeader>
          <div class="flex items-center">
            <i class="material-symbols-outlined mr-2">flag</i>
            Objectifs et Progrès
          </div>
        </CardHeader>
        <CardContent>
          <div class="mb-3 text-xs text-muted-foreground">
            Objectifs personnalisables depuis <router-link to="/parametres" class="underline">Paramètres</router-link>
          </div>
          <div class="space-y-4">
            <div v-for="goal in goals" :key="goal.id"
                 class="p-4 rounded-2xl border-l-4 bg-white shadow-sm"
                 :class="goal.progress >= 100 ? 'border-l-green-400' : goal.progress >= 70 ? 'border-l-yellow-400' : 'border-l-red-400'">
              <div class="flex items-start space-x-3">
                <div class="p-2 rounded-2xl"
                     :class="goal.progress >= 100 ? 'bg-green-100' : goal.progress >= 70 ? 'bg-yellow-100' : 'bg-red-100'">
                  <i class="material-symbols-outlined"
                     :class="goal.progress >= 100 ? 'text-green-600' : goal.progress >= 70 ? 'text-yellow-600' : 'text-red-600'">
                    {{ goal.icon }}
                  </i>
                </div>
                <div class="flex-1">
                  <div class="flex items-center justify-between mb-2">
                    <span class="font-medium">{{ goal.title }}</span>
                    <span class="text-sm font-semibold"
                          :class="goal.progress >= 100 ? 'text-green-600' : goal.progress >= 70 ? 'text-yellow-600' : 'text-red-600'">
                      {{ goal.progress }}%
                    </span>
                  </div>
                  <div class="w-full bg-gray-200 rounded-full h-2 mb-3">
                    <div
                        class="h-2 rounded-full transition-all duration-500"
                        :class="goal.progress >= 100 ? 'bg-green-500' : goal.progress >= 70 ? 'bg-yellow-500' : 'bg-red-500'"
                        :style="`width: ${goal.progress}%`"
                    ></div>
                  </div>
                  <p class="text-sm text-gray-600 mb-1">{{ goal.description }}</p>
                  <p v-if="goal.detail" class="text-xs text-gray-500">{{ goal.detail }}</p>
                </div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      <!-- Insights & Actions -->
      <Card class="container mt-4 mx-auto w-full bg-clearer rounded-3xl shadow-xl ml-auto flex flex-col">
        <CardHeader>
          <CardTitle class="flex items-center">
            <i class="material-symbols-outlined mr-2 text-2xl">psychology</i>
            Insights & Actions
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div class="space-y-4">
            <div v-for="insight in insightsWithActions" :key="insight.id"
                 class="p-4 rounded-2xl border-l-4 bg-white shadow-sm"
                 :class="`border-l-${insight.severity === 'success' ? 'green' : insight.severity === 'warning' ? 'yellow' : insight.severity === 'danger' ? 'red' : 'blue'}-400`">
              <div class="flex items-start space-x-3">
                <div
                    :class="`p-2 rounded-2xl bg-${insight.severity === 'success' ? 'green' : insight.severity === 'warning' ? 'yellow' : insight.severity === 'danger' ? 'red' : 'blue'}-100`">
                  <i class="material-symbols-outlined"
                     :class="`text-${insight.severity === 'success' ? 'green' : insight.severity === 'warning' ? 'yellow' : insight.severity === 'danger' ? 'red' : 'blue'}-600`">
                    {{ insight.icon }}
                  </i>
                </div>
                <div class="flex-1">
                  <!-- Diagnostic -->
                  <h4 class="font-medium text-gray-800 mb-1">{{ insight.title }}</h4>
                  <p class="text-sm text-gray-600 mb-2">{{ insight.diagnosis }}</p>

                  <!-- Action recommandée -->
                  <div v-if="insight.action" class="p-2 rounded-lg bg-gray-50 border-l-2 border-blue-300 mb-2">
                    <p class="text-sm text-blue-800 font-medium flex items-center">
                      <i class="material-symbols-outlined mr-1 text-sm">lightbulb</i>
                      {{ insight.action.title }}
                    </p>
                    <p class="text-xs text-blue-600 mt-1">{{ insight.action.description }}</p>
                  </div>

                  <!-- Objectif -->
                  <div class="flex items-center justify-end">
                    <span v-if="insight.target" class="text-xs text-gray-400">
                      Objectif: {{ insight.target }}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  </div>
</template>

<script setup lang="ts">
import {computed} from 'vue';
import {Card, CardContent, CardHeader, CardTitle} from "@/shared/components/ui/card";
import type {BilanQuotidien} from "@/features/bilan-quotidien/types/bilan-quotidien";
import type {ScoreMetric} from "@/features/bilan-quotidien/models/score-metric";
import type {Goal} from "@/features/bilan-quotidien/models/goal";
import type {InsightWithAction} from "@/features/bilan-quotidien/models/insight-with-action";
import { getWellbeingGoals } from '@/shared/services/wellbeingGoalsStorage';

const props = defineProps<{
  bilans: BilanQuotidien[];
}>();

const wellbeingGoals = getWellbeingGoals();

const getMoodScore = (mood: string): number => {
  const mapping: Record<string, number> = {'Heureuse': 20, 'Neutre': 10, 'Triste': 0};
  return mapping[mood] || 10;
};

const getScoreColor = (score: number): string => {
  if (score >= 70) return 'text-green-500';
  if (score >= 40) return 'text-yellow-500';
  return 'text-red-500';
};

const getScoreColorHex = (score: number): string => {
  if (score >= 70) return '#10b981';
  if (score >= 40) return '#f59e0b';
  return '#ef4444';
};

const getScoreLabel = (score: number): string => {
  if (score >= 80) return 'Excellent bien-être';
  if (score >= 60) return 'Bon équilibre';
  if (score >= 40) return 'Équilibre moyen';
  return 'Attention nécessaire';
};

// Fonctions utilitaires pour formater les dates
const formatSingleDate = (date: Date): string => {
  return `${date.getDate()}/${String(date.getMonth() + 1).padStart(2, '0')}`;
};

const formatDateRange = (firstDate: Date, lastDate: Date): string => {
  return `du ${formatSingleDate(firstDate)} au ${formatSingleDate(lastDate)}`;
};

const formatDatesList = (dates: Date[]): string => {
  // Si plus de 5 dates, utiliser une période
  if (dates.length > 5) {
    return formatDateRange(dates[0], dates[dates.length - 1]);
  }

  const formattedDates = dates.map(formatSingleDate);

  // Vérifier si toutes les dates sont du même mois
  const sameMonth = dates.every(d =>
      d.getMonth() === dates[0].getMonth() && d.getFullYear() === dates[0].getFullYear()
  );

  if (sameMonth) {
    // Même mois : "16, 18, 19 et 20/07"
    const days = dates.map(d => d.getDate());
    const month = `/${String(dates[0].getMonth() + 1).padStart(2, '0')}`;

    if (days.length === 1) return `${days[0]}${month}`;
    if (days.length === 2) return `${days[0]} et ${days[1]}${month}`;

    const lastDay = days.pop();
    return `${days.join(', ')} et ${lastDay}${month}`;
  } else {
    // Mois différents : "30/06, 02 et 05/07"
    if (formattedDates.length === 1) return formattedDates[0];
    if (formattedDates.length === 2) return `${formattedDates[0]} et ${formattedDates[1]}`;

    const lastDate = formattedDates.pop();
    return `${formattedDates.join(', ')} et ${lastDate}`;
  }
};

// Informations sur l'échantillon de données (refactorisée)
const dataInfo = computed(() => {
  const recent = props.bilans?.slice(-7) || [];
  const count = recent.length;

  if (count === 0) {
    return {count: 0, period: 'Aucune donnée'};
  }

  const dates = recent.map(b => new Date(b.date)).sort((a, b) => a.getTime() - b.getTime());

  if (count === 1) {
    const isToday = new Date().toDateString() === dates[0].toDateString();
    return {
      count: 1,
      period: isToday ? 'Aujourd\'hui uniquement' : formatSingleDate(dates[0])
    };
  }

  return {
    count,
    period: formatDatesList(dates)
  };
});

const scoreBreakdown = computed((): ScoreMetric[] => {
  if (!props.bilans || props.bilans.length === 0) return [];

  const recent = props.bilans.slice(-7);
  if (recent.length === 0) return [];

  const avgMoodScore = recent.reduce((sum, b) => sum + getMoodScore(b.mood), 0) / recent.length;
  const avgStress = recent.reduce((sum, b) => sum + ((b.stressPro || 0) + (b.stressPerso || 0)) / 2, 0) / recent.length;
  const avgFatigue = recent.reduce((sum, b) => sum + (b.fatigue || 0), 0) / recent.length;
  const avgPain = recent.reduce((sum, b) => sum + (b.douleurMoyenne || 0), 0) / recent.length;
  const avgSteps = recent.reduce((sum, b) => sum + (b.pas || 0), 0) / recent.length;

  return [
    {
      name: 'Humeur',
      value: Math.round(avgMoodScore),
      icon: 'mood'
    },
    {
      name: 'Stress',
      value: Math.round((5 - Math.min(Math.max(avgStress, 0), 5)) / 5 * 20),
      icon: 'psychology'
    },
    {
      name: 'Fatigue',
      value: Math.round((5 - Math.min(Math.max(avgFatigue, 0), 5)) / 5 * 20),
      icon: 'bedtime'
    },
    {
      name: 'Douleur',
      value: Math.round((10 - Math.min(Math.max(avgPain, 0), 10)) / 10 * 20),
      icon: 'sick'
    },
    {
      name: 'Activité',
      value: Math.round(Math.min(avgSteps / wellbeingGoals.stepsGoal, 1) * 20),
      icon: 'directions_walk'
    }
  ];
});

// Score global calculé à partir du breakdown pour cohérence
const globalScore = computed((): number => {
  if (!scoreBreakdown.value || scoreBreakdown.value.length === 0) return 0;

  const total = scoreBreakdown.value.reduce((sum, metric) => sum + metric.value, 0);
  return Math.min(Math.max(total, 0), 100);
});

// Objectifs avec valeurs réelles
const goals = computed((): Goal[] => {
  if (!props.bilans || props.bilans.length === 0) return [];

  const recent = props.bilans.slice(-7);

  const avgHydration = recent.reduce((sum, b) => sum + (b.hydratation || 0), 0) / recent.length;
  const avgSteps = recent.reduce((sum, b) => sum + (b.pas || 0), 0) / recent.length;
  const avgStress = recent.reduce((sum, b) => sum + ((b.stressPro || 0) + (b.stressPerso || 0)) / 2, 0) / recent.length;

  return [
    {
      id: 1,
      title: 'Hydratation quotidienne',
      description: `Objectif: ${wellbeingGoals.hydrationLitersGoal}L/jour`,
      progress: Math.min(Math.round((avgHydration / wellbeingGoals.hydrationLitersGoal) * 100), 100),
      icon: 'water_drop',
      detail: `${avgHydration >= wellbeingGoals.hydrationLitersGoal ? '✅' : '⚠️'} ${avgHydration.toFixed(1)}L sur les ${recent.length} derniers jours`,
      targetValue: wellbeingGoals.hydrationLitersGoal,
      targetLabel: 'L/jour'
    },
    {
      id: 2,
      title: 'Activité physique',
      description: `Objectif: ${wellbeingGoals.stepsGoal.toLocaleString()} pas/jour`,
      progress: Math.min(Math.round((avgSteps / wellbeingGoals.stepsGoal) * 100), 100),
      icon: 'directions_walk',
      detail: `${avgSteps >= wellbeingGoals.stepsGoal ? '✅' : '⚠️'} ${Math.round(avgSteps).toLocaleString()} pas en moyenne`,
      targetValue: wellbeingGoals.stepsGoal,
      targetLabel: 'pas/jour'
    },
    {
      id: 3,
      title: 'Gestion du stress',
      description: `Objectif: <= ${wellbeingGoals.stressMaxGoal}/5`,
      progress: Math.max(Math.round((1 - Math.max(avgStress - wellbeingGoals.stressMaxGoal, 0) / 2) * 100), 0),
      icon: 'psychology',
      detail: `${avgStress <= wellbeingGoals.stressMaxGoal ? '✅' : '⚠️'} Niveau moyen de ${avgStress.toFixed(1)}/5`,
      targetValue: wellbeingGoals.stressMaxGoal,
      targetLabel: '/5 max'
    }
  ];
});

const insightsWithActions = computed((): InsightWithAction[] => {
  const insights: InsightWithAction[] = [];
  const recent = props.bilans?.slice(-7) || [];

  if (recent.length === 0) {
    return [{
      id: 1,
      title: 'Commencez votre suivi',
      diagnosis: 'Continuez à enregistrer vos bilans pour obtenir des insights personnalisés.',
      icon: 'insights',
      severity: 'info',
      priority: 'medium'
    }];
  }

  // Analyse Hydratation
  const avgHydration = recent.reduce((sum, b) => sum + (b.hydratation || 0), 0) / recent.length;
  if (avgHydration < wellbeingGoals.hydrationLitersGoal) {
    insights.push({
      id: 1,
      title: 'Hydratation insuffisante',
      diagnosis: `Moyenne de ${avgHydration.toFixed(1)}L/jour, en dessous de l'objectif.`,
      icon: 'water_drop',
      severity: 'warning',
      priority: 'medium',
      action: {
        title: 'Augmenter l\'hydratation',
        description: 'Garde une bouteille d\'eau à portée de main et bois régulièrement.'
      },
      target: `${wellbeingGoals.hydrationLitersGoal}L/jour`
    });
  } else if (avgHydration >= wellbeingGoals.hydrationLitersGoal) {
    insights.push({
      id: 1,
      title: 'Excellente hydratation',
      diagnosis: `Tu maintiens une bonne hydratation avec ${avgHydration.toFixed(1)}L/jour.`,
      icon: 'water_drop',
      severity: 'success',
      priority: 'low',
      target: `${wellbeingGoals.hydrationLitersGoal}L/jour`
    });
  }

  // Analyse Fatigue
  const avgFatigue = recent.reduce((sum, b) => sum + (b.fatigue || 0), 0) / recent.length;
  if (avgFatigue > wellbeingGoals.fatigueMaxGoal) {
    insights.push({
      id: 2,
      title: 'Fatigue élevée',
      diagnosis: `Niveau de fatigue moyen de ${avgFatigue.toFixed(1)}/5 ces derniers jours.`,
      icon: 'hotel',
      severity: 'danger',
      priority: 'high',
      action: {
        title: 'Améliorer le sommeil',
        description: 'Essaie de te coucher 30 minutes plus tôt et maintiens des horaires réguliers.'
      },
      target: `<= ${wellbeingGoals.fatigueMaxGoal}/5`
    });
  }

  // Analyse Activité
  const avgSteps = recent.reduce((sum, b) => sum + (b.pas || 0), 0) / recent.length;
  if (avgSteps < wellbeingGoals.stepsGoal * 0.6) {
    insights.push({
      id: 3,
      title: 'Activité physique faible',
      diagnosis: `Moyenne de ${Math.round(avgSteps)} pas/jour, bien en dessous de l'objectif.`,
      icon: 'directions_walk',
      severity: 'warning',
      priority: 'medium',
      action: {
        title: 'Bouger plus',
        description: 'Intègre de courtes marches dans ta journée, même 10 minutes suffisent.'
      },
      target: `${wellbeingGoals.stepsGoal.toLocaleString()} pas/jour`
    });
  } else if (avgSteps >= wellbeingGoals.stepsGoal * 0.6) {
    insights.push({
      id: 3,
      title: 'Bonne activité physique !',
      diagnosis: `Excellent ! Tu fais en moyenne ${Math.round(avgSteps)} pas/jour.`,
      icon: 'directions_walk',
      severity: 'success',
      priority: 'medium',
      target: `${wellbeingGoals.stepsGoal.toLocaleString()} pas/jour`
    });
  }

  // Analyse Stress
  const avgStress = recent.reduce((sum, b) => sum + ((b.stressPro || 0) + (b.stressPerso || 0)) / 2, 0) / recent.length;
  if (avgStress > wellbeingGoals.stressMaxGoal + 0.5) {
    insights.push({
      id: 4,
      title: 'Niveau de stress élevé',
      diagnosis: `Stress moyen de ${avgStress.toFixed(1)}/5, nécessite une attention.`,
      icon: 'spa',
      severity: 'danger',
      priority: 'high',
      action: {
        title: 'Techniques de relaxation',
        description: 'Essaie la méditation, la respiration profonde ou le yoga.'
      },
      target: `<= ${wellbeingGoals.stressMaxGoal}/5`
    });
  } else if (avgStress < Math.max(wellbeingGoals.stressMaxGoal - 1, 0)) {
    insights.push({
      id: 4,
      title: 'Stress bien géré',
      diagnosis: 'Tu maintiens un excellent niveau de stress. Continue !',
      icon: 'self_care',
      severity: 'success',
      priority: 'low',
      target: `<= ${wellbeingGoals.stressMaxGoal}/5`
    });
  }

  // Analyse Douleur
  const avgPain = recent.reduce((sum, b) => sum + (b.douleurMoyenne || 0), 0) / recent.length;
  if (avgPain > wellbeingGoals.painMaxGoal + 1) {
    insights.push({
      id: 5,
      title: 'Douleurs persistantes',
      diagnosis: `Niveau de douleur moyen de ${avgPain.toFixed(1)}/10, préoccupant.`,
      icon: 'healing',
      severity: 'danger',
      priority: 'high',
      action: {
        title: 'Consulter un professionnel',
        description: 'Prends rendez-vous avec un professionnel de santé si les douleurs persistent.'
      },
      target: `<= ${wellbeingGoals.painMaxGoal}/10`
    });
  }

  // Message d'encouragement si tout va bien
  if (insights.length === 0 || insights.every(i => i.severity === 'success')) {
    insights.push({
      id: 6,
      title: 'Bon équilibre général',
      diagnosis: 'Tes métriques sont dans de bonnes plages. Tu gères bien ton bien-être !',
      icon: 'emoji_events',
      severity: 'success',
      priority: 'low'
    });
  }

  // Trier par priorité (high -> medium -> low)
  return insights.sort((a, b) => {
    const priorityOrder = {high: 0, medium: 1, low: 2};
    return priorityOrder[a.priority] - priorityOrder[b.priority];
  }).slice(0, 5); // Limiter à 5 insights max
});
</script>

<style scoped>
.material-symbols-outlined {
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}
</style>