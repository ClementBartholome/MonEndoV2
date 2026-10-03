<template>
  <ul class="m-0 grid list-none grid-cols-2 gap-x-3 gap-y-1.5 p-0 text-left text-[0.8125rem] text-texte-2" aria-label="Règles du mot de passe">
    <li v-for="critere in criteres" :key="critere.cle" class="flex items-center gap-1.5">
      <i class="material-symbols-outlined text-lg" :class="critere.respecte ? 'text-teinte-traitement' : 'text-contour'" aria-hidden="true">{{ critere.respecte ? 'check_circle' : 'radio_button_unchecked' }}</i>
      <span>{{ critere.libelle }}<span class="sr-only"> : {{ critere.respecte ? 'respectée' : 'pas encore respectée' }}</span></span>
    </li>
  </ul>
</template>

<script setup lang="ts">
/** Les règles du mot de passe, cochées au fil de la saisie. */
defineProps<{ criteres: { cle: string; libelle: string; respecte: boolean }[] }>();
</script>
