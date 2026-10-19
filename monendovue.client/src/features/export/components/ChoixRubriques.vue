<template>
  <fieldset class="m-0 border-0 p-0">
    <legend class="mb-2.5 p-0 text-corps font-semibold text-texte">À inclure</legend>
    <div class="flex flex-col rounded-carte bg-surface shadow-elevation">
      <div v-for="(rubrique, index) in RUBRIQUES" :key="rubrique.cle"
           class="flex min-h-16 items-center gap-3 px-4 py-2 text-left" :class="{ 'border-t border-trait': index > 0 }">
        <span :id="`rubrique-${rubrique.cle}`" class="flex min-w-0 grow flex-col">
          <span class="text-corps font-medium text-texte">{{ rubrique.titre }}</span>
          <span class="text-legende font-normal text-texte-3">{{ rubrique.detail }}</span>
        </span>
        <!-- Interrupteur aux couleurs de l'app (les cases du système étaient bleues), même composant que les catégories du bilan. -->
        <Switch :checked="rubriques[rubrique.cle]" :aria-labelledby="`rubrique-${rubrique.cle}`"
                @update:checked="(coche: boolean) => (rubriques = { ...rubriques, [rubrique.cle]: coche })"/>
      </div>
    </div>
    <p v-if="aucune" role="alert" class="m-0 mt-2 text-left text-legende text-danger">Choisis au moins une rubrique.</p>
  </fieldset>
</template>

<script setup lang="ts">
import { Switch } from '@/shared/components/ui/switch';
import type { Rubrique } from '../types/synthese';

/** Rubriques du suivi à faire figurer dans le PDF. */
const rubriques = defineModel<Record<Rubrique, boolean>>({ required: true });
defineProps<{ aucune: boolean }>();

const RUBRIQUES: { cle: Rubrique; titre: string; detail: string }[] = [
  { cle: 'douleurs', titre: 'Douleurs', detail: 'Par type, intensité de 0 à 10' },
  { cle: 'cycle', titre: 'Cycle', detail: 'Règles, flux et symptômes' },
  { cle: 'traitements', titre: 'Traitements', detail: 'Prises, séances et dates des traitements' },
  { cle: 'bilans', titre: 'Bilans quotidiens', detail: 'Émotions, fatigue, nuit et journée, notes' },
  { cle: 'activite', titre: 'Activité physique', detail: 'Séances et effet sur la douleur' },
  { cle: 'transit', titre: 'Ancien suivi du transit', detail: 'Avant les bilans quotidiens' },
  { cle: 'rapports', titre: 'Rapports', detail: 'Douleur pendant ou après un rapport (désactivée par défaut)' },
];
</script>
