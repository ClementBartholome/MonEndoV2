<template>
  <main class="mx-auto flex w-full max-w-xl flex-col gap-5 px-5 pb-40 pt-24 lg:pb-12 lg:pt-10">
    <header class="flex items-center justify-between gap-3">
      <h1 class="m-0 text-left text-titre-page font-semibold tracking-normal text-texte">Agenda</h1>
      <button v-if="etat === 'pret'" type="button" aria-label="Actualiser" class="-mr-2 flex h-11 w-11 items-center justify-center rounded-full text-texte hover:bg-surface-2" @click="actualiser">
        <i class="material-symbols-outlined" aria-hidden="true">refresh</i>
      </button>
    </header>

    <div v-if="etat === 'chargement'" class="flex flex-col gap-3" aria-busy="true" aria-label="Chargement de l'agenda">
      <Skeleton class="h-11 w-full rounded-controle"/>
      <Skeleton class="h-24 w-full rounded-carte"/>
      <Skeleton class="h-24 w-full rounded-carte"/>
    </div>

    <template v-else-if="etat === 'pret'">
      <div role="group" aria-label="Vue" class="grid grid-cols-2 gap-2">
        <button v-for="option in VUES" :key="option.valeur" type="button" :aria-pressed="vue === option.valeur"
                class="min-h-11 rounded-controle border-[1.5px] text-sm"
                :class="vue === option.valeur ? 'border-texte bg-texte font-semibold text-fond' : 'border-contour bg-surface font-medium text-texte'"
                @click="changerVue(option.valeur)">{{ option.libelle }}</button>
      </div>

      <template v-if="vue === 'liste'">
        <EtatAgenda v-if="groupes.length === 0" type="aucun" @reessayer="actualiser"/>
        <section v-for="groupe in groupes" :key="groupe.cle" class="flex flex-col gap-2" :aria-labelledby="`groupe-${groupe.cle}`">
          <h2 :id="`groupe-${groupe.cle}`" class="m-0 text-left text-legende font-semibold uppercase tracking-wider text-texte-3">{{ groupe.titre }}</h2>
          <ul class="m-0 flex list-none flex-col divide-y divide-trait overflow-hidden rounded-carte bg-surface p-0 shadow-elevation">
            <li v-for="evenement in groupe.evenements" :key="evenement.id" class="flex flex-col">
              <LigneRendezVous :evenement="evenement" :avec-jour="groupe.cle === 'semaine' || groupe.cle === 'plus-tard'"
                               :avec-mois="groupe.cle === 'plus-tard'" :puce="evenement.id === prochain?.id ? dansNJours(evenement, maintenant) : null"
                               @ouvrir="selection = evenement"/>
              <div v-if="evenement.id === prochain?.id" class="px-4 pb-3.5">
                <button type="button" :disabled="preparationEnCours"
                        class="inline-flex min-h-11 w-full items-center justify-center gap-2 rounded-controle bg-button text-sm font-semibold text-texte disabled:opacity-60"
                        @click="preparer(evenement)">
                  <i class="material-symbols-outlined" aria-hidden="true">picture_as_pdf</i>Préparer ce rendez-vous
                </button>
              </div>
            </li>
          </ul>
        </section>

        <p v-if="erreurSuite" role="alert" class="m-0 text-left text-sm text-danger">Les rendez-vous suivants n'ont pas pu être chargés. Réessaie dans un instant.</p>
        <button v-if="groupes.length > 0 && peutVoirPlusLoin" type="button" :disabled="chargementSuite"
                class="min-h-12 rounded-controle border-[1.5px] border-contour bg-surface px-4 text-sm font-medium text-texte disabled:opacity-60"
                @click="voirPlusLoin">{{ chargementSuite ? 'Chargement…' : 'Voir plus loin' }}</button>
      </template>

      <template v-else>
        <CalendrierMois :mois="moisAffiche" :evenements="evenementsMois" :jour-choisi="jourChoisi" :aujourdhui="maintenant"
                        @mois="changerMois" @choisir="choisirJour"/>
        <p v-if="erreurMois" role="alert" class="m-0 text-left text-sm text-danger">Ce mois n'a pas pu être chargé. Touche « Actualiser » pour réessayer.</p>
        <section class="flex flex-col gap-2" aria-labelledby="titre-jour-choisi">
          <h2 id="titre-jour-choisi" class="m-0 text-left text-legende font-semibold uppercase tracking-wider text-texte-3">{{ titreJour }}</h2>
          <ul v-if="evenementsDuJour.length > 0" class="m-0 flex list-none flex-col divide-y divide-trait overflow-hidden rounded-carte bg-surface p-0 shadow-elevation">
            <li v-for="evenement in evenementsDuJour" :key="evenement.id">
              <LigneRendezVous :evenement="evenement" :avec-jour="false" :avec-mois="false" @ouvrir="selection = evenement"/>
            </li>
          </ul>
          <p v-else-if="!chargementMois" class="m-0 text-left text-sm text-texte-2">Aucun rendez-vous ce jour-là.</p>
        </section>
      </template>
    </template>

    <EtatAgenda v-else :type="typeEtat" @reessayer="charger"/>

    <FeuilleRendezVous v-model:open="feuilleOuverte" :evenement="selection" :a-venir="selection ? estAVenir(selection, maintenant) : false"
                       :preparation-en-cours="preparationEnCours" @preparer="selection && preparer(selection)"/>
  </main>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { Skeleton } from '@/shared/components/ui/skeleton';
import CalendrierMois from '../components/CalendrierMois.vue';
import EtatAgenda, { type TypeEtat } from '../components/EtatAgenda.vue';
import FeuilleRendezVous from '../components/FeuilleRendezVous.vue';
import LigneRendezVous from '../components/LigneRendezVous.vue';
import { useAgenda, type VueAgenda } from '../composables/useAgenda';
import { dansNJours, estAVenir, grouperParJour, jourComplet } from '../utils/rendezVous';

const maintenant = new Date();
const {
  etat, statut, vue, evenements, chargementSuite, erreurSuite, prochain, peutVoirPlusLoin,
  moisAffiche, evenementsMois, chargementMois, erreurMois, jourChoisi, evenementsDuJour, selection, preparationEnCours,
  charger, voirPlusLoin, changerVue, changerMois, choisirJour, actualiser, preparer,
} = useAgenda({ maintenant: () => maintenant });

const VUES: { valeur: VueAgenda; libelle: string }[] = [
  { valeur: 'liste', libelle: 'Liste' },
  { valeur: 'mois', libelle: 'Mois' },
];

const groupes = computed(() => grouperParJour(evenements.value, maintenant));

const titreJour = computed(() => {
  const jour = jourComplet(jourChoisi.value);
  return jour.charAt(0).toUpperCase() + jour.slice(1);
});

/** Panneau du rendez-vous ouvert : ouvert tant qu'un rendez-vous est sélectionné. */
const feuilleOuverte = computed({
  get: () => selection.value !== null,
  set: (ouvert: boolean) => { if (!ouvert) selection.value = null; },
});

const typeEtat = computed<TypeEtat>(() => {
  if (etat.value === 'non-lie') return statut.value?.disponible === false ? 'non-disponible' : 'non-lie';
  if (etat.value === 'sans-calendrier') return 'sans-calendrier';
  return 'indisponible';
});

onMounted(charger);
</script>
