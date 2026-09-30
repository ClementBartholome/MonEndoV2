<template>
  <section aria-labelledby="titre-cycle-en-cours" class="flex items-center gap-3.5 rounded-carte bg-surface px-[18px] py-4 shadow-elevation">
    <div class="flex min-w-0 grow flex-col">
      <span class="text-legende text-texte-3">Cycle en cours</span>
      <h2 id="titre-cycle-en-cours" class="m-0 text-xl font-semibold tracking-normal"
          :class="enCours?.jourDeRegles ? 'text-teinte-regles' : 'text-texte'">{{ titre }}</h2>
      <span class="text-legende text-texte-2">{{ detail }}</span>
    </div>
    <span v-if="enCours?.jourDeRegles" class="inline-flex shrink-0 items-center gap-1 text-sm font-medium text-etat-fait">
      <i class="material-symbols-outlined icone-pleine text-xl" aria-hidden="true">check_circle</i>Noté aujourd'hui
    </span>
    <button v-else type="button" :disabled="envoi"
            class="inline-flex min-h-11 shrink-0 items-center gap-1.5 rounded-controle bg-button px-3.5 text-sm font-semibold text-texte disabled:opacity-60"
            @click="emit('noterAujourdhui')">
      <i v-if="envoi" class="material-symbols-outlined animate-spin text-lg" aria-hidden="true">progress_activity</i>Règles aujourd'hui
    </button>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { CycleEnCours } from '../types/cycle';
import { jourEnToutesLettres } from '../utils/cycle';

const props = defineProps<{ enCours: CycleEnCours | null; envoi: boolean }>();
const emit = defineEmits<{ noterAujourdhui: [] }>();

const titre = computed(() => {
  if (!props.enCours) return 'Pas de règles récentes';
  return props.enCours.jourDeRegles ? `Règles · jour ${props.enCours.jourDeRegles}` : `Jour ${props.enCours.jourDuCycle} du cycle`;
});

const detail = computed(() => {
  if (!props.enCours) return 'Touche un jour du calendrier pour noter tes règles.';
  const debut = jourEnToutesLettres(props.enCours.debut);
  return props.enCours.jourDeRegles ? `Débutées le ${debut}` : `Dernières règles le ${debut}`;
});
</script>
