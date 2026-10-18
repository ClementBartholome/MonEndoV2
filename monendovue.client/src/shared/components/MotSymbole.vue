<template>
  <!-- Mot-symbole MonEndo : une petite frise de cinq traits (un cycle qui monte puis redescend) et le nom en Literata. -->
  <span class="inline-flex items-center text-texte" :class="grand ? 'gap-3' : 'gap-2'">
    <span class="flex items-end" :class="grand ? 'gap-[5px]' : 'gap-[3px]'" aria-hidden="true">
      <span v-for="(trait, index) in TRAITS" :key="index" class="rounded-full" :class="[trait.couleur, grand ? trait.grand : trait.petit]"></span>
    </span>
    <span class="font-serif font-semibold leading-none" :class="grand ? 'text-titre-2' : 'text-[1.1875rem]'">MonEndo</span>
  </span>
</template>

<script setup lang="ts">
/** `grand` : pages d'accueil des comptes (bienvenue, connexion) ; sinon en-tête et barre latérale. */
defineProps<{ grand?: boolean }>();

const TRAITS = [
  { couleur: 'bg-intensite-1', petit: 'h-[5px] w-[5px]', grand: 'h-2 w-2' },
  { couleur: 'bg-intensite-5', petit: 'h-2 w-[5px]', grand: 'h-3.5 w-2' },
  { couleur: 'bg-teinte-regles', petit: 'h-[11px] w-[5px]', grand: 'h-5 w-2' },
  { couleur: 'bg-intensite-5', petit: 'h-2 w-[5px]', grand: 'h-3.5 w-2' },
  { couleur: 'bg-intensite-1', petit: 'h-[5px] w-[5px]', grand: 'h-2 w-2' },
];
</script>
