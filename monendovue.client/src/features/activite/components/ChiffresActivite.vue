<template>
  <section aria-label="Ce mois-ci" class="grid grid-cols-3 rounded-carte bg-surface shadow-elevation">
    <div class="flex flex-col gap-0.5 px-3 py-3.5">
      <span class="text-2xl font-semibold leading-tight text-texte">{{ chiffres.seances }}</span>
      <span class="text-xs leading-snug text-texte-3">{{ chiffres.seances > 1 ? 'séances' : 'séance' }}</span>
    </div>
    <div class="flex flex-col gap-0.5 border-l border-trait px-3 py-3.5">
      <span class="text-2xl font-semibold leading-tight text-texte">{{ duree(chiffres.minutes) }}</span>
      <span class="text-xs leading-snug text-texte-3">au total</span>
    </div>
    <div class="flex flex-col gap-0.5 border-l border-trait px-3 py-3.5">
      <span class="text-2xl font-semibold leading-tight text-texte">
        <template v-if="chiffres.effetRenseigne">{{ chiffres.soulagee }}<span class="text-sm font-medium text-texte-3">/{{ chiffres.effetRenseigne }}</span></template>
        <template v-else>—</template>
      </span>
      <span class="text-xs leading-snug text-texte-3">douleur soulagée après</span>
    </div>
  </section>
</template>

<script setup lang="ts">
import { duree, type ChiffresActivite } from '../utils/activite';

defineProps<{ chiffres: ChiffresActivite }>();
</script>
