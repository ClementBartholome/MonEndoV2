<template>
  <section class="flex flex-col gap-2.5 border-y border-trait py-3.5" aria-labelledby="titre-semaine">
    <h2 id="titre-semaine" class="m-0 text-legende font-semibold uppercase tracking-wider text-texte-2">Ta semaine</h2>
    <Frise :jours="frise" numeros :aujourdhui="aujourdhui"/>
    <p class="m-0 text-corps leading-relaxed text-texte">{{ phrases.length ? phrases.join(' ') : 'Ta frise se remplira jour après jour, au fil de tes notes.' }}</p>
    <router-link v-if="phrases.length" to="/bilan-quotidien?onglet=analyse" class="inline-flex min-h-11 items-center self-start text-sm font-medium !text-lien">
      Voir l'analyse
    </router-link>
  </section>
</template>

<script setup lang="ts">
import Frise from '@/shared/components/Frise.vue';
import type { JourFrise } from '@/shared/types/frise';

/** La semaine en un coup d'œil : frise des 7 jours (douleur, règles) et faits descriptifs. `aujourdhui` : AAAA-MM-JJ. */
defineProps<{ phrases: string[]; frise: JourFrise[]; aujourdhui: string }>();
</script>
