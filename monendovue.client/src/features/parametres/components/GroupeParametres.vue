<template>
  <section class="flex flex-col gap-2" :aria-labelledby="idTitre">
    <h2 :id="idTitre" class="m-0 text-left text-legende font-semibold uppercase tracking-wider text-texte-3">{{ titre }}</h2>
    <div class="flex flex-col divide-y divide-trait overflow-hidden rounded-carte bg-surface shadow-elevation">
      <slot/>
    </div>
  </section>
</template>

<script setup lang="ts">
import { useId } from 'radix-vue';

/** Un groupe de réglages : titre de section et lignes séparées par un trait, dans une seule carte. */
defineProps<{ titre: string }>();
const idTitre = useId(undefined, 'groupe-parametres');
</script>
