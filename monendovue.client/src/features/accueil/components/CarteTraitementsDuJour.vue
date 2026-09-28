<template>
  <section class="flex flex-col rounded-grand bg-surface px-[18px] pb-1.5 pt-2 shadow-elevation" aria-labelledby="titre-traitements-du-jour">
    <div class="flex items-center justify-between">
      <h2 id="titre-traitements-du-jour" class="m-0 flex items-center gap-2 text-base font-semibold tracking-normal text-texte">
        <i class="material-symbols-outlined text-[22px] text-teinte-traitement" aria-hidden="true">pill</i>Traitements du jour
      </h2>
      <router-link to="/medicaments" class="inline-flex min-h-11 items-center text-[13px] !text-lien">Tout voir</router-link>
    </div>
    <div v-for="traitement in traitements" :key="traitement.id"
         class="flex min-h-[62px] items-center gap-3 border-t border-trait py-2">
      <div class="flex min-w-0 grow flex-col">
        <span class="text-[15px] font-medium text-texte">{{ traitement.nom }}</span>
        <span v-if="traitement.posologie" class="truncate text-[13px] text-texte-3">{{ traitement.posologie }}</span>
      </div>
      <span v-if="traitement.dernierePrise"
            class="inline-flex min-h-11 shrink-0 items-center gap-1 rounded-full bg-teinte-traitement-fond px-3 text-sm font-medium text-teinte-traitement">
        <i class="material-symbols-outlined text-lg" aria-hidden="true">check</i>
        <span aria-hidden="true">{{ heure(traitement) }}</span>
        <span class="sr-only">{{ libellePrise(traitement) }}</span>
      </span>
      <button type="button"
              class="inline-flex min-h-11 shrink-0 items-center gap-1 rounded-full border-[1.5px] border-contour px-3.5 text-sm font-medium text-texte disabled:opacity-60"
              :class="{ 'w-11 justify-center px-0': traitement.dernierePrise }"
              :disabled="priseEnCours === traitement.id"
              :aria-label="traitement.dernierePrise ? `Noter une autre prise de ${traitement.nom}` : `Noter une prise de ${traitement.nom}`"
              @click="emit('prendre', traitement)">
        <i class="material-symbols-outlined text-lg" :class="{ 'animate-spin': priseEnCours === traitement.id }" aria-hidden="true">
          {{ priseEnCours === traitement.id ? 'progress_activity' : 'add' }}
        </i><span v-if="!traitement.dernierePrise">Pris</span>
      </button>
    </div>
  </section>
</template>

<script setup lang="ts">
import type { TraitementAujourdhui } from '../types/aujourdhui';

defineProps<{ traitements: TraitementAujourdhui[]; priseEnCours: number | null }>();
const emit = defineEmits<{ prendre: [traitement: TraitementAujourdhui] }>();

/** Heure de la dernière prise : « 8 h 10 » (date locale sans fuseau). */
function heure(traitement: TraitementAujourdhui): string {
  const [heures, minutes] = traitement.dernierePrise!.slice(11, 16).split(':');
  return `${Number(heures)} h ${minutes}`;
}

/** Libellé lu : « Pris à 8 h 10 », ou « 2 prises, dernière à 14 h 05 ». */
function libellePrise(traitement: TraitementAujourdhui): string {
  return traitement.prisesDuJour > 1 ? `${traitement.prisesDuJour} prises, dernière à ${heure(traitement)}` : `Pris à ${heure(traitement)}`;
}
</script>
