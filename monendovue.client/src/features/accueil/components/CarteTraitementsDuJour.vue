<template>
  <section class="flex flex-col rounded-carte bg-surface px-[18px] pb-1.5 pt-2 shadow-elevation" aria-labelledby="titre-traitements-du-jour">
    <div class="flex items-center justify-between">
      <h2 id="titre-traitements-du-jour" class="m-0 flex items-center gap-2 text-base font-semibold tracking-normal text-texte">
        <i class="material-symbols-outlined text-titre-2 text-teinte-traitement" aria-hidden="true">pill</i>Traitements du jour
      </h2>
      <router-link v-if="prises.length" to="/medicaments" class="inline-flex min-h-11 items-center text-legende !text-lien">
        {{ faites }} sur {{ prises.length }}
      </router-link>
    </div>

    <div v-for="prise in prises" :key="`${prise.traitementId}-${prise.heurePrevue}`"
         class="flex min-h-[62px] items-center gap-3 border-t border-trait py-2">
      <span class="w-[52px] shrink-0 text-legende font-semibold text-texte-2">{{ heureAffichee(prise.heurePrevue) }}</span>
      <div class="flex min-w-0 grow flex-col">
        <span class="text-corps font-medium text-texte">{{ prise.nom }}</span>
        <span v-if="prise.dose" class="truncate text-legende text-texte-3">{{ prise.dose }}</span>
      </div>
      <EtatPrise v-if="prise.reponse" :reponse="prise.reponse"/>
      <button v-else type="button"
              class="inline-flex min-h-11 shrink-0 items-center gap-1.5 rounded-controle bg-button px-3.5 text-sm font-semibold text-texte disabled:opacity-60"
              :disabled="priseEnCours !== null"
              :aria-label="`Je l'ai pris : ${prise.nom}, ${heureAffichee(prise.heurePrevue)}`"
              @click="emit('prendre', prise)">
        <i v-if="enCours(prise)" class="material-symbols-outlined animate-spin text-lg" aria-hidden="true">progress_activity</i>Je l'ai pris
      </button>
    </div>

    <div v-if="auBesoin.length" class="flex min-h-[52px] items-center gap-2 border-t border-trait text-legende text-texte-3">
      <span class="grow">Au besoin : {{ auBesoin.map((t) => t.nom).join(', ') }}</span>
      <router-link to="/medicaments" class="inline-flex min-h-11 items-center gap-0.5 font-medium !text-lien">
        <i class="material-symbols-outlined text-lg" aria-hidden="true">add</i>Noter une prise
      </router-link>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { PrisePrevue, TraitementAuBesoin } from '@/features/medicament/types/traitements';
import { heureAffichee } from '@/features/medicament/utils/prises';
import EtatPrise from '@/features/medicament/components/EtatPrise.vue';

const props = defineProps<{ prises: PrisePrevue[]; auBesoin: TraitementAuBesoin[]; priseEnCours: string | null }>();
const emit = defineEmits<{ prendre: [prise: PrisePrevue] }>();

const faites = computed(() => props.prises.filter((p) => p.reponse?.statut === 'Pris').length);

function enCours(prise: PrisePrevue) {
  return props.priseEnCours === `${prise.traitementId}-${prise.heurePrevue}`;
}
</script>
