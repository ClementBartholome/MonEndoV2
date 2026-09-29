<script setup lang="ts">
import { Button } from '@/shared/components/ui/button';
import { echelleBristol, intensitesTransit } from '@/features/bilan-quotidien/config/transit';
import type { IntensiteTransit, TransitBilan } from '@/features/bilan-quotidien/types/bilan-quotidien';

// Catégorie « Transit » du bilan quotidien : toutes les questions sont facultatives.
// Recliquer sur la réponse sélectionnée la retire (retour à « non renseigné »).
const transit = defineModel<TransitBilan>({ required: true });

type Symptome = 'crampesEstomac' | 'ballonnements';
type ChampIntensite = 'intensiteCrampes' | 'intensiteBallonnements';

const symptomes: { cle: Symptome; intensite: ChampIntensite; question: string; icone: string }[] = [
  { cle: 'crampesEstomac', intensite: 'intensiteCrampes', question: "Des crampes d'estomac ?", icone: 'pulse_alert' },
  { cle: 'ballonnements', intensite: 'intensiteBallonnements', question: 'Un ventre gonflé, des ballonnements ?', icone: 'bubble_chart' },
];

const reponses = [
  { valeur: true, libelle: 'Oui' },
  { valeur: false, libelle: 'Non' },
];

const repondreSelles = (valeur: boolean) => {
  const selles = transit.value.selles === valeur ? null : valeur;
  transit.value = { ...transit.value, selles, typeBristol: selles ? transit.value.typeBristol : null };
};

const choisirBristol = (type: number | null) => {
  transit.value = { ...transit.value, typeBristol: transit.value.typeBristol === type ? null : type };
};

const repondreSymptome = (cle: Symptome, champ: ChampIntensite, valeur: boolean) => {
  const present = transit.value[cle] === valeur ? null : valeur;
  transit.value = { ...transit.value, [cle]: present, [champ]: present ? transit.value[champ] : null };
};

const choisirIntensite = (champ: ChampIntensite, intensite: IntensiteTransit) => {
  transit.value = { ...transit.value, [champ]: intensite };
};
</script>

<template>
  <div class="flex flex-col gap-4">
    <p class="font-semibold text-texte flex items-center gap-2">
      <i class="material-symbols-outlined text-teinte-bilan" aria-hidden="true">gastroenterology</i>Transit
    </p>

    <!-- Selles -->
    <section class="flex flex-col gap-2">
      <h3 class="text-sm font-medium text-texte">Des selles ?</h3>
      <div class="grid grid-cols-2 gap-2">
        <Button
            v-for="reponse in reponses"
            :key="`selles-${reponse.libelle}`"
            type="button"
            class="h-11"
            :variant="transit.selles === reponse.valeur ? 'selected' : 'outline'"
            :aria-pressed="transit.selles === reponse.valeur"
            @click="repondreSelles(reponse.valeur)"
        >
          {{ reponse.libelle }}
        </Button>
      </div>

      <div v-if="transit.selles" class="flex flex-col gap-1.5">
        <p class="text-sm font-medium text-texte">Quel aspect ? <span class="font-normal text-texte-2">(échelle de Bristol)</span></p>
        <!-- Lignes compactes : la description n'apparaît que pour le type choisi. -->
        <button
            v-for="bristol in echelleBristol"
            :key="bristol.type"
            type="button"
            :aria-pressed="transit.typeBristol === bristol.type"
            :class="[
              'flex items-start gap-2.5 min-h-[44px] px-2.5 py-2 rounded-controle border-[1.5px] text-left transition-colors',
              transit.typeBristol === bristol.type
                ? 'border-texte bg-surface-2'
                : 'border-trait bg-surface hover:border-contour'
            ]"
            @click="choisirBristol(bristol.type)"
        >
          <span
              :class="[
                'shrink-0 w-7 h-7 rounded-controle flex items-center justify-center text-sm font-semibold',
                transit.typeBristol === bristol.type ? 'bg-texte text-fond' : 'bg-surface-2 text-texte'
              ]"
          >
            {{ bristol.type }}
          </span>
          <span class="flex flex-col min-w-0 flex-1 pt-0.5">
            <span class="flex flex-col">
              <span class="text-sm font-semibold text-texte">{{ bristol.titre }}</span>
              <span class="text-xs text-texte-3 italic">{{ bristol.tendance }}</span>
            </span>
            <span v-if="transit.typeBristol === bristol.type" class="text-xs text-texte-2 mt-0.5">{{ bristol.description }}</span>
          </span>
        </button>
        <button
            type="button"
            :aria-pressed="transit.typeBristol === null"
            :class="[
              'min-h-[44px] px-3 rounded-controle border-[1.5px] text-sm transition-colors',
              transit.typeBristol === null ? 'border-texte bg-texte font-medium text-fond' : 'border-trait bg-surface text-texte hover:border-contour'
            ]"
            @click="choisirBristol(null)"
        >
          Je préfère ne pas préciser l'aspect
        </button>
      </div>
    </section>

    <!-- Crampes et ballonnements -->
    <section v-for="symptome in symptomes" :key="symptome.cle" class="flex flex-col gap-2">
      <h3 class="text-sm font-medium text-texte flex items-center gap-2">
        <i class="material-symbols-outlined text-teinte-bilan text-xl" aria-hidden="true">{{ symptome.icone }}</i>{{ symptome.question }}
      </h3>
      <div class="grid grid-cols-2 gap-2">
        <Button
            v-for="reponse in reponses"
            :key="`${symptome.cle}-${reponse.libelle}`"
            type="button"
            class="h-11"
            :variant="transit[symptome.cle] === reponse.valeur ? 'selected' : 'outline'"
            :aria-pressed="transit[symptome.cle] === reponse.valeur"
            @click="repondreSymptome(symptome.cle, symptome.intensite, reponse.valeur)"
        >
          {{ reponse.libelle }}
        </Button>
      </div>
      <div v-if="transit[symptome.cle]" class="flex flex-col gap-2">
        <p class="text-sm font-medium text-texte">Intensité</p>
        <div class="grid grid-cols-3 gap-2">
          <Button
              v-for="intensite in intensitesTransit"
              :key="`${symptome.cle}-${intensite}`"
              type="button"
              class="h-11"
              :variant="transit[symptome.intensite] === intensite ? 'selected' : 'outline'"
              :aria-pressed="transit[symptome.intensite] === intensite"
              @click="choisirIntensite(symptome.intensite, intensite)"
          >
            {{ intensite }}
          </Button>
        </div>
        <p v-if="!transit[symptome.intensite]" class="text-xs text-texte-2 italic">
          Choisis une intensité pour pouvoir enregistrer.
        </p>
      </div>
    </section>
  </div>
</template>
