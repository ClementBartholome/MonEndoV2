<script setup lang="ts">
import { computed } from 'vue';
import { libelleCourt, TYPES_DOULEUR } from '@/features/douleurs/utils/douleurs';
import { heure } from '@/shared/utils/jours';
import type { CriseDouleur, ZoneDouleur } from '@/features/douleurs/types/douleurs-du-jour';

export interface ActionsZonesDouleur {
  basculer: (typeDouleur: string) => void;
  ajuster: (typeDouleur: string, ecart: number) => void;
}

// « Où as-tu eu mal ? » : présentation seule, les zones et leurs actions viennent du composable useZonesDouleur.
const props = defineProps<{
  zones: ZoneDouleur[];
  crises: CriseDouleur[];
  indisponible: boolean;
  min: number;
  max: number;
  actions: ActionsZonesDouleur;
}>();

const heureDeCrise = (crise: CriseDouleur) => heure(`2000-01-01T${crise.heure}`);

const choix = computed(() => TYPES_DOULEUR.map((t) => {
  const crise = props.crises.find((c) => c.typeDouleur === t.valeur);
  return {
    ...t,
    crise,
    choisi: crise !== undefined || props.zones.some((z) => z.typeDouleur === t.valeur),
  };
}));
</script>

<template>
  <fieldset class="m-0 flex flex-col gap-3 border-0 border-t border-trait p-0 pt-4">
    <legend class="float-left mb-3 flex w-full flex-col gap-1 p-0">
      <span class="text-corps font-semibold text-texte">Où as-tu eu mal ? <span class="font-normal text-texte-3">facultatif</span></span>
      <span class="text-legende leading-normal text-texte-2">Chaque zone touchée rejoint ta page Douleurs : c'est ce qui permet au médecin de voir ta douleur par type.</span>
    </legend>

    <p v-if="indisponible" class="m-0 text-sm text-texte-2">Les zones n'ont pas pu être chargées : tu pourras les préciser plus tard.</p>

    <template v-else>
      <div class="grid grid-cols-2 gap-2">
        <button v-for="t in choix" :key="t.valeur" type="button"
                class="flex min-h-12 flex-col items-center justify-center rounded-controle border-[1.5px] px-2.5 py-1 text-sm"
                :class="t.choisi ? 'choix-actif font-semibold' : 'border-contour bg-surface text-texte'"
                :aria-pressed="t.choisi" :disabled="t.crise !== undefined"
                @click="actions.basculer(t.valeur)">
          <span>{{ t.libelle }}</span>
          <span v-if="t.crise" class="text-xs font-normal">crise à {{ heureDeCrise(t.crise) }}</span>
        </button>
      </div>

      <ul v-if="crises.length || zones.length" class="m-0 flex list-none flex-col rounded-controle border border-trait p-0" aria-label="Douleurs du jour">
        <li v-for="(crise, index) in crises" :key="`crise-${index}`" class="flex min-h-14 items-center gap-3 border-b border-trait px-3 py-2 text-left last:border-b-0">
          <span class="flex grow flex-col">
            <span class="text-corps font-medium text-texte">{{ libelleCourt(crise.typeDouleur) }}</span>
            <span class="text-xs text-texte-2">Crise notée à {{ heureDeCrise(crise) }}</span>
          </span>
          <span class="chiffre text-titre-carte text-texte">{{ crise.intensite }}/10</span>
        </li>
        <li v-for="zone in zones" :key="zone.typeDouleur" class="flex min-h-14 items-center gap-2.5 border-b border-trait px-3 py-2 text-left last:border-b-0">
          <span class="flex grow flex-col">
            <span class="text-corps font-medium text-texte">{{ libelleCourt(zone.typeDouleur) }}</span>
            <span class="text-xs text-texte-2">Modifiable</span>
          </span>
          <button type="button" class="h-11 w-11 shrink-0 rounded-controle border-[1.5px] border-contour bg-surface text-xl text-texte disabled:opacity-50"
                  :aria-label="`Moins fort : ${libelleCourt(zone.typeDouleur)}`" :disabled="zone.intensite <= min"
                  @click="actions.ajuster(zone.typeDouleur, -1)">−</button>
          <span class="chiffre min-w-10 text-center text-titre-carte text-texte" aria-live="polite">{{ zone.intensite }}/10</span>
          <button type="button" class="h-11 w-11 shrink-0 rounded-controle border-[1.5px] border-contour bg-surface text-xl text-texte disabled:opacity-50"
                  :aria-label="`Plus fort : ${libelleCourt(zone.typeDouleur)}`" :disabled="zone.intensite >= max"
                  @click="actions.ajuster(zone.typeDouleur, 1)">+</button>
        </li>
      </ul>
    </template>
  </fieldset>
</template>
