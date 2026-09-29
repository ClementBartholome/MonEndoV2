<template>
  <section aria-labelledby="titre-episode-acne" class="flex items-center gap-3.5 rounded-carte bg-surface px-[18px] py-4 shadow-elevation">
    <div class="flex min-w-0 grow flex-col">
      <span class="text-[13px] text-texte-3">Acné</span>
      <h2 id="titre-episode-acne" class="m-0 text-lg font-semibold tracking-normal"
          :class="enCours ? 'text-teinte-symptome' : 'text-texte'">{{ enCours ? 'En ce moment' : 'Pas en ce moment' }}</h2>
      <span class="text-[13px] text-texte-2">{{ detail }}</span>
    </div>
    <button type="button"
            class="inline-flex min-h-11 shrink-0 items-center rounded-controle border-[1.5px] border-contour px-3.5 text-sm font-medium text-texte"
            @click="enCours ? emit('terminer') : emit('commencer')">
      {{ enCours ? 'Ça s\'est calmé' : 'L\'acné revient' }}
    </button>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { EpisodeAcne } from '../types/acne';
import { jourCourt, jours } from '../utils/cycle';

const props = defineProps<{ enCours: EpisodeAcne | null; dernier: EpisodeAcne | null }>();
const emit = defineEmits<{ commencer: []; terminer: [] }>();

const detail = computed(() => {
  if (props.enCours) return `Depuis le ${jourCourt(props.enCours.debut)} · ${jours(props.enCours.jours)}`;
  if (props.dernier?.fin) return `Dernier épisode terminé le ${jourCourt(props.dernier.fin)}`;
  return 'Rien à noter chaque jour : indique quand elle apparaît.';
});
</script>
