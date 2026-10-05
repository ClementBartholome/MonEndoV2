<script setup lang="ts">
import { computed } from 'vue';
import { categoriesDuBilan } from '@/features/bilan-quotidien/config/categories';
import { groupesDeQuestions, lignesDeRecap } from '@/features/bilan-quotidien/config/questions';
import { useCategoriesBilan } from '@/features/bilan-quotidien/composables/useCategoriesBilan';
import type { BilanQuotidien } from '@/features/bilan-quotidien/types/bilan-quotidien';

/**
 * Réponses données aux catégories facultatives (urinaire, saignements, nuit et journée…) : seulement ce qui est renseigné.
 * La catégorie discrète « Rapports » ne s'affiche que si elle est activée dans Paramètres.
 */
const props = defineProps<{ bilan: BilanQuotidien }>();
const { estActive } = useCategoriesBilan();

const blocs = computed(() =>
  categoriesDuBilan.flatMap((categorie) => {
    const groupe = groupesDeQuestions[categorie.id];
    if (!groupe || (categorie.id === 'rapports' && !estActive('rapports'))) return [];
    const lignes = lignesDeRecap(groupe.questions, props.bilan);
    return lignes.length ? [{ id: categorie.id, titre: categorie.titre, icone: categorie.icone, lignes }] : [];
  }),
);
</script>

<template>
  <div v-for="bloc in blocs" :key="bloc.id" class="mb-3 rounded-controle bg-fond p-3">
    <p class="mb-2 flex items-center gap-1 text-xs text-texte-3">
      <i class="material-symbols-outlined text-2xl leading-6" aria-hidden="true">{{ bloc.icone }}</i>
      {{ bloc.titre }}
    </p>
    <ul class="flex flex-col gap-1">
      <li v-for="ligne in bloc.lignes" :key="ligne.libelle" class="flex flex-wrap items-center gap-x-2 text-sm">
        <span class="text-texte-2">{{ ligne.libelle }} :</span>
        <span class="font-semibold text-texte">{{ ligne.valeur }}</span>
      </li>
    </ul>
  </div>
</template>
