<script setup lang="ts">
import { computed } from 'vue';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import type { ModePeriode } from '@/features/bilan-quotidien/types/historique';
import type { Evolution, Tendances } from '@/features/bilan-quotidien/types/tendances';
import { TEINTE_BLOC, teinteDe } from '@/features/bilan-quotidien/config/teintes';

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

// Période précédente sans bilan : on le dit une fois plutôt que sur chaque carte.
const sansComparaison = computed(() => props.tendances.couverture.bilansPrecedents === 0);

const couverture = computed(() => {
  const { bilans, jours } = props.tendances.couverture;
  return `${bilans} bilan${bilans > 1 ? 's' : ''} sur ${jours} jour${jours > 1 ? 's' : ''}`;
});
</script>

<template>
  <div class="flex w-full flex-col gap-3.5">
    <p class="m-0 flex items-center justify-center gap-2 text-sm text-texte-2">
      <i class="material-symbols-outlined text-lg" aria-hidden="true">event_available</i>
      {{ couverture }}
    </p>

    <!-- Début de semaine ou de mois : rien à analyser tant que le premier bilan n'est pas saisi. -->
    <p v-if="tendances.couverture.bilans === 0" class="m-0 text-center text-texte-2">
      Pas encore de bilan sur cette période : les tendances apparaîtront dès le premier.
    </p>

    <template v-else>
      <Card class="flex w-full flex-col rounded-carte border-0 bg-surface shadow-elevation">
        <CardHeader class="p-4 md:p-6">
          <div class="flex flex-col gap-1">
            <CardTitle class="m-0 flex items-center gap-2 text-titre-carte font-semibold leading-tight tracking-normal text-texte">
              <i class="material-symbols-outlined rounded-controle p-1.5 text-titre-2" :class="[TEINTE_BLOC.tendances.fond, TEINTE_BLOC.tendances.texte]" aria-hidden="true">insights</i>Moyennes
            </CardTitle>
            <p class="m-0 text-left text-sm tracking-normal text-texte-3">
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
                class="rounded-controle bg-surface-2 p-3 text-left"
            >
              <p class="m-0 flex items-center gap-1 text-xs font-medium text-texte-2">
                <i class="material-symbols-outlined text-base" :class="teinteDe(indicateur.cle).texte" aria-hidden="true">{{ indicateur.icone }}</i>
                {{ indicateur.libelle }}
              </p>
              <p class="text-base font-semibold text-texte mt-1">{{ indicateur.valeur ?? '—' }}</p>
              <p v-if="indicateur.valeur === null" class="text-xs text-texte-3 mt-1">Non renseigné</p>
              <p v-else-if="indicateur.evolution" class="text-xs text-texte-2 mt-1 flex items-center gap-1">
                <i class="material-symbols-outlined text-base" aria-hidden="true">
                  {{ presentationEvolution[indicateur.evolution].icone }}
                </i>
                <span>
                  {{ presentationEvolution[indicateur.evolution].libelle }}
                  <template v-if="indicateur.evolution !== 'stable'">({{ indicateur.ecart }})</template>
                </span>
              </p>
              <p v-else-if="!sansComparaison" class="text-xs text-texte-3 mt-1">Pas de comparaison</p>
            </li>
          </ul>
        </CardContent>
      </Card>

      <Card v-if="tendances.reperes.length" class="flex w-full flex-col rounded-carte border-0 bg-surface shadow-elevation">
        <CardHeader class="p-4 md:p-6">
          <div class="flex flex-col gap-1">
            <CardTitle class="m-0 flex items-center gap-2 text-titre-carte font-semibold leading-tight tracking-normal text-texte">
              <i class="material-symbols-outlined rounded-controle p-1.5 text-titre-2" :class="[TEINTE_BLOC.reperes.fond, TEINTE_BLOC.reperes.texte]" aria-hidden="true">flag</i>Repères personnels
            </CardTitle>
            <p class="m-0 text-left text-sm tracking-normal text-texte-3">
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
                class="rounded-controle bg-fond p-3 flex items-center gap-3 text-left"
            >
              <i class="material-symbols-outlined shrink-0 rounded-controle p-1.5 text-xl" :class="[teinteDe(repere.cle).fond, teinteDe(repere.cle).texte]" aria-hidden="true">{{ repere.icone }}</i>
              <div class="min-w-0 flex-1">
                <p class="text-sm font-semibold text-texte">{{ repere.libelle }}</p>
                <p class="text-xs text-texte-3">{{ repere.repere }}</p>
              </div>
              <p class="text-sm text-texte-2 text-right shrink-0">
                <span class="font-semibold text-texte">{{ repere.atteints }}</span> jour{{ repere.atteints > 1 ? 's' : '' }}
                <span class="block text-xs text-texte-3">sur {{ repere.renseignes }} renseigné{{ repere.renseignes > 1 ? 's' : '' }}</span>
              </p>
            </li>
          </ul>
        </CardContent>
      </Card>

      <Card class="flex w-full flex-col rounded-carte border-0 bg-surface shadow-elevation">
        <CardHeader class="p-4 md:p-6">
          <CardTitle class="m-0 flex items-center gap-2 text-titre-carte font-semibold leading-tight tracking-normal text-texte">
            <i class="material-symbols-outlined rounded-controle p-1.5 text-titre-2" :class="[TEINTE_BLOC.cycle.fond, TEINTE_BLOC.cycle.texte]" aria-hidden="true">menstrual_health</i>Douleur et cycle
          </CardTitle>
        </CardHeader>
        <CardContent class="px-4 pb-4 md:px-6 md:pb-6 text-left flex flex-col gap-2">
          <p v-if="!tendances.reglesNotees" class="text-sm text-texte-2">
            Aucun jour de règles noté sur cette période. Note-les dans
            <router-link to="/cycle?onglet=cycles" class="underline">Cycle</router-link>
            pour voir s'ils coïncident avec tes jours de douleur.
          </p>
          <p v-else-if="tendances.observations.length === 0" class="text-sm text-texte-2">
            Pas encore assez de bilans sur cette période pour une observation.
          </p>
          <ul v-else class="flex flex-col gap-2">
            <li
                v-for="observation in tendances.observations"
                :key="observation.cle"
                class="rounded-controle bg-fond p-3 flex items-start gap-3"
            >
              <i class="material-symbols-outlined shrink-0 text-xl text-teinte-regles" aria-hidden="true">{{ observation.icone }}</i>
              <div class="text-left">
                <p class="text-sm text-texte">{{ observation.texte }}</p>
                <p class="text-xs text-texte-3 mt-1">{{ observation.detail }}</p>
              </div>
            </li>
          </ul>
          <p class="text-xs text-texte-3">
            Ces observations décrivent tes bilans ; elles ne sont pas un avis médical.
          </p>
        </CardContent>
      </Card>
    </template>
  </div>
</template>
