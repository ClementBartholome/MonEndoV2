<template>
  <!--
    La frise : un trait par jour, plus foncé quand la douleur est plus forte (échelle d'intensité), en trait très clair quand
    rien n'est noté, et un ruban sous les jours de règles. Purement descriptive. Le dessin est caché aux lecteurs d'écran,
    qui lisent la liste des jours juste en dessous.
  -->
  <div class="flex flex-col gap-1.5">
    <div aria-hidden="true" class="grid items-end" :style="{ gridTemplateColumns: `repeat(${jours.length}, minmax(0, 1fr))` }">
      <span v-for="jour in jours" :key="jour.jour" class="flex flex-col items-center gap-1">
        <span class="rounded-full" :class="[largeurTrait, hauteurTrait, classeTrait(jour)]"></span>
        <span v-if="numeros" class="text-legende" :class="jour.jour === aujourdhui ? 'font-bold text-texte' : 'text-texte-2'">{{ numero(jour.jour) }}</span>
      </span>
    </div>
    <div aria-hidden="true" class="grid h-1.5" :style="{ gridTemplateColumns: `repeat(${jours.length}, minmax(0, 1fr))` }">
      <span v-for="serie in seriesDeRegles" :key="serie.debut" class="rounded-full bg-teinte-regles" :class="jours.length > 45 ? '' : 'mx-1'"
            :style="{ gridColumn: `${serie.debut + 1} / ${serie.fin + 2}` }"></span>
    </div>
    <div v-if="!numeros && jours.length" aria-hidden="true" class="flex justify-between text-legende text-texte-2">
      <span>{{ dateCourte(jours[0].jour) }}</span><span>{{ dateCourte(jours[jours.length - 1].jour) }}</span>
    </div>
    <ul class="sr-only">
      <li v-for="jour in jours" :key="jour.jour">{{ lecture(jour) }}</li>
    </ul>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { format, parseISO } from 'date-fns';
import { fr } from 'date-fns/locale';
import type { JourFrise } from '@/shared/types/frise';

/**
 * `numeros` : numéro du jour sous chaque trait (frise courte, 7 jours) ; sinon la première et la dernière date (30 jours).
 * `aujourdhui` (AAAA-MM-JJ) : jour mis en gras.
 */
const props = defineProps<{ jours: JourFrise[]; numeros?: boolean; aujourdhui?: string }>();

/** Le trait s'affine avec le nombre de jours : jusqu'à remplir sa colonne au-delà de trois mois (synthèse d'un an). */
const largeurTrait = computed(() => {
  const n = props.jours.length;
  if (n > 90) return 'w-full';
  if (n > 45) return 'w-[3px]';
  return n > 14 ? 'w-1.5' : 'w-2.5';
});
const hauteurTrait = computed(() => (props.jours.length > 14 ? 'h-9' : 'h-11'));

/** Classes complètes (lisibles par Tailwind) de l'échelle d'intensité 0 à 10. */
const INTENSITE = ['bg-intensite-0', 'bg-intensite-1', 'bg-intensite-2', 'bg-intensite-3', 'bg-intensite-4', 'bg-intensite-5',
  'bg-intensite-6', 'bg-intensite-7', 'bg-intensite-8', 'bg-intensite-9', 'bg-intensite-10'];

const classeTrait = (jour: JourFrise) => (jour.douleur === null
  ? 'bg-trait'
  : INTENSITE[Math.min(10, Math.max(0, jour.douleur))]);

/** Ruban des règles : un seul trait par série de jours qui se suivent (indices de début et de fin dans la frise). */
const seriesDeRegles = computed(() => {
  const series: { debut: number; fin: number }[] = [];
  props.jours.forEach((jour, index) => {
    if (!jour.regles) return;
    const derniere = series[series.length - 1];
    if (derniere && derniere.fin === index - 1) derniere.fin = index;
    else series.push({ debut: index, fin: index });
  });
  return series;
});

const numero = (jour: string) => format(parseISO(jour), 'd');
const dateCourte = (jour: string) => format(parseISO(jour), 'd MMM', { locale: fr });

function lecture(jour: JourFrise) {
  const date = format(parseISO(jour.jour), 'EEEE d MMMM', { locale: fr });
  const douleur = jour.douleur === null ? 'rien de noté' : `douleur ${jour.douleur} sur 10`;
  return `${date} : ${douleur}${jour.regles ? ', règles' : ''}`;
}
</script>
