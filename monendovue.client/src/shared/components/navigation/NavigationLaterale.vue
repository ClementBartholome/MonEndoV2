<template>
  <nav aria-label="Navigation principale"
       class="navigation-laterale fixed inset-y-0 left-0 z-30 hidden w-[200px] flex-col gap-1 overflow-y-auto border-r border-trait bg-fond px-3 py-4 lg:flex">
    <router-link to="/" class="mb-6 mt-2 flex min-h-11 items-center px-3" aria-label="Accueil MonEndo">
      <MotSymbole/>
    </router-link>
    <template v-for="(groupe, index) in groupes" :key="index">
      <!-- Simple texte, pas un titre : la barre latérale ne doit pas entrer dans le plan de la page. -->
      <p v-if="groupe.titre" class="m-0 mt-3 px-3 text-xs font-semibold text-texte-3">{{ groupe.titre }}</p>
      <router-link
          v-for="entree in groupe.entrees"
          :key="entree.vers"
          :to="entree.vers"
          class="flex min-h-11 items-center gap-3 rounded-controle px-3 text-sm"
          :class="estActive(entree, route.path) ? 'bg-surface-2 font-semibold text-texte' : 'text-texte-2 hover:bg-surface-2'"
          :aria-current="estActive(entree, route.path) ? 'page' : undefined">
        <i class="material-symbols-outlined text-xl" :class="{ remplie: estActive(entree, route.path) }" aria-hidden="true">{{ entree.icone }}</i>
        {{ entree.libelle }}
      </router-link>
    </template>
  </nav>
</template>

<script setup lang="ts">
import MotSymbole from '@/shared/components/MotSymbole.vue';
import {useRoute} from 'vue-router';
import {estActive, navigationCompte, navigationPrincipale, navigationSecondaire} from '@/shared/config/navigation';

const route = useRoute();
/** Suivis du quotidien sans titre, puis les groupes du menu « Plus », puis les paramètres. */
const groupes = [
  { titre: '', entrees: navigationPrincipale },
  ...navigationSecondaire,
  { titre: '', entrees: [navigationCompte] },
];
</script>

<style scoped>
.remplie {
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}
</style>
