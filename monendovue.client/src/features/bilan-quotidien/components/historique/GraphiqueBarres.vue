<script setup lang="ts">
import { computed, ref } from 'vue';
import { useElementSize } from '@vueuse/core';

/**
 * Un indicateur sur la période : une barre par jour (hauteur = valeur, plus haut = plus lourd), dans la couleur de
 * l'indicateur ; jour non renseigné = pas de barre, jamais 0. Jours de règles en bande de fond. Toucher un jour le choisit.
 */
const props = defineProps<{
  titre: string;
  max: number;
  /** « /10 » : pour la moyenne. */
  unite: string;
  /** Couleur CSS des barres (token de teinte). */
  couleur: string;
  valeurs: (number | null)[];
  /** Un booléen par jour : jour de règles. */
  bandes: boolean[];
  /** Une étiquette par jour ('' = pas d'étiquette). */
  etiquettes: string[];
  /** Index du jour sélectionné, -1 si hors période. */
  selection: number;
}>();

const emit = defineEmits<{ selectionner: [index: number] }>();

const HAUTEUR = 96;
const MARGE = { gauche: 22, droite: 4, haut: 6, bas: 18 };
/** Espace entre deux barres (couleur de la surface). */
const ESPACE = 2;

const conteneur = ref<HTMLElement | null>(null);
const { width: largeur } = useElementSize(conteneur);

const nombre = computed(() => props.valeurs.length);
const colonne = computed(() => (nombre.value ? Math.max(largeur.value - MARGE.gauche - MARGE.droite, 0) / nombre.value : 0));
const basTrace = HAUTEUR - MARGE.bas;
const hauteurUtile = basTrace - MARGE.haut;

const barres = computed(() => props.valeurs.map((valeur, index) => {
  const x = MARGE.gauche + colonne.value * index;
  if (valeur === null) return { index, x, valeur, hauteur: 0 };
  // Une valeur 0 reste visible (trait de 2 px) : renseignée à zéro n'est pas « non renseignée ».
  const hauteur = Math.max((Math.min(valeur, props.max) / props.max) * hauteurUtile, 2);
  return { index, x, valeur, hauteur };
}));

const renseignees = computed(() => props.valeurs.filter((v): v is number => v !== null));
const moyenne = computed(() => {
  if (!renseignees.value.length) return null;
  const m = renseignees.value.reduce((total, v) => total + v, 0) / renseignees.value.length;
  return m.toLocaleString('fr-FR', { maximumFractionDigits: 1 });
});

const resume = computed(() => {
  const n = renseignees.value.length;
  const base = `${props.titre} : renseigné ${n} jour${n > 1 ? 's' : ''} sur ${nombre.value}`;
  return moyenne.value === null ? base : `${base}, moyenne ${moyenne.value}${props.unite}.`;
});
</script>

<template>
  <figure class="m-0 w-full">
    <figcaption class="mb-1 flex items-baseline justify-between gap-2 text-left">
      <span class="text-sm font-medium text-texte">{{ titre }}</span>
      <span class="text-xs text-texte-3">{{ moyenne === null ? 'non renseigné' : `moyenne ${moyenne}${unite}` }}</span>
    </figcaption>
    <div ref="conteneur" class="w-full" :style="{ height: `${HAUTEUR}px` }">
      <svg v-if="largeur > 0" :width="largeur" :height="HAUTEUR" role="img" :aria-label="resume" class="block text-texte-3">
        <rect v-for="(regles, index) in bandes" v-show="regles" :key="`bande-${index}`"
              :x="MARGE.gauche + colonne * index" :y="MARGE.haut" :width="colonne" :height="hauteurUtile"
              class="fill-teinte-regles-fond"/>
        <rect v-if="selection >= 0" :x="MARGE.gauche + colonne * selection" :y="MARGE.haut" :width="colonne" :height="hauteurUtile"
              rx="3" class="fill-surface-2"/>
        <line :x1="MARGE.gauche" :x2="largeur - MARGE.droite" :y1="basTrace" :y2="basTrace" class="stroke-trait"/>
        <text :x="MARGE.gauche - 5" :y="MARGE.haut" dy="0.7em" text-anchor="end" font-size="10" fill="currentColor">{{ max }}</text>
        <text :x="MARGE.gauche - 5" :y="basTrace" text-anchor="end" font-size="10" fill="currentColor">0</text>

        <rect v-for="barre in barres.filter((b) => b.valeur !== null)" :key="`barre-${barre.index}`"
              :x="barre.x + ESPACE / 2" :y="basTrace - barre.hauteur"
              :width="Math.max(colonne - ESPACE, 1)" :height="barre.hauteur" rx="2"
              :style="{ fill: couleur, opacity: selection >= 0 && barre.index !== selection ? 0.55 : 1 }"/>

        <text v-for="(etiquette, index) in etiquettes" v-show="etiquette" :key="`etiquette-${index}`"
              :x="MARGE.gauche + colonne * (index + 0.5)" :y="HAUTEUR - 4" text-anchor="middle" font-size="10" fill="currentColor">
          {{ etiquette }}
        </text>

        <!-- Zones de toucher : toute la hauteur de la colonne, plus facile que la barre seule. -->
        <rect v-for="(_, index) in valeurs" :key="`zone-${index}`"
              :x="MARGE.gauche + colonne * index" y="0" :width="colonne" :height="HAUTEUR"
              fill="transparent" class="cursor-pointer" @click="emit('selectionner', index)"/>
      </svg>
    </div>
  </figure>
</template>
