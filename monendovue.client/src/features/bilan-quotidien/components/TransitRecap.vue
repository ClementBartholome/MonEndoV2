<script setup lang="ts">
import { computed } from 'vue';
import { libelleBristol } from '@/features/bilan-quotidien/config/transit';
import type { BilanQuotidien, IntensiteTransit } from '@/features/bilan-quotidien/types/bilan-quotidien';

const props = defineProps<{ bilan: BilanQuotidien }>();

const libelleSymptome = (present: boolean | null | undefined, intensite: IntensiteTransit | null | undefined) => {
  if (present === true) return intensite ? `Oui · ${intensite.toLowerCase()}` : 'Oui';
  if (present === false) return 'Non';
  return null;
};

const lignes = computed(() => {
  const { selles, typeBristol, crampesEstomac, intensiteCrampes, ballonnements, intensiteBallonnements } = props.bilan;
  const sellesTexte = selles === true ? (libelleBristol(typeBristol) ?? 'Oui') : selles === false ? 'Non' : null;

  return [
    { icone: 'gastroenterology', libelle: 'Selles', valeur: sellesTexte },
    { icone: 'pulse_alert', libelle: "Crampes d'estomac", valeur: libelleSymptome(crampesEstomac, intensiteCrampes) },
    { icone: 'bubble_chart', libelle: 'Ballonnements', valeur: libelleSymptome(ballonnements, intensiteBallonnements) },
  ].filter((ligne) => ligne.valeur !== null);
});
</script>

<template>
  <div class="bg-white rounded-xl border border-gray-100 p-3 mb-3">
    <p class="text-xs text-muted-foreground flex items-center gap-1 mb-2">
      <i class="material-symbols-outlined text-base">gastroenterology</i>
      Transit
    </p>
    <ul v-if="lignes.length" class="flex flex-col gap-1">
      <li v-for="ligne in lignes" :key="ligne.libelle" class="flex flex-wrap items-center gap-x-2 text-sm">
        <span class="text-paragraph">{{ ligne.libelle }} :</span>
        <span class="font-semibold text-headline">{{ ligne.valeur }}</span>
      </li>
    </ul>
    <p v-else class="text-gray-500 italic text-sm">Non renseigné</p>
  </div>
</template>
