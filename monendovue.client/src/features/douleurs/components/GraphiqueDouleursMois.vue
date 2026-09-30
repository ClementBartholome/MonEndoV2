<template>
  <section class="flex flex-col gap-2.5 rounded-carte bg-surface px-4 pb-3 pt-4 shadow-elevation" aria-labelledby="titre-graphique-douleurs">
    <h2 id="titre-graphique-douleurs" class="m-0 text-[15px] font-semibold tracking-normal text-texte">Intensité la plus forte, jour par jour</h2>
    <!-- Les règles sont un repère sous l'axe, jamais un fond de colonne : il se lirait comme une barre de douleur. -->
    <div class="flex h-[128px] items-end gap-[3px]" role="img" :aria-label="description">
      <div v-for="jour in jours" :key="jour.jour" class="flex h-full grow flex-col justify-end gap-1">
        <div class="rounded-[3px]" :style="barre(jour.intensiteMax)"></div>
        <div class="h-1 rounded-full" :class="{ 'bg-teinte-regles': jour.regles }"></div>
      </div>
    </div>
    <div class="flex justify-between text-[11px] text-texte-3" aria-hidden="true">
      <span>1</span><span>8</span><span>15</span><span>22</span><span>{{ jours.length }}</span>
    </div>
    <div class="flex flex-wrap items-center gap-x-3.5 gap-y-1 text-xs text-texte-2">
      <span v-if="jours.some((j) => j.regles)" class="inline-flex items-center gap-1.5">
        <span class="h-1 w-3 rounded-full bg-teinte-regles" aria-hidden="true"></span>Règles
      </span>
      <span>Plus la barre est foncée, plus la douleur était forte.</span>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { JourDuGraphique } from '../utils/douleurs';

const props = defineProps<{ jours: JourDuGraphique[] }>();

/** Barre de hauteur et de teinte proportionnelles à l'intensité ; un jour sans douleur reste un simple trait. */
function barre(intensite: number) {
  return intensite === 0
    ? { height: '4px', background: 'var(--couleur-trait)' }
    : { height: `${8 + intensite * 10}px`, background: `var(--intensite-${intensite})` };
}

/** Résumé textuel pour les lecteurs d'écran (le graphique lui-même est décoratif). */
const description = computed(() => {
  const notes = props.jours.filter((j) => j.intensiteMax > 0);
  if (notes.length === 0) return 'Aucune douleur notée ce mois-ci.';
  const max = Math.max(...notes.map((j) => j.intensiteMax));
  return `Douleur notée ${notes.length} jours ce mois-ci, au plus ${max} sur 10.`;
});
</script>
