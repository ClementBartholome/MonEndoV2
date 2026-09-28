<template>
  <nav aria-label="Navigation principale"
       class="barre-navigation fixed inset-x-0 bottom-0 z-40 grid grid-cols-5 items-center border-t border-trait bg-surface lg:hidden">
    <router-link
        v-for="entree in navigationPrincipale"
        :key="entree.vers"
        :to="entree.vers"
        class="entree flex flex-col items-center gap-0.5 text-[11px]"
        :class="estActive(entree, route.path) ? 'font-semibold text-texte' : 'text-texte-2'"
        :aria-current="estActive(entree, route.path) ? 'page' : undefined">
      <i class="material-symbols-outlined pastille" :class="{ active: estActive(entree, route.path) }" aria-hidden="true">{{ entree.icone }}</i>
      {{ entree.libelle }}
    </router-link>
    <button type="button"
            class="entree flex flex-col items-center gap-0.5 text-[11px]"
            :class="plusActif ? 'font-semibold text-texte' : 'text-texte-2'"
            :aria-expanded="menuOuvert"
            @click="menuOuvert = true">
      <i class="material-symbols-outlined pastille" :class="{ active: plusActif }" aria-hidden="true">menu</i>
      Plus
    </button>
  </nav>
  <MenuPlus v-model:open="menuOuvert" @deconnexion="emit('deconnexion')"/>
</template>

<script setup lang="ts">
import {computed, ref} from 'vue';
import {useRoute} from 'vue-router';
import MenuPlus from './MenuPlus.vue';
import {estActive, navigationCompte, navigationPrincipale, navigationSecondaire} from '@/shared/config/navigation';

const emit = defineEmits<{ deconnexion: [] }>();
const route = useRoute();
const menuOuvert = ref(false);

/** « Plus » est mis en avant sur les pages qu'il regroupe. */
const plusActif = computed(() => menuOuvert.value
    || [...navigationSecondaire, navigationCompte].some((entree) => estActive(entree, route.path)));
</script>

<style scoped>
/* Hauteur fixe : les éléments collés en bas de page se posent au-dessus (SaisieBilan : bottom 4.25rem). */
.barre-navigation {
  height: 4.25rem;
}

.entree {
  min-height: 44px;
}

.pastille {
  box-sizing: border-box;
  width: 56px;
  height: 32px;
  padding: 5px 0;
  border-radius: 16px;
  font-size: 22px;
  text-align: center;
}

.pastille.active {
  background: var(--couleur-accent);
  color: var(--couleur-sur-accent);
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 24;
}
</style>
