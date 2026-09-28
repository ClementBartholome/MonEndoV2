<template>
  <section class="flex flex-col gap-2.5" aria-labelledby="titre-ajouter">
    <h2 id="titre-ajouter" class="m-0 text-base font-semibold tracking-normal text-texte">Ajouter</h2>
    <div class="grid grid-cols-3 gap-2.5">
      <router-link v-for="tuile in tuiles" :key="tuile.libelle" :to="tuile.vers"
                   class="flex flex-col items-start gap-2.5 rounded-moyen bg-surface p-3.5 !text-texte shadow-elevation">
        <i class="material-symbols-outlined rounded-petit p-1.5 text-2xl" :class="tuile.teinte" aria-hidden="true">{{ tuile.icone }}</i>
        <span class="whitespace-nowrap text-sm font-medium">{{ tuile.libelle }}</span>
      </router-link>
    </div>
  </section>
</template>

<script setup lang="ts">
import { materialSymbols } from '@/shared/config/materialSymbols';

/** Raccourcis de saisie : chaque page de suivi ouvre sa saisie depuis ces liens. */
const tuiles = [
  { libelle: 'Douleur', icone: 'sick', vers: '/douleurs', teinte: 'bg-teinte-douleur-fond text-teinte-douleur' },
  { libelle: 'Règles', icone: materialSymbols.cycle, vers: '/cycle?onglet=cycles', teinte: 'bg-teinte-regles-fond text-teinte-regles' },
  { libelle: 'Symptôme', icone: materialSymbols.symptoms, vers: '/cycle?onglet=symptomes', teinte: 'bg-teinte-symptome-fond text-teinte-symptome' },
];
</script>
