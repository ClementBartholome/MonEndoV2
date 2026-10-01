<template>
  <button type="button" class="flex w-full items-start gap-3 px-4 py-3.5 text-left text-texte" @click="emit('ouvrir')">
    <span class="flex w-[4.75rem] shrink-0 flex-col pt-0.5 leading-snug">
      <span v-if="jour" class="text-legende text-texte-3">{{ jour }}</span>
      <span class="text-sm font-semibold">{{ heure }}</span>
    </span>
    <span class="flex min-w-0 grow flex-col gap-0.5">
      <span class="text-corps font-medium">{{ evenement.titre }}</span>
      <span v-if="evenement.lieu" class="text-sm text-texte-2">{{ evenement.lieu }}</span>
      <span v-if="puce" class="mt-1 self-start rounded-full bg-teinte-neutre-fond px-2.5 py-0.5 text-legende font-medium text-teinte-neutre">{{ puce }}</span>
    </span>
    <i class="material-symbols-outlined mt-1 text-texte-3" aria-hidden="true">chevron_right</i>
  </button>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { EvenementAgenda } from '../types/agenda';
import { heureDe, jourCourt } from '../utils/rendezVous';

/** Une ligne de la liste : jour (hors « aujourd'hui » et « demain », que le groupe dit déjà), heure, titre, lieu ; ouvre le détail. */
const props = defineProps<{ evenement: EvenementAgenda; avecJour: boolean; avecMois: boolean; puce?: string | null }>();
const emit = defineEmits<{ ouvrir: [] }>();

const heure = computed(() => heureDe(props.evenement));
const jour = computed(() => (props.avecJour ? jourCourt(props.evenement, props.avecMois) : null));
</script>
