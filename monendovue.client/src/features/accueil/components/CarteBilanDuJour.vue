<template>
  <section class="flex flex-col gap-3.5 rounded-carte bg-surface p-[18px] shadow-elevation" aria-labelledby="titre-bilan-du-jour">
    <div class="flex items-center gap-3.5">
      <i class="material-symbols-outlined rounded-controle bg-teinte-bilan-fond p-2.5 text-titre-page text-teinte-bilan" aria-hidden="true">event_note</i>
      <div class="flex grow flex-col gap-0.5">
        <h2 id="titre-bilan-du-jour" class="m-0 text-titre-carte font-semibold tracking-normal text-texte">Bilan du jour</h2>
        <p v-if="bilan" class="m-0 inline-flex items-center gap-1 text-sm font-medium text-etat-fait">
          <i class="material-symbols-outlined icone-pleine text-lg" aria-hidden="true">check_circle</i>Fait
        </p>
        <p v-else class="m-0 text-sm leading-snug text-texte-2">Douleur, émotions, fatigue… fais le point sur ta journée.</p>
      </div>
      <router-link v-if="bilan" to="/bilan-quotidien" class="inline-flex min-h-11 items-center gap-1 text-sm font-medium !text-lien">
        <i class="material-symbols-outlined text-lg" aria-hidden="true">edit</i>Modifier
      </router-link>
    </div>

    <router-link v-if="!bilan" to="/bilan-quotidien?ajouter"
                 class="flex min-h-12 items-center justify-center rounded-controle bg-button text-corps font-semibold !text-texte">
      Faire mon bilan
    </router-link>

    <dl v-else class="m-0 flex flex-col">
      <div class="flex min-h-11 items-center justify-between gap-3 border-b border-trait">
        <dt class="text-sm text-texte-2">Douleur</dt>
        <dd class="m-0 text-texte"><span class="chiffre text-lg font-semibold">{{ bilan.douleurMoyenne }}</span><span class="text-sm text-texte-2">/10</span></dd>
      </div>
      <div class="flex min-h-11 items-center justify-between gap-3 border-b border-trait">
        <dt class="shrink-0 text-sm text-texte-2">Émotions</dt>
        <dd class="m-0 min-w-0 truncate text-right text-sm font-medium text-texte">{{ libellesEmotions }}</dd>
      </div>
      <div class="flex min-h-11 items-center justify-between gap-3">
        <dt class="text-sm text-texte-2">Fatigue</dt>
        <dd class="m-0 text-texte">
          <template v-if="bilan.fatigue !== null"><span class="chiffre text-lg font-semibold">{{ bilan.fatigue }}</span><span class="text-sm text-texte-2">/5</span></template>
          <span v-else class="text-sm text-texte-3">—</span>
        </dd>
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
