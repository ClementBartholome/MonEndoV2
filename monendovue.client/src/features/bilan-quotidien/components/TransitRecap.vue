<script setup lang="ts">
import { computed } from 'vue';
import { lignesDeRecap, questionsTransitSuite } from '@/features/bilan-quotidien/config/questions';
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
  ].filter((ligne) => ligne.valeur !== null).concat(
    lignesDeRecap(questionsTransitSuite, props.bilan).map((ligne) => ({ icone: 'gastroenterology', libelle: ligne.libelle, valeur: ligne.valeur })),
  );
});
</script>

<template>
  <div class="mb-3 rounded-controle bg-fond p-3">
    <p class="mb-2 flex items-center gap-1 text-xs text-texte-3">
      <i class="material-symbols-outlined text-2xl leading-6">gastroenterology</i>
      Transit
    </p>
    <ul v-if="lignes.length" class="flex flex-col gap-1">
      <li v-for="ligne in lignes" :key="ligne.libelle" class="flex flex-wrap items-center gap-x-2 text-sm">
        <span class="text-texte-2">{{ ligne.libelle }} :</span>
        <span class="font-semibold text-texte">{{ ligne.valeur }}</span>
      </li>
    </ul>
    <p v-else class="m-0 text-sm italic text-texte-3">Non renseigné</p>
  </div>
</template>
