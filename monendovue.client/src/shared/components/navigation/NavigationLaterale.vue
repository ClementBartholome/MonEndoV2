<template>
  <nav aria-label="Navigation principale"
       class="navigation-laterale fixed inset-y-0 left-0 z-30 hidden w-[200px] flex-col gap-1 overflow-y-auto border-r border-trait bg-fond px-3 py-4 lg:flex">
    <router-link to="/" class="mb-4 self-center" aria-label="Accueil MonEndo">
      <img src="@/images/MonEndo_transparent.png" alt="" class="h-24 w-24">
    </router-link>
    <router-link
        v-for="entree in entrees"
        :key="entree.vers"
        :to="entree.vers"
        class="flex min-h-11 items-center gap-3 rounded-controle px-3 text-sm"
        :class="estActive(entree, route.path) ? 'bg-surface-2 font-semibold text-texte' : 'text-texte-2 hover:bg-surface-2'"
        :aria-current="estActive(entree, route.path) ? 'page' : undefined">
      <i class="material-symbols-outlined text-xl" :class="{ remplie: estActive(entree, route.path) }" aria-hidden="true">{{ entree.icone }}</i>
      {{ entree.libelle }}
    </router-link>
  </nav>
</template>

<script setup lang="ts">
import {useRoute} from 'vue-router';
import {estActive, navigationCompte, navigationPrincipale, navigationSecondaire} from '@/shared/config/navigation';

const route = useRoute();
const entrees = [...navigationPrincipale, ...navigationSecondaire, navigationCompte];
</script>

<style scoped>
.remplie {
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}
</style>
