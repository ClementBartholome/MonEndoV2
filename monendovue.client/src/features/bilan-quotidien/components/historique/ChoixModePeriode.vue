<script setup lang="ts">
import type { ModePeriode } from '@/features/bilan-quotidien/types/historique';

// Bascule compacte semaine / mois, posée à côté du titre de la page (elle prenait une rangée entière au-dessus des onglets).
defineProps<{ mode: ModePeriode }>();
const emit = defineEmits<{ changer: [mode: ModePeriode] }>();

const modes: { valeur: ModePeriode; libelle: string }[] = [
  { valeur: 'semaine', libelle: 'Semaine' },
  { valeur: 'mois', libelle: 'Mois' },
];
</script>

<template>
  <div class="grid shrink-0 grid-cols-2 gap-1 rounded-controle bg-surface-2 p-1" role="group" aria-label="Période affichée">
    <button
        v-for="choix in modes"
        :key="choix.valeur"
        type="button"
        class="min-h-11 rounded-controle px-3 text-sm"
        :class="mode === choix.valeur ? 'bg-surface font-semibold text-texte shadow-elevation' : 'text-texte-2'"
        :aria-pressed="mode === choix.valeur"
        @click="emit('changer', choix.valeur)"
    >
      {{ choix.libelle }}
    </button>
  </div>
</template>
