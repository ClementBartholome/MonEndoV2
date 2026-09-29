<script setup lang="ts">
import { computed } from 'vue';
import { format, isSameDay } from 'date-fns';
import { fr } from 'date-fns/locale';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import GraphiqueLignes from '@/features/bilan-quotidien/components/historique/GraphiqueLignes.vue';
import { graphiquesCourbes } from '@/features/bilan-quotidien/config/historique';
import type { JourHistorique, ModePeriode } from '@/features/bilan-quotidien/types/historique';

const props = defineProps<{
  jours: JourHistorique[];
  mode: ModePeriode;
  jourSelectionne: Date;
}>();

const emit = defineEmits<{ selectionner: [date: Date] }>();

// En mois, une étiquette par semaine (1, 8, 15…) pour rester lisible à 375px.
const etiquettes = computed(() => props.jours.map((jour, index) => {
  if (props.mode === 'semaine') return format(jour.date, 'EEEEEE', { locale: fr });
  return index % 7 === 0 ? String(jour.date.getDate()) : '';
}));

const bandes = computed(() => props.jours.map((jour) => jour.regles));
const aDesRegles = computed(() => bandes.value.some(Boolean));

const selection = computed(() => props.jours.findIndex((jour) => isSameDay(jour.date, props.jourSelectionne)));

const graphiques = computed(() => graphiquesCourbes.map((graphique) => ({
  ...graphique,
  series: graphique.series.map((serie) => ({
    ...serie,
    valeurs: props.jours.map((jour) => (jour.bilan ? serie.valeur(jour.bilan) : null)),
  })),
})));

const aucunBilan = computed(() => props.jours.every((jour) => !jour.bilan));
</script>

<template>
  <Card class="flex w-full flex-col rounded-carte border-0 bg-surface shadow-elevation">
    <CardHeader class="p-4 md:p-6">
      <CardTitle class="m-0 flex items-center gap-2 text-[17px] font-semibold leading-tight tracking-normal text-texte">
        <i class="material-symbols-outlined" aria-hidden="true">show_chart</i>
        Évolution {{ mode === 'mois' ? 'du mois' : 'de la semaine' }}
      </CardTitle>
    </CardHeader>
    <CardContent class="px-4 pb-4 md:px-6 md:pb-6 flex flex-col gap-4">
      <p v-if="aucunBilan" class="text-texte-2 text-left">Aucun bilan sur cette période.</p>
      <template v-else>
        <GraphiqueLignes
            v-for="graphique in graphiques"
            :key="graphique.titre"
            :titre="graphique.titre"
            :max="graphique.max"
            :graduations="graphique.graduations"
            :series="graphique.series"
            :bandes="bandes"
            :etiquettes="etiquettes"
            :selection="selection"
            @selectionner="(index) => emit('selectionner', jours[index].date)"
        />
        <p v-if="aDesRegles" class="flex items-center gap-2 text-xs text-texte-2 text-left">
          <span class="w-4 h-3 rounded-sm bg-teinte-regles-fond" aria-hidden="true"/>
          Jours de règles
        </p>
        <p class="text-xs text-texte-3 text-left">
          Les jours sans valeur ne sont pas tracés : une mesure non renseignée ne compte jamais pour zéro.
        </p>
      </template>
    </CardContent>
  </Card>
</template>
