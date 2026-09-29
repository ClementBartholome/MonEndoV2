<script setup lang="ts">
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
  <div class="flex w-full flex-col gap-3">
    <!-- Bascule de vue : même segment que les onglets de la page Cycle. -->
    <div class="grid grid-cols-2 gap-1 rounded-controle bg-surface-2 p-1" role="group" aria-label="Période affichée">
      <button
          v-for="mode in modes"
          :key="mode.valeur"
          type="button"
          class="min-h-10 rounded-controle text-sm"
          :class="periode.mode === mode.valeur ? 'bg-surface font-semibold text-texte shadow-elevation' : 'text-texte-2'"
          :aria-pressed="periode.mode === mode.valeur"
          @click="emit('changer-mode', mode.valeur)"
      >
        {{ mode.libelle }}
      </button>
    </div>

    <div class="-mx-3 flex items-start justify-between gap-2">
      <button
          type="button" class="flex h-11 w-11 shrink-0 items-center justify-center rounded-full text-texte hover:bg-surface-2"
          :aria-label="periode.mode === 'mois' ? 'Mois précédent' : 'Semaine précédente'"
          @click="emit('precedente')"
      >
        <i class="material-symbols-outlined" aria-hidden="true">chevron_left</i>
      </button>
      <div class="flex min-h-11 min-w-0 flex-1 flex-col items-center justify-center">
        <h2 class="m-0 w-full truncate text-center text-base font-semibold tracking-normal text-texte first-letter:uppercase" aria-live="polite">
          {{ periode.libelle }}
        </h2>
        <button
            v-if="!periode.contientAujourdhui"
            type="button"
            class="min-h-11 px-2 text-sm font-medium text-lien"
            @click="emit('aujourdhui')"
        >
          Revenir à aujourd'hui
        </button>
      </div>
      <button
          type="button" class="flex h-11 w-11 shrink-0 items-center justify-center rounded-full text-texte hover:bg-surface-2 disabled:text-trait"
          :disabled="!periode.suivantePossible"
          :aria-label="periode.mode === 'mois' ? 'Mois suivant' : 'Semaine suivante'"
          @click="emit('suivante')"
      >
        <i class="material-symbols-outlined" aria-hidden="true">chevron_right</i>
      </button>
    </div>
  </div>
</template>
