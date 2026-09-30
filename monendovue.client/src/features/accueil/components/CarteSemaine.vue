<template>
  <section class="flex flex-col gap-1.5 rounded-carte bg-surface-2 px-[18px] py-4" aria-labelledby="titre-semaine">
    <h2 id="titre-semaine" class="m-0 inline-flex items-center gap-1.5 text-[13px] font-medium tracking-normal text-texte-2">
      <i class="material-symbols-outlined text-lg" aria-hidden="true">insights</i>Ta semaine
    </h2>
    <p class="m-0 text-[15px] leading-relaxed text-texte">{{ phrases.join(' ') }}</p>
    <router-link to="/bilan-quotidien?onglet=analyse" class="inline-flex min-h-11 items-center text-sm font-medium !text-lien">
      Voir l'analyse
    </router-link>
  </section>
</template>

<script setup lang="ts">
defineProps<{ phrases: string[] }>();
</script>
