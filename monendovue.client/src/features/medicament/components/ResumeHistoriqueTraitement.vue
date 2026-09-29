<template>
  <section aria-label="Ce mois-ci" class="grid rounded-carte bg-surface shadow-elevation" :class="planifie ? 'grid-cols-3' : 'grid-cols-2'">
    <div class="flex flex-col gap-0.5 px-3 py-3.5">
      <span class="text-2xl font-semibold leading-tight text-texte">
        {{ historique.faites }}<span v-if="planifie" class="text-sm font-medium text-texte-3">/{{ historique.prevues }}</span>
      </span>
      <span class="text-xs leading-snug text-texte-3">{{ libelleFaites }}</span>
    </div>
    <div v-if="planifie" class="flex flex-col gap-0.5 border-l border-trait px-3 py-3.5">
      <span class="text-2xl font-semibold leading-tight text-texte">{{ historique.ignorees }}</span>
      <span class="text-xs leading-snug text-texte-3">{{ historique.ignorees > 1 ? 'ignorées' : 'ignorée' }}</span>
    </div>
    <div class="flex flex-col gap-0.5 border-l border-trait px-3 py-3.5">
      <span class="text-2xl font-semibold leading-tight text-texte">{{ historique.faitesMoisPrecedent }}</span>
      <span class="text-xs leading-snug text-texte-3">le mois précédent</span>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { HistoriqueTraitement } from '../types/traitements';

const props = defineProps<{ historique: HistoriqueTraitement }>();

/** Traitement à heures fixes : les prises se comptent sur le nombre prévu. */
const planifie = computed(() => props.historique.traitement.type === 'Medicamenteux' && props.historique.traitement.frequence !== 'AuBesoin');

const libelleFaites = computed(() => {
  const n = props.historique.faites;
  if (props.historique.traitement.type === 'NonMedicamenteux') return n > 1 ? 'séances' : 'séance';
  return planifie.value ? 'prises sur les prévues' : n > 1 ? 'prises' : 'prise';
});
</script>
