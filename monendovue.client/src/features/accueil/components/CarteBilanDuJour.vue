<template>
  <section class="flex flex-col gap-3.5 rounded-grand bg-surface p-[18px] shadow-elevation" aria-labelledby="titre-bilan-du-jour">
    <div class="flex items-center gap-3.5">
      <i class="material-symbols-outlined rounded-[14px] p-2.5 text-[26px]"
         :class="bilan ? 'bg-teinte-traitement-fond text-teinte-traitement' : 'bg-teinte-bilan-fond text-teinte-bilan'"
         aria-hidden="true">{{ bilan ? 'event_available' : 'event_note' }}</i>
      <div class="flex grow flex-col gap-0.5">
        <h2 id="titre-bilan-du-jour" class="m-0 text-[17px] font-semibold tracking-normal text-texte">{{ bilan ? 'Bilan du jour fait' : 'Bilan du jour' }}</h2>
        <p v-if="!bilan" class="m-0 text-sm leading-snug text-texte-2">Douleur, émotions, fatigue… fais le point sur ta journée.</p>
      </div>
      <router-link v-if="bilan" to="/bilan-quotidien" class="inline-flex min-h-11 items-center gap-1 text-sm font-medium !text-lien">
        <i class="material-symbols-outlined text-lg" aria-hidden="true">edit</i>Modifier
      </router-link>
    </div>

    <router-link v-if="!bilan" to="/bilan-quotidien"
                 class="flex min-h-12 items-center justify-center rounded-[14px] bg-button text-[15px] font-semibold !text-texte">
      Faire mon bilan
    </router-link>

    <dl v-else class="m-0 grid grid-cols-3 gap-2">
      <div class="flex flex-col rounded-xl bg-fond p-2.5">
        <dt class="text-xs text-texte-3">Douleur</dt>
        <dd class="m-0 text-base font-semibold text-texte">{{ bilan.douleurMoyenne }}/10</dd>
      </div>
      <div class="flex min-w-0 flex-col rounded-xl bg-fond p-2.5">
        <dt class="text-xs text-texte-3">Émotions</dt>
        <dd class="m-0 truncate text-sm font-semibold leading-6 text-texte">{{ libellesEmotions }}</dd>
      </div>
      <div class="flex flex-col rounded-xl bg-fond p-2.5">
        <dt class="text-xs text-texte-3">Fatigue</dt>
        <dd class="m-0 text-base font-semibold text-texte">{{ bilan.fatigue !== null ? `${bilan.fatigue}/5` : '—' }}</dd>
      </div>
    </dl>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { emotions } from '@/features/bilan-quotidien/config/emotions';
import type { BilanAujourdhui } from '../types/aujourdhui';

const props = defineProps<{ bilan: BilanAujourdhui | null }>();

const libellesEmotions = computed(() => (props.bilan?.emotions ?? [])
    .map((code) => emotions.find((e) => e.code === code)?.libelle ?? code)
    .join(', '));
</script>
