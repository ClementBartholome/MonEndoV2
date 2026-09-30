<template>
  <fieldset class="m-0 border-0 p-0">
    <legend class="mb-2.5 p-0 text-corps font-semibold text-texte">Période</legend>
    <div class="grid grid-cols-3 gap-2">
      <button v-for="option in OPTIONS" :key="option.valeur" type="button"
              class="min-h-11 rounded-controle border-[1.5px] text-sm"
              :class="choix === option.valeur ? 'border-texte bg-texte font-medium text-fond' : 'border-contour bg-surface text-texte'"
              :aria-pressed="choix === option.valeur"
              @click="choix = option.valeur">{{ option.libelle }}</button>
    </div>

    <div v-if="choix === 'autre'" class="mt-3 grid grid-cols-2 gap-2">
      <label class="flex min-w-0 flex-col gap-1 text-sm font-medium text-texte">Du
        <input v-model="du" type="date" :max="au || aujourdhui"
               class="min-h-11 min-w-0 rounded-controle border-[1.5px] border-contour bg-champ px-3 text-corps font-normal text-texte">
      </label>
      <label class="flex min-w-0 flex-col gap-1 text-sm font-medium text-texte">Au
        <input v-model="au" type="date" :min="du" :max="aujourdhui"
               class="min-h-11 min-w-0 rounded-controle border-[1.5px] border-contour bg-champ px-3 text-corps font-normal text-texte">
      </label>
    </div>

    <p v-if="erreur" role="alert" class="m-0 mt-2 text-left text-legende text-danger">{{ erreur }}</p>
    <p v-else class="m-0 mt-2 text-left text-legende text-texte-3" aria-live="polite">{{ libelle }}</p>
  </fieldset>
</template>

<script setup lang="ts">
import type { ChoixPeriode } from '../composables/usePreparationRendezVous';

/** Période du PDF : deux raccourcis, ou des dates libres (« Autre »), un an au plus. */
const choix = defineModel<ChoixPeriode>('choix', { required: true });
const du = defineModel<string>('du', { required: true });
const au = defineModel<string>('au', { required: true });
defineProps<{ aujourdhui: string; libelle: string; erreur: string | null }>();

const OPTIONS: { valeur: ChoixPeriode; libelle: string }[] = [
  { valeur: '1mois', libelle: '1 mois' },
  { valeur: '3mois', libelle: '3 mois' },
  { valeur: 'autre', libelle: 'Autre' },
];
</script>
