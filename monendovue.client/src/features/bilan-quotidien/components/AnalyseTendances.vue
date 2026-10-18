<script setup lang="ts">
import { computed } from 'vue';
import type { ModePeriode } from '@/features/bilan-quotidien/types/historique';
import type { Evolution, Tendances } from '@/features/bilan-quotidien/types/tendances';
import { teinteDe } from '@/features/bilan-quotidien/config/teintes';

// Tendances neutres d'une période : moyennes, évolutions sans jugement, repères personnels, observations factuelles.
// Présentées comme les pages d'un carnet : des sections titrées, des lignes séparées par des filets, les chiffres écrits.
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

// Période précédente sans bilan : on le dit une fois plutôt que sur chaque ligne.
const sansComparaison = computed(() => props.tendances.couverture.bilansPrecedents === 0);

const couverture = computed(() => {
  const { bilans, jours } = props.tendances.couverture;
  return `${bilans} bilan${bilans > 1 ? 's' : ''} sur ${jours} jour${jours > 1 ? 's' : ''}`;
});
</script>

<template>
  <div class="flex w-full flex-col gap-5">
    <p class="m-0 flex items-center justify-center gap-2 text-sm text-texte-2">
      <i class="material-symbols-outlined text-lg" aria-hidden="true">event_available</i>
      {{ couverture }}
    </p>

    <!-- Début de semaine ou de mois : rien à analyser tant que le premier bilan n'est pas saisi. -->
    <p v-if="tendances.couverture.bilans === 0" class="m-0 text-center text-texte-2">
      Pas encore de bilan sur cette période : les tendances apparaîtront dès le premier.
    </p>

    <template v-else>
      <section aria-labelledby="titre-moyennes" class="flex flex-col text-left">
        <h2 id="titre-moyennes" class="m-0 text-legende font-semibold uppercase tracking-wider text-texte-2">Moyennes</h2>
        <p class="m-0 mt-1 text-sm text-texte-3">
          <template v-if="sansComparaison">Moyennes de la période ({{ periodePrecedente.sansBilan }} pour comparer).</template>
          <template v-else>Moyennes de la période, comparées {{ periodePrecedente.comparee }}.</template>
        </p>
        <ul class="m-0 mt-2 flex list-none flex-col p-0">
          <li v-for="indicateur in tendances.indicateurs" :key="indicateur.cle"
              class="flex min-h-14 items-center gap-3 border-b border-trait py-2.5 text-left last:border-b-0">
            <i class="material-symbols-outlined text-xl" :class="teinteDe(indicateur.cle).texte" aria-hidden="true">{{ indicateur.icone }}</i>
            <span class="min-w-0 grow text-corps text-texte">{{ indicateur.libelle }}</span>
            <span class="flex flex-col items-end">
              <span class="chiffre text-lg font-semibold text-texte">{{ indicateur.valeur ?? '—' }}</span>
              <span v-if="indicateur.valeur === null" class="text-legende text-texte-3">Non renseigné</span>
              <span v-else-if="indicateur.evolution" class="flex items-center gap-1 text-legende text-texte-2">
                <i class="material-symbols-outlined text-base" aria-hidden="true">{{ presentationEvolution[indicateur.evolution].icone }}</i>
                {{ presentationEvolution[indicateur.evolution].libelle }}<template v-if="indicateur.evolution !== 'stable'"> ({{ indicateur.ecart }})</template>
              </span>
              <span v-else-if="!sansComparaison" class="text-legende text-texte-3">Pas de comparaison</span>
            </span>
          </li>
        </ul>
      </section>

      <section v-if="tendances.reperes.length" aria-labelledby="titre-reperes" class="flex flex-col border-t border-trait pt-4 text-left">
        <h2 id="titre-reperes" class="m-0 text-legende font-semibold uppercase tracking-wider text-texte-2">Repères personnels</h2>
        <p class="m-0 mt-1 text-sm text-texte-3">
          Tes repères se règlent dans
          <router-link to="/parametres" class="underline">Paramètres</router-link>.
        </p>
        <ul class="m-0 mt-2 flex list-none flex-col p-0">
          <li v-for="repere in tendances.reperes" :key="repere.cle"
              class="flex min-h-14 items-center gap-3 border-b border-trait py-2.5 text-left last:border-b-0">
            <i class="material-symbols-outlined text-xl" :class="teinteDe(repere.cle).texte" aria-hidden="true">{{ repere.icone }}</i>
            <span class="flex min-w-0 grow flex-col">
              <span class="text-corps text-texte">{{ repere.libelle }}</span>
              <span class="text-legende text-texte-3">{{ repere.repere }}</span>
            </span>
            <span class="flex shrink-0 flex-col items-end">
              <span class="text-sm text-texte-2"><span class="chiffre text-lg font-semibold text-texte">{{ repere.atteints }}</span> jour{{ repere.atteints > 1 ? 's' : '' }}</span>
              <span class="text-legende text-texte-3">sur {{ repere.renseignes }} renseigné{{ repere.renseignes > 1 ? 's' : '' }}</span>
            </span>
          </li>
        </ul>
      </section>

      <section aria-labelledby="titre-douleur-cycle" class="flex flex-col gap-2 border-t border-trait pt-4 text-left">
        <h2 id="titre-douleur-cycle" class="m-0 text-legende font-semibold uppercase tracking-wider text-texte-2">Douleur et cycle</h2>
        <p v-if="!tendances.reglesNotees" class="m-0 text-sm text-texte-2">
          Aucun jour de règles noté sur cette période. Note-les dans
          <router-link to="/cycle?onglet=cycles" class="underline">Cycle</router-link>
          pour voir s'ils coïncident avec tes jours de douleur.
        </p>
        <p v-else-if="tendances.observations.length === 0" class="m-0 text-sm text-texte-2">
          Pas encore assez de bilans sur cette période pour une observation.
        </p>
        <ul v-else class="m-0 flex list-none flex-col p-0">
          <li v-for="observation in tendances.observations" :key="observation.cle"
              class="flex items-start gap-3 border-b border-trait py-2.5 text-left last:border-b-0">
            <i class="material-symbols-outlined shrink-0 text-xl text-teinte-regles" aria-hidden="true">{{ observation.icone }}</i>
            <span class="flex flex-col">
              <span class="text-corps text-texte">{{ observation.texte }}</span>
              <span class="mt-0.5 text-legende text-texte-3">{{ observation.detail }}</span>
            </span>
          </li>
        </ul>
        <p class="m-0 text-legende text-texte-3">
          Ces observations décrivent tes bilans ; elles ne sont pas un avis médical.
        </p>
      </section>
    </template>
  </div>
</template>
