<script setup lang="ts">
import { computed, ref } from 'vue';
import { useElementSize } from '@vueuse/core';

export interface SerieLignes {
  cle: string;
  libelle: string;
  trait: string;
  point: string;
  pastille: string;
  /** Une valeur par jour ; null = non renseigné (ni point ni trait, jamais 0). */
  valeurs: (number | null)[];
}

// Courbes d'une période sur une seule échelle (0 à max), jours de règles en bandes de fond.
const props = defineProps<{
  titre: string;
  max: number;
  graduations: number[];
  series: SerieLignes[];
  /** Un booléen par jour : jour de règles. */
  bandes: boolean[];
  /** Une étiquette par jour ('' = pas d'étiquette). */
  etiquettes: string[];
  /** Index du jour sélectionné, -1 si hors période. */
  selection: number;
}>();

const emit = defineEmits<{ selectionner: [index: number] }>();

const HAUTEUR = 150;
const MARGE = { gauche: 26, droite: 8, haut: 10, bas: 22 };

const conteneur = ref<HTMLElement | null>(null);
const { width: largeur } = useElementSize(conteneur);

const nombreJours = computed(() => props.etiquettes.length);
const colonne = computed(() =>
  nombreJours.value ? Math.max(largeur.value - MARGE.gauche - MARGE.droite, 0) / nombreJours.value : 0);
const basTrace = HAUTEUR - MARGE.bas;

const x = (index: number) => MARGE.gauche + colonne.value * (index + 0.5);
const y = (valeur: number) =>
  MARGE.haut + (1 - Math.min(Math.max(valeur, 0), props.max) / props.max) * (basTrace - MARGE.haut);

interface Trace {
  serie: SerieLignes;
  segments: { x1: number; y1: number; x2: number; y2: number }[];
  points: { x: number; y: number; index: number }[];
}

// Deux jours consécutifs renseignés sont reliés ; un jour isolé reste un point visible.
const traces = computed<Trace[]>(() => props.series.map((serie) => {
  const segments: Trace['segments'] = [];
  const points: Trace['points'] = [];
  serie.valeurs.forEach((valeur, index) => {
    if (valeur === null) return;
    points.push({ x: x(index), y: y(valeur), index });
    const suivante = serie.valeurs[index + 1];
    if (suivante !== null && suivante !== undefined) {
      segments.push({ x1: x(index), y1: y(valeur), x2: x(index + 1), y2: y(suivante) });
    }
  });
  return { serie, segments, points };
}));

const rayon = computed(() => (colonne.value < 12 ? 2.5 : 3.5));

const resume = computed(() => {
  const renseignes = props.series.map((serie) => {
    const n = serie.valeurs.filter((v) => v !== null).length;
    return `${serie.libelle} : ${n} jour${n > 1 ? 's' : ''} renseigné${n > 1 ? 's' : ''}`;
  });
  return `${props.titre}. ${renseignes.join(', ')} sur ${nombreJours.value} jours.`;
});
</script>

<template>
  <figure class="w-full m-0">
    <figcaption class="text-sm font-medium text-headline text-left mb-1">{{ titre }}</figcaption>
    <div ref="conteneur" class="w-full" :style="{ height: `${HAUTEUR}px` }">
      <svg
          v-if="largeur > 0"
          :width="largeur"
          :height="HAUTEUR"
          role="img"
          :aria-label="resume"
          class="block text-muted-foreground"
      >
        <rect
            :x="MARGE.gauche" :y="MARGE.haut"
            :width="Math.max(largeur - MARGE.gauche - MARGE.droite, 0)" :height="basTrace - MARGE.haut"
            rx="6" class="fill-white"
        />
        <rect
            v-for="(regles, index) in bandes"
            v-show="regles"
            :key="`bande-${index}`"
            :x="MARGE.gauche + colonne * index"
            :y="MARGE.haut"
            :width="colonne"
            :height="basTrace - MARGE.haut"
            class="fill-rose-200/70"
        />
        <rect
            v-if="selection >= 0"
            :x="MARGE.gauche + colonne * selection"
            :y="MARGE.haut"
            :width="colonne"
            :height="basTrace - MARGE.haut"
            class="fill-gray-300/60"
        />

        <g v-for="graduation in graduations" :key="`graduation-${graduation}`">
          <line
              :x1="MARGE.gauche" :x2="largeur - MARGE.droite" :y1="y(graduation)" :y2="y(graduation)"
              class="stroke-gray-200" stroke-dasharray="3 3"
          />
          <text :x="MARGE.gauche - 6" :y="y(graduation)" dy="0.32em" text-anchor="end" font-size="10" fill="currentColor">
            {{ graduation.toLocaleString('fr-FR') }}
          </text>
        </g>

        <text
            v-for="(etiquette, index) in etiquettes"
            v-show="etiquette"
            :key="`etiquette-${index}`"
            :x="x(index)"
            :y="HAUTEUR - 6"
            text-anchor="middle"
            font-size="10"
            fill="currentColor"
        >
          {{ etiquette }}
        </text>

        <g v-for="trace in traces" :key="trace.serie.cle">
          <line
              v-for="(segment, index) in trace.segments"
              :key="`segment-${index}`"
              v-bind="segment"
              :class="trace.serie.trait"
              stroke-width="2"
              stroke-linecap="round"
          />
          <circle
              v-for="point in trace.points"
              :key="`point-${point.index}`"
              :cx="point.x" :cy="point.y" :r="rayon"
              :class="trace.serie.point"
          />
        </g>

        <!-- Zones de tap par jour (le calendrier offre l'équivalent au clavier). -->
        <rect
            v-for="(_, index) in etiquettes"
            :key="`zone-${index}`"
            :x="MARGE.gauche + colonne * index"
            :y="0"
            :width="colonne"
            :height="HAUTEUR"
            fill="transparent"
            class="cursor-pointer"
            @click="emit('selectionner', index)"
        />
      </svg>
    </div>
    <ul v-if="series.length > 1" class="flex flex-wrap gap-x-4 gap-y-1 text-xs text-paragraph mt-1" aria-hidden="true">
      <li v-for="serie in series" :key="serie.cle" class="flex items-center gap-1">
        <span class="w-3 h-3 rounded-full" :class="serie.pastille"/>{{ serie.libelle }}
      </li>
    </ul>
  </figure>
</template>
