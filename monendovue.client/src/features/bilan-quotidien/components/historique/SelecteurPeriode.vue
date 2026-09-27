<script setup lang="ts">
import { Button } from '@/shared/components/ui/button';
import type { ModePeriode, PeriodeHistorique } from '@/features/bilan-quotidien/types/historique';

defineProps<{ periode: PeriodeHistorique }>();

const emit = defineEmits<{
  'changer-mode': [mode: ModePeriode];
  precedente: [];
  suivante: [];
  aujourdhui: [];
}>();

const modes: { valeur: ModePeriode; libelle: string }[] = [
  { valeur: 'semaine', libelle: 'Semaine' },
  { valeur: 'mois', libelle: 'Mois' },
];
</script>

<template>
  <div class="flex flex-col gap-3 w-full">
    <div class="grid grid-cols-2 gap-2" role="group" aria-label="Période affichée">
      <Button
          v-for="mode in modes"
          :key="mode.valeur"
          type="button"
          class="h-11"
          :variant="periode.mode === mode.valeur ? 'selected' : 'outline'"
          :aria-pressed="periode.mode === mode.valeur"
          @click="emit('changer-mode', mode.valeur)"
      >
        {{ mode.libelle }}
      </Button>
    </div>

    <div class="flex items-start justify-between gap-2">
      <Button
          type="button" variant="outline" size="sm" class="h-11 w-11 p-0 shrink-0"
          :aria-label="periode.mode === 'mois' ? 'Mois précédent' : 'Semaine précédente'"
          @click="emit('precedente')"
      >
        <i class="material-symbols-outlined text-lg" aria-hidden="true">chevron_left</i>
      </Button>
      <div class="flex flex-1 flex-col items-center justify-center min-w-0 min-h-11">
        <h2 class="m-0 w-full text-center text-base font-semibold text-headline first-letter:uppercase truncate" aria-live="polite">
          {{ periode.libelle }}
        </h2>
        <button
            v-if="!periode.contientAujourdhui"
            type="button"
            class="text-sm text-paragraph underline min-h-[44px] px-2"
            @click="emit('aujourdhui')"
        >
          Revenir à aujourd'hui
        </button>
      </div>
      <Button
          type="button" variant="outline" size="sm" class="h-11 w-11 p-0 shrink-0"
          :disabled="!periode.suivantePossible"
          :aria-label="periode.mode === 'mois' ? 'Mois suivant' : 'Semaine suivante'"
          @click="emit('suivante')"
      >
        <i class="material-symbols-outlined text-lg" aria-hidden="true">chevron_right</i>
      </Button>
    </div>
  </div>
</template>
