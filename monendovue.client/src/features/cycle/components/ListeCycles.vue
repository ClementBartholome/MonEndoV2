<template>
  <section aria-labelledby="titre-mes-cycles" class="flex flex-col gap-2">
    <div class="flex items-baseline justify-between">
      <h2 id="titre-mes-cycles" class="m-0 text-[17px] font-semibold tracking-normal text-texte">Mes cycles</h2>
      <span v-if="dureeMoyenne !== null" class="text-[13px] text-texte-3">en moyenne {{ jours(dureeMoyenne) }}<span class="sr-only"> sur les 6 derniers cycles</span></span>
    </div>
    <ul v-if="cycles.length" class="m-0 flex list-none flex-col rounded-carte bg-surface p-0 shadow-elevation">
      <li v-for="(cycle, index) in cycles" :key="cycle.debut"
          class="flex min-h-[58px] items-center gap-3 px-3.5 py-2.5 text-left" :class="{ 'border-t border-trait': index > 0 }">
        <span class="flex min-w-0 grow flex-col">
          <span class="text-[15px] font-medium text-texte">Début le {{ jourCourt(cycle.debut) }}</span>
          <span class="text-[13px] text-texte-3">Règles {{ jours(cycle.joursDeRegles) }}</span>
        </span>
        <span class="shrink-0 text-sm font-semibold text-texte-2">{{ jours(cycle.duree) }}</span>
      </li>
    </ul>
    <button v-if="plusAnciens > 0" type="button" class="inline-flex min-h-11 items-center gap-1.5 self-start text-sm font-medium text-lien" @click="emit('voirPlus')">
      <i class="material-symbols-outlined text-lg" aria-hidden="true">expand_more</i>Voir les cycles précédents ({{ plusAnciens }})
    </button>
    <p v-if="!cycles.length" class="m-0 rounded-carte bg-surface-2 px-4 py-3 text-sm text-texte-2">
      Tes cycles apparaîtront ici dès que deux débuts de règles seront notés.
    </p>
  </section>
</template>

<script setup lang="ts">
import type { CycleTermine } from '../types/cycle';
import { jourCourt, jours } from '../utils/cycle';

defineProps<{ cycles: CycleTermine[]; dureeMoyenne: number | null; plusAnciens: number }>();
const emit = defineEmits<{ voirPlus: [] }>();
</script>
