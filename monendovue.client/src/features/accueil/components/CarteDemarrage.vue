<template>
  <section class="flex flex-col gap-1 rounded-carte bg-surface px-[18px] pb-1.5 pt-4 shadow-elevation" aria-labelledby="titre-demarrage">
    <div class="flex items-start justify-between gap-3">
      <div class="flex flex-col gap-0.5 text-left">
        <h2 id="titre-demarrage" class="m-0 text-titre-carte font-semibold tracking-normal text-texte">Pour bien démarrer</h2>
        <p class="m-0 text-sm leading-snug text-texte-2">Quand tu veux, note ce que tu ressens aujourd'hui.</p>
      </div>
      <button type="button" aria-label="Masquer cette carte" class="-mr-2 -mt-2 flex h-11 w-11 shrink-0 items-center justify-center rounded-full text-texte-2 hover:bg-surface-2"
              @click="emit('masquer')">
        <i class="material-symbols-outlined" aria-hidden="true">close</i>
      </button>
    </div>
    <ul class="m-0 mt-2 flex list-none flex-col p-0">
      <li v-for="action in ACTIONS" :key="action.libelle" class="border-t border-trait">
        <router-link :to="action.vers" class="flex min-h-14 items-center gap-3 text-left !text-texte no-underline" @click="emit('masquer')">
          <i class="material-symbols-outlined rounded-controle p-1.5 text-titre-2" :class="action.teinte" aria-hidden="true">{{ action.icone }}</i>
          <span class="grow text-corps font-medium">{{ action.libelle }}</span>
          <i class="material-symbols-outlined text-texte-3" aria-hidden="true">chevron_right</i>
        </router-link>
      </li>
    </ul>
  </section>
</template>

<script setup lang="ts">
import { materialSymbols } from '@/shared/config/materialSymbols';

/** Carte du premier jour : deux saisies et un lien vers des sources fiables à portée de main, sans progression ni tâche à finir. Se masque dès qu'on l'utilise ou la ferme. */
const emit = defineEmits<{ masquer: [] }>();

const ACTIONS = [
  { libelle: 'Noter une douleur', icone: 'sick', vers: '/douleurs?ajouter', teinte: 'bg-teinte-douleur-fond text-teinte-douleur' },
  { libelle: 'Noter mes règles', icone: materialSymbols.cycle, vers: '/cycle?onglet=cycles', teinte: 'bg-teinte-regles-fond text-teinte-regles' },
  { libelle: 'Des sources fiables sur l\'endométriose', icone: 'menu_book', vers: '/s-informer', teinte: 'bg-teinte-neutre-fond text-teinte-neutre' },
];
</script>
