<script setup lang="ts">
import { computed } from 'vue';
import { format, isSameDay } from 'date-fns';
import { fr } from 'date-fns/locale';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import GraphiqueBarres from '@/features/bilan-quotidien/components/historique/GraphiqueBarres.vue';
import { indicateursGraphiques } from '@/features/bilan-quotidien/config/historique';
import { teinteDe } from '@/features/bilan-quotidien/config/teintes';
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

const graphiques = computed(() => indicateursGraphiques.map((indicateur) => ({
  ...indicateur,
  couleur: teinteDe(indicateur.cle).variable,
  valeurs: props.jours.map((jour) => (jour.bilan ? indicateur.valeur(jour.bilan) : null)),
})));

const aucunBilan = computed(() => props.jours.every((jour) => !jour.bilan));
</script>

<template>
  <Card class="flex w-full flex-col rounded-carte border-0 bg-surface shadow-elevation">
    <CardHeader class="p-4 md:p-6">
      <CardTitle class="m-0 flex items-center gap-2 text-titre-carte font-semibold leading-tight tracking-normal text-texte">
        <i class="material-symbols-outlined rounded-controle bg-teinte-bilan-fond p-1.5 text-titre-2 text-teinte-bilan" aria-hidden="true">bar_chart</i>
        Évolution {{ mode === 'mois' ? 'du mois' : 'de la semaine' }}
      </CardTitle>
    </CardHeader>
    <CardContent class="flex flex-col gap-4 px-4 pb-4 md:px-6 md:pb-6">
      <p v-if="aucunBilan" class="m-0 text-left text-texte-2">Aucun bilan sur cette période.</p>
      <template v-else>
        <p class="m-0 text-left text-xs text-texte-3">Plus la barre est haute, plus la journée a été lourde. Touche un jour pour voir son bilan.</p>
        <GraphiqueBarres
            v-for="graphique in graphiques"
            :key="graphique.cle"
            :titre="graphique.titre"
            :max="graphique.max"
            :unite="graphique.unite"
            :couleur="graphique.couleur"
            :valeurs="graphique.valeurs"
            :bandes="bandes"
            :etiquettes="etiquettes"
            :selection="selection"
            @selectionner="(index) => emit('selectionner', jours[index].date)"
        />
        <p v-if="aDesRegles" class="m-0 flex items-center gap-2 text-left text-xs text-texte-2">
          <span class="h-1 w-4 rounded-full bg-teinte-regles" aria-hidden="true"/>
          Jours de règles
        </p>
        <p class="m-0 text-left text-xs text-texte-3">
          Un jour sans valeur n'a pas de barre : une mesure non renseignée ne compte jamais pour zéro.
        </p>
      </template>
    </CardContent>
  </Card>
</template>
