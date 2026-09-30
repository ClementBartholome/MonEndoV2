<template>
  <fieldset class="m-0 border-0 p-0">
    <legend class="mb-2.5 p-0 text-corps font-semibold text-texte">À inclure</legend>
    <div class="flex flex-col rounded-carte bg-surface shadow-elevation">
      <label v-for="(rubrique, index) in RUBRIQUES" :key="rubrique.cle"
             class="flex min-h-16 cursor-pointer items-center gap-3 px-4 py-2 text-left" :class="{ 'border-t border-trait': index > 0 }">
        <span class="flex min-w-0 grow flex-col">
          <span class="text-corps font-medium text-texte">{{ rubrique.titre }}</span>
          <span class="text-legende font-normal text-texte-3">{{ rubrique.detail }}</span>
        </span>
        <input type="checkbox" :checked="rubriques[rubrique.cle]"
               @change="rubriques = { ...rubriques, [rubrique.cle]: ($event.target as HTMLInputElement).checked }"
               class="h-[22px] w-[22px] shrink-0 accent-[var(--couleur-lien)]">
      </label>
    </div>
    <p v-if="aucune" role="alert" class="m-0 mt-2 text-left text-legende text-danger">Choisis au moins une rubrique.</p>
  </fieldset>
</template>

<script setup lang="ts">
import type { Rubrique } from '../types/synthese';

/** Rubriques du suivi à faire figurer dans le PDF. */
const rubriques = defineModel<Record<Rubrique, boolean>>({ required: true });
defineProps<{ aucune: boolean }>();

const RUBRIQUES: { cle: Rubrique; titre: string; detail: string }[] = [
  { cle: 'douleurs', titre: 'Douleurs', detail: 'Par type, intensité de 0 à 10' },
  { cle: 'cycle', titre: 'Cycle', detail: 'Jours de règles et symptômes' },
  { cle: 'traitements', titre: 'Traitements', detail: 'Prises, séances et dates des traitements' },
  { cle: 'bilans', titre: 'Bilans quotidiens', detail: 'Émotions, fatigue, stress, transit, notes' },
  { cle: 'activite', titre: 'Activité physique', detail: 'Séances et effet sur la douleur' },
  { cle: 'transit', titre: 'Ancien suivi du transit', detail: 'Avant les bilans quotidiens' },
];
</script>
