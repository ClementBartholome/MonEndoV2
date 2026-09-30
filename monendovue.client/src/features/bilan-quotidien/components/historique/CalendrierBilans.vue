<script setup lang="ts">
import { computed, ref } from 'vue';
import { format, isSameDay, isToday } from 'date-fns';
import { fr } from 'date-fns/locale';
import { echellesCalendrier } from '@/features/bilan-quotidien/config/historique';
import { decalageLundi } from '@/features/bilan-quotidien/utils/historique';
import type { IndicateurCalendrier, JourHistorique, ModePeriode } from '@/features/bilan-quotidien/types/historique';

// Une pastille par jour, colorée selon l'indicateur choisi ; jours de règles marqués, jours sans bilan en pointillés.
const props = defineProps<{
  jours: JourHistorique[];
  mode: ModePeriode;
  jourSelectionne: Date;
}>();

const emit = defineEmits<{ selectionner: [date: Date] }>();

const indicateur = ref<IndicateurCalendrier>('douleur');
const echelle = computed(() => echellesCalendrier[indicateur.value]);

const JOURS_SEMAINE = ['L', 'M', 'M', 'J', 'V', 'S', 'D'];

// En mois, la grille commence un lundi : cases vides avant le 1er.
const casesVides = computed(() => (props.mode === 'mois' && props.jours.length ? decalageLundi(props.jours[0].date) : 0));

const valeurDu = (jour: JourHistorique) => (jour.bilan ? echelle.value.valeur(jour.bilan) : null);

const classesPastille = (jour: JourHistorique) => {
  const valeur = valeurDu(jour);
  if (valeur !== null) return echelle.value.classes[echelle.value.niveau(valeur)];
  if (jour.bilan) return 'bg-surface border border-trait text-texte';
  return 'border border-dashed border-contour text-texte-3';
};

const libelleJour = (jour: JourHistorique) => {
  const morceaux = [format(jour.date, 'EEEE d MMMM', { locale: fr })];
  const valeur = valeurDu(jour);
  if (jour.aVenir) morceaux.push('à venir');
  else if (!jour.bilan) morceaux.push('pas de bilan');
  else if (valeur === null) morceaux.push(echelle.value.nonRenseigne);
  else morceaux.push(echelle.value.description(valeur));
  if (jour.regles) morceaux.push('règles');
  return morceaux.join(', ');
};
</script>

<template>
  <div class="w-full flex flex-col gap-3">
    <div>
      <p id="calendrier-indicateur" class="sr-only">Couleur des jours selon</p>
      <div class="grid grid-cols-2 gap-1 rounded-controle bg-surface-2 p-1" role="group" aria-labelledby="calendrier-indicateur">
        <button
            v-for="(config, cle) in echellesCalendrier"
            :key="cle"
            type="button"
            class="min-h-11 rounded-controle px-3 text-sm"
            :class="indicateur === cle ? 'bg-surface font-semibold text-texte shadow-elevation' : 'text-texte-2'"
            :aria-pressed="indicateur === cle"
            @click="indicateur = cle"
        >
          {{ config.libelle }}
        </button>
      </div>
    </div>

    <div class="grid grid-cols-7 gap-1" role="group" aria-label="Bilans de la période">
      <div
          v-for="(initiale, index) in JOURS_SEMAINE"
          :key="`entete-${index}`"
          class="text-center text-xs font-medium text-texte-3"
          aria-hidden="true"
      >
        {{ initiale }}
      </div>
      <div v-for="n in casesVides" :key="`vide-${n}`" aria-hidden="true"/>
      <button
          v-for="jour in jours"
          :key="jour.cle"
          type="button"
          class="jour relative mx-auto w-full max-w-12 aspect-square rounded-full flex items-center justify-center text-sm transition-transform disabled:opacity-40"
          :class="[
            classesPastille(jour),
            isSameDay(jour.date, jourSelectionne) ? 'ring-2 ring-texte ring-offset-2 ring-offset-surface' : '',
            isToday(jour.date) ? 'font-bold underline underline-offset-2' : 'font-medium',
          ]"
          :disabled="jour.aVenir"
          :aria-label="libelleJour(jour)"
          :aria-pressed="isSameDay(jour.date, jourSelectionne)"
          @click="emit('selectionner', jour.date)"
      >
        {{ jour.date.getDate() }}
        <span
            v-if="jour.regles"
            class="absolute -right-0.5 -top-0.5 flex h-4 w-4 items-center justify-center rounded-full bg-surface"
            aria-hidden="true"
        >
          <i class="material-symbols-outlined regles text-teinte-regles">water_drop</i>
        </span>
      </button>
    </div>

    <ul class="flex flex-wrap items-center justify-center gap-x-4 gap-y-2 text-xs text-texte-2" aria-label="Légende">
      <li class="flex items-center gap-1">
        <span class="text-texte-3">{{ echelle.libelle }}</span>
        <span class="flex items-center gap-0.5">
          <span v-for="(classes, niveau) in echelle.classes" :key="niveau" class="w-4 h-4 rounded-full" :class="classes"/>
        </span>
        <span class="sr-only">de {{ echelle.bornes[0] }} à {{ echelle.bornes[1] }}</span>
        <span aria-hidden="true">{{ echelle.bornes[0] }} → {{ echelle.bornes[1] }}</span>
      </li>
      <li class="flex items-center gap-1">
        <i class="material-symbols-outlined regles text-teinte-regles" aria-hidden="true">water_drop</i>Règles
      </li>
      <li class="flex items-center gap-1">
        <span class="w-4 h-4 rounded-full border border-dashed border-contour" aria-hidden="true"/>Sans bilan
      </li>
    </ul>
  </div>
</template>

<style scoped>
.regles {
  font-size: 0.75rem;
  font-variation-settings: 'FILL' 1, 'wght' 400, 'GRAD' 0, 'opsz' 20;
}
</style>
