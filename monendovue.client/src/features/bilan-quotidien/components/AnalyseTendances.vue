<script setup lang="ts">
import { computed } from 'vue';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import type { ModePeriode } from '@/features/bilan-quotidien/types/historique';
import type { Evolution, Tendances } from '@/features/bilan-quotidien/types/tendances';

// Tendances neutres d'une période : moyennes, évolutions sans jugement, repères personnels, observations factuelles.
const props = defineProps<{
  tendances: Tendances;
  mode: ModePeriode;
}>();

const periodePrecedente = computed(() => (props.mode === 'mois'
  ? { comparee: 'au mois précédent', sansBilan: 'aucun bilan le mois précédent' }
  : { comparee: 'à la semaine précédente', sansBilan: 'aucun bilan la semaine précédente' }));

const presentationEvolution: Record<Evolution, { icone: string; libelle: string }> = {
  hausse: { icone: 'trending_up', libelle: 'en hausse' },
  baisse: { icone: 'trending_down', libelle: 'en baisse' },
  stable: { icone: 'trending_flat', libelle: 'stable' },
};

// Aucune comparaison possible (période précédente sans bilan) : on le dit une fois plutôt que sur chaque carte.
const sansComparaison = computed(() => props.tendances.indicateurs.every((i) => i.evolution === null));

const couverture = computed(() => {
  const { bilans, jours } = props.tendances.couverture;
  return `${bilans} bilan${bilans > 1 ? 's' : ''} sur ${jours} jour${jours > 1 ? 's' : ''}`;
});
</script>

<template>
  <div class="w-full">
    <p class="mt-4 flex items-center justify-center gap-2 text-sm text-paragraph">
      <i class="material-symbols-outlined text-lg" aria-hidden="true">event_available</i>
      {{ couverture }}
    </p>

    <p v-if="tendances.indicateurs.length === 0" class="mt-4 text-paragraph text-center">
      Aucun bilan sur cette période ni la précédente.
    </p>

    <template v-else>
      <Card class="container !mx-0 mt-4 w-full bg-clearer rounded-3xl shadow-xl flex flex-col">
        <CardHeader class="p-4 md:p-6">
          <div class="flex flex-col gap-1">
            <CardTitle class="m-0 text-lg leading-tight flex items-center gap-2">
              <i class="material-symbols-outlined" aria-hidden="true">insights</i>Tendances
            </CardTitle>
            <p class="text-sm text-muted-foreground text-left">
              <template v-if="sansComparaison">Moyennes de la période ({{ periodePrecedente.sansBilan }} pour comparer).</template>
              <template v-else>Moyennes de la période, comparées {{ periodePrecedente.comparee }}.</template>
            </p>
          </div>
        </CardHeader>
        <CardContent class="px-4 pb-4 md:px-6 md:pb-6">
          <ul class="grid grid-cols-2 md:grid-cols-3 gap-2">
            <li
                v-for="indicateur in tendances.indicateurs"
                :key="indicateur.cle"
                class="bg-white rounded-xl border border-gray-100 p-3 text-left"
            >
              <p class="text-xs text-muted-foreground flex items-center gap-1">
                <i class="material-symbols-outlined text-base" aria-hidden="true">{{ indicateur.icone }}</i>
                {{ indicateur.libelle }}
              </p>
              <p class="text-base font-semibold text-headline mt-1">{{ indicateur.valeur ?? '—' }}</p>
              <p v-if="indicateur.valeur === null" class="text-xs text-muted-foreground mt-1">Non renseigné</p>
              <p v-else-if="indicateur.evolution" class="text-xs text-paragraph mt-1 flex items-center gap-1">
                <i class="material-symbols-outlined text-base" aria-hidden="true">
                  {{ presentationEvolution[indicateur.evolution].icone }}
                </i>
                <span>
                  {{ presentationEvolution[indicateur.evolution].libelle }}
                  <template v-if="indicateur.evolution !== 'stable'">({{ indicateur.ecart }})</template>
                </span>
              </p>
              <p v-else-if="!sansComparaison" class="text-xs text-muted-foreground mt-1">Pas de comparaison</p>
            </li>
          </ul>
        </CardContent>
      </Card>

      <Card v-if="tendances.reperes.length" class="container !mx-0 mt-4 w-full bg-clearer rounded-3xl shadow-xl flex flex-col">
        <CardHeader class="p-4 md:p-6">
          <div class="flex flex-col gap-1">
            <CardTitle class="m-0 text-lg leading-tight flex items-center gap-2">
              <i class="material-symbols-outlined" aria-hidden="true">flag</i>Repères personnels
            </CardTitle>
            <p class="text-sm text-muted-foreground text-left">
              Tes repères se règlent dans
              <router-link to="/parametres" class="underline">Paramètres</router-link>.
            </p>
          </div>
        </CardHeader>
        <CardContent class="px-4 pb-4 md:px-6 md:pb-6">
          <ul class="flex flex-col gap-2">
            <li
                v-for="repere in tendances.reperes"
                :key="repere.cle"
                class="bg-white rounded-xl border border-gray-100 p-3 flex items-center gap-3 text-left"
            >
              <i class="material-symbols-outlined text-xl text-headline shrink-0" aria-hidden="true">{{ repere.icone }}</i>
              <div class="min-w-0 flex-1">
                <p class="text-sm font-semibold text-headline">{{ repere.libelle }}</p>
                <p class="text-xs text-muted-foreground">{{ repere.repere }}</p>
              </div>
              <p class="text-sm text-paragraph text-right shrink-0">
                <span class="font-semibold text-headline">{{ repere.atteints }}</span> jour{{ repere.atteints > 1 ? 's' : '' }}
                <span class="block text-xs text-muted-foreground">sur {{ repere.renseignes }} renseigné{{ repere.renseignes > 1 ? 's' : '' }}</span>
              </p>
            </li>
          </ul>
        </CardContent>
      </Card>

      <Card class="container !mx-0 mt-4 w-full bg-clearer rounded-3xl shadow-xl flex flex-col">
        <CardHeader class="p-4 md:p-6">
          <CardTitle class="m-0 text-lg leading-tight flex items-center gap-2">
            <i class="material-symbols-outlined" aria-hidden="true">menstrual_health</i>Douleur et cycle
          </CardTitle>
        </CardHeader>
        <CardContent class="px-4 pb-4 md:px-6 md:pb-6 text-left flex flex-col gap-2">
          <p v-if="!tendances.reglesNotees" class="text-sm text-paragraph">
            Aucun jour de règles noté sur cette période. Note-les dans
            <router-link to="/cycle?onglet=cycles" class="underline">Cycle</router-link>
            pour voir s'ils coïncident avec tes jours de douleur.
          </p>
          <p v-else-if="tendances.observations.length === 0" class="text-sm text-paragraph">
            Pas encore assez de bilans sur cette période pour une observation.
          </p>
          <ul v-else class="flex flex-col gap-2">
            <li
                v-for="observation in tendances.observations"
                :key="observation.cle"
                class="bg-white rounded-xl border border-gray-100 p-3 flex items-start gap-3"
            >
              <i class="material-symbols-outlined text-xl text-headline shrink-0" aria-hidden="true">{{ observation.icone }}</i>
              <div class="text-left">
                <p class="text-sm text-headline">{{ observation.texte }}</p>
                <p class="text-xs text-muted-foreground mt-1">{{ observation.detail }}</p>
              </div>
            </li>
          </ul>
          <p class="text-xs text-muted-foreground">
            Ces observations décrivent tes bilans ; elles ne sont pas un avis médical.
          </p>
        </CardContent>
      </Card>
    </template>
  </div>
</template>
