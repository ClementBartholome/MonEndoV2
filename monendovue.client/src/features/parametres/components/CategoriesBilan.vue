<template>
  <div class="flex flex-col gap-4 pb-2">
    <p class="m-0 text-left text-sm text-texte-2">
      Choisis ce qui apparaît dans ton bilan. La douleur, les émotions, la fatigue et le stress y restent toujours.
      Tes réponses déjà enregistrées sont conservées, même pour une catégorie masquée. Ce réglage reste sur cet appareil.
    </p>
    <ul class="m-0 flex list-none flex-col p-0">
      <li v-for="categorie in categoriesDuBilan" :key="categorie.id" class="border-t border-trait first:border-t-0">
        <label class="flex min-h-16 items-center gap-3 py-2 text-left">
          <i class="material-symbols-outlined text-teinte-bilan" aria-hidden="true">{{ categorie.icone }}</i>
          <span class="flex min-w-0 grow flex-col">
            <span class="text-corps font-medium text-texte">{{ categorie.titre }}</span>
            <span class="text-legende text-texte-3">{{ categorie.detail }}</span>
          </span>
          <Switch :checked="estActive(categorie.id)" :aria-label="`Afficher ${categorie.titre} dans le bilan`"
                  @update:checked="(actif: boolean) => definir(categorie.id, actif)"/>
        </label>
      </li>
    </ul>
    <button type="button" class="min-h-12 rounded-controle border-[1.5px] border-contour px-4 font-medium text-texte" @click="reinitialiser">
      Rétablir les réglages par défaut
    </button>
  </div>
</template>

<script setup lang="ts">
import { Switch } from '@/shared/components/ui/switch';
import { categoriesDuBilan } from '@/features/bilan-quotidien/config/categories';
import { useCategoriesBilan } from '@/features/bilan-quotidien/composables/useCategoriesBilan';

/** Catégories facultatives du bilan quotidien : un interrupteur chacune, enregistré dès le geste. */
const { estActive, definir, reinitialiser } = useCategoriesBilan();
</script>
