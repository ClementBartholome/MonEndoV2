<template>
  <section aria-label="Ce mois-ci" class="grid grid-cols-3 rounded-grand bg-surface shadow-elevation">
    <div class="flex flex-col gap-0.5 px-3 py-3.5">
      <span class="text-2xl font-semibold leading-tight text-texte">{{ chiffres.joursAvecDouleur }}</span>
      <span class="text-xs leading-snug text-texte-3">{{ chiffres.joursAvecDouleur > 1 ? 'jours avec douleur' : 'jour avec douleur' }}</span>
    </div>
    <div class="flex flex-col gap-0.5 border-l border-trait px-3 py-3.5">
      <span class="text-2xl font-semibold leading-tight text-texte">
        <template v-if="chiffres.intensiteMoyenne !== null">{{ moyenne }}<span class="text-sm font-medium text-texte-3">/10</span></template>
        <template v-else>—</template>
      </span>
      <span class="text-xs leading-snug text-texte-3">intensité moyenne</span>
    </div>
    <div class="flex min-w-0 flex-col gap-0.5 border-l border-trait px-3 py-3.5">
      <span class="truncate text-base font-semibold leading-8 text-texte">{{ chiffres.typeLePlusNote ?? '—' }}</span>
      <span class="text-xs leading-snug text-texte-3">la plus notée</span>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { ChiffresDuMois } from '../utils/douleurs';

const props = defineProps<{ chiffres: ChiffresDuMois }>();

const moyenne = computed(() => props.chiffres.intensiteMoyenne?.toLocaleString('fr-FR'));
</script>
