<script setup lang="ts">
import { Button } from '@/shared/components/ui/button';
import { echelleBristol, intensitesTransit } from '@/features/bilan-quotidien/config/transit';
import type { IntensiteTransit, TransitBilan } from '@/features/bilan-quotidien/types/bilan-quotidien';

// Étape « Transit » du bilan quotidien : toutes les questions sont facultatives.
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
  <div class="flex flex-col gap-6">
    <h2 class="text-2xl font-bold flex items-center justify-center">
      <i class="material-symbols-outlined mr-2">gastroenterology</i>Transit
    </h2>
    <p class="text-sm text-paragraph text-center -mt-4">
      Toutes les questions sont facultatives.
    </p>

    <!-- Selles -->
    <section class="flex flex-col gap-3">
      <h3 class="text-lg font-semibold text-headline">Des selles aujourd'hui ?</h3>
      <div class="grid grid-cols-2 gap-3">
        <Button
            v-for="reponse in reponses"
            :key="`selles-${reponse.libelle}`"
            type="button"
            class="h-12"
            :variant="transit.selles === reponse.valeur ? 'selected' : 'outline'"
            :aria-pressed="transit.selles === reponse.valeur"
            @click="repondreSelles(reponse.valeur)"
        >
          {{ reponse.libelle }}
        </Button>
      </div>

      <div v-if="transit.selles" class="flex flex-col gap-2">
        <p class="text-sm font-medium text-headline">Quel aspect ? <span class="font-normal text-paragraph">(échelle de Bristol)</span></p>
        <button
            v-for="bristol in echelleBristol"
            :key="bristol.type"
            type="button"
            :aria-pressed="transit.typeBristol === bristol.type"
            :class="[
              'flex items-start gap-3 p-3 rounded-xl border-2 text-left transition-all duration-200',
              transit.typeBristol === bristol.type
                ? 'border-button bg-button/15 shadow-sm'
                : 'border-gray-200 bg-white hover:border-button/50'
            ]"
            @click="choisirBristol(bristol.type)"
        >
          <span
              :class="[
                'shrink-0 w-9 h-9 rounded-full flex items-center justify-center font-bold',
                transit.typeBristol === bristol.type ? 'bg-button text-white' : 'bg-gray-100 text-headline'
              ]"
          >
            {{ bristol.type }}
          </span>
          <span class="flex flex-col gap-0.5">
            <span class="font-semibold text-headline">{{ bristol.titre }}</span>
            <span class="text-sm text-paragraph">{{ bristol.description }}</span>
            <span class="text-xs text-paragraph/70 italic">{{ bristol.tendance }}</span>
          </span>
        </button>
        <button
            type="button"
            :aria-pressed="transit.typeBristol === null"
            :class="[
              'p-3 rounded-xl border-2 text-sm transition-all duration-200',
              transit.typeBristol === null ? 'border-button bg-button/15' : 'border-gray-200 bg-white hover:border-button/50'
            ]"
            @click="choisirBristol(null)"
        >
          Je préfère ne pas renseigner l'aspect
        </button>
      </div>
    </section>

    <!-- Crampes et ballonnements -->
    <section v-for="symptome in symptomes" :key="symptome.cle" class="flex flex-col gap-3">
      <h3 class="text-lg font-semibold text-headline flex items-center gap-2">
        <i class="material-symbols-outlined text-button">{{ symptome.icone }}</i>{{ symptome.question }}
      </h3>
      <div class="grid grid-cols-2 gap-3">
        <Button
            v-for="reponse in reponses"
            :key="`${symptome.cle}-${reponse.libelle}`"
            type="button"
            class="h-12"
            :variant="transit[symptome.cle] === reponse.valeur ? 'selected' : 'outline'"
            :aria-pressed="transit[symptome.cle] === reponse.valeur"
            @click="repondreSymptome(symptome.cle, symptome.intensite, reponse.valeur)"
        >
          {{ reponse.libelle }}
        </Button>
      </div>
      <div v-if="transit[symptome.cle]" class="flex flex-col gap-2">
        <p class="text-sm font-medium text-headline">Intensité</p>
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
        <p v-if="!transit[symptome.intensite]" class="text-xs text-paragraph italic">
          Choisis une intensité pour continuer.
        </p>
      </div>
    </section>
  </div>
</template>
