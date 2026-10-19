<script setup lang="ts">
import ChoixParmi from '@/features/bilan-quotidien/components/saisie/ChoixParmi.vue';
import OuiNon from '@/features/bilan-quotidien/components/saisie/OuiNon.vue';
import type { ChampReponse, QuestionBilan } from '@/features/bilan-quotidien/config/questions';

/**
 * Affiche une série de questions facultatives décrites par la configuration (`config/questions.ts`) et met à jour les
 * réponses du formulaire. Répondre autre chose que « Oui » à une question vide sa suite (intensité, abondance).
 */
defineProps<{ questions: QuestionBilan[] }>();
const reponses = defineModel<object>({ required: true });

const lues = () => reponses.value as Record<string, unknown>;
const valeur = (champ: ChampReponse) => lues()[champ] ?? null;
// Les conversions de type restent dans le script : un « | » dans le gabarit serait lu comme un filtre.
const booleen = (champ: ChampReponse) => valeur(champ) as boolean | null;
const texte = (champ: ChampReponse) => valeur(champ) as string | null;

function changer(question: QuestionBilan, nouvelle: unknown) {
  const suivantes = { ...lues(), [question.champ]: nouvelle };
  if (question.suite && nouvelle !== true) suivantes[question.suite.champ] = null;
  reponses.value = suivantes;
}

const changerSuite = (champ: ChampReponse, nouvelle: unknown) => {
  reponses.value = { ...lues(), [champ]: nouvelle };
};

</script>

<template>
  <template v-for="question in questions" :key="question.champ">
    <OuiNon v-if="question.genre === 'ouiNon'" :libelle="question.libelle" :aide="question.aide"
            :model-value="booleen(question.champ)" @update:model-value="(v) => changer(question, v)">
      <ChoixParmi v-if="question.suite && valeur(question.champ) === true" :libelle="question.suite.libelle" :options="question.suite.options"
                  title-niveau="p" class="mt-2" :model-value="texte(question.suite.champ)"
                  @update:model-value="(v) => changerSuite(question.suite!.champ, v)"/>
      <p v-if="question.suite?.requise && valeur(question.champ) === true && valeur(question.suite.champ) === null" class="m-0 text-xs italic text-texte-2">
        Choisis une intensité pour pouvoir enregistrer.
      </p>
    </OuiNon>
    <ChoixParmi v-else :libelle="question.libelle" :aide="question.aide" :options="question.options ?? []"
                :model-value="texte(question.champ)" @update:model-value="(v) => changer(question, v)"/>
  </template>
</template>
