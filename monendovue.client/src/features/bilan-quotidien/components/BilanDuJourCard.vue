<script setup lang="ts">
import { teinteDe } from '@/features/bilan-quotidien/config/teintes';
import { computed } from 'vue';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import HumeurResume from '@/features/bilan-quotidien/components/HumeurResume.vue';
import TransitRecap from '@/features/bilan-quotidien/components/TransitRecap.vue';
import { consommationsAlimentaires } from '@/features/bilan-quotidien/config/saisie';
import { stressDuBilan } from '@/features/bilan-quotidien/utils/mesures';
import type { BilanQuotidien } from '@/features/bilan-quotidien/types/bilan-quotidien';

// Détail d'un jour : les mesures non renseignées s'affichent « — », jamais 0.
const props = defineProps<{
  titre: string;
  bilan: BilanQuotidien | null;
  /** Jour pas encore arrivé : pas de saisie possible. */
  aVenir: boolean;
}>();

const emit = defineEmits<{
  modifier: [];
  remplir: [];
}>();

const NON_RENSEIGNE = '—';

const avecUnite = (valeur: number | null | undefined, unite: string, decimales = 0) =>
  valeur === null || valeur === undefined
    ? NON_RENSEIGNE
    : `${valeur.toLocaleString('fr-FR', { maximumFractionDigits: decimales })}${unite}`;

const tuiles = computed(() => {
  const b = props.bilan;
  if (!b) return [];
  return [
    { cle: 'douleur', icone: 'sick', libelle: 'Douleur', valeur: avecUnite(b.douleurMoyenne, '/10') },
    { cle: 'fatigue', icone: 'bedtime', libelle: 'Fatigue', valeur: avecUnite(b.fatigue, '/5') },
    { cle: 'stress', icone: 'psychology', libelle: 'Stress moyen', valeur: avecUnite(stressDuBilan(b), '/5', 1) },
    { cle: 'pas', icone: 'footprint', libelle: 'Pas', valeur: avecUnite(b.pas, '') },
    { cle: 'hydratation', icone: 'water_drop', libelle: 'Hydratation', valeur: avecUnite(b.hydratation, ' L', 1) },
  ];
});

const consommations = computed(() => consommationsAlimentaires.filter((c) => props.bilan?.[c.cle]));
</script>

<template>
  <Card class="flex w-full flex-col rounded-carte border-0 bg-surface shadow-elevation">
    <CardHeader class="flex flex-row items-center justify-between gap-3 space-y-0 p-4 md:p-6">
      <CardTitle class="m-0 text-left text-[17px] font-semibold leading-tight tracking-normal text-texte first-letter:uppercase">{{ titre }}</CardTitle>
      <Button v-if="bilan" type="button" variant="outline" class="h-11 shrink-0 gap-2" @click="emit('modifier')">
        <i class="material-symbols-outlined text-lg" aria-hidden="true">edit</i>Modifier
      </Button>
    </CardHeader>
    <CardContent class="px-4 pb-4 md:px-6 md:pb-6">
      <div v-if="bilan" class="text-left">
        <div class="grid grid-cols-2 md:grid-cols-3 gap-2 mb-3">
          <HumeurResume :bilan="bilan" class="col-span-2 md:col-span-3"/>
          <div v-for="tuile in tuiles" :key="tuile.libelle" class="rounded-controle p-3" :class="teinteDe(tuile.cle).fond">
            <p class="m-0 flex items-center gap-1 text-xs font-medium" :class="teinteDe(tuile.cle).texte">
              <i class="material-symbols-outlined text-base" aria-hidden="true">{{ tuile.icone }}</i>
              {{ tuile.libelle }}
            </p>
            <p class="text-base font-semibold text-texte mt-1">{{ tuile.valeur }}</p>
          </div>
        </div>

        <div class="rounded-controle bg-fond p-3 mb-3">
          <p class="text-xs text-texte-3 flex items-center gap-1 mb-2">
            <i class="material-symbols-outlined text-base" aria-hidden="true">restaurant</i>
            Alimentation
          </p>
          <ul v-if="consommations.length" class="flex flex-wrap gap-2">
            <li
                v-for="item in consommations"
                :key="item.cle"
                class="inline-flex items-center gap-1 rounded-controle bg-surface px-2 py-1 text-xs text-texte"
            >
              <i class="material-symbols-outlined text-sm" aria-hidden="true">{{ item.icone }}</i>{{ item.libelle }}
            </li>
          </ul>
          <p v-else class="m-0 text-sm italic text-texte-3">Aucune consommation signalée</p>
        </div>

        <TransitRecap :bilan="bilan"/>

        <div v-if="bilan.commentaire" class="rounded-controle bg-fond p-3">
          <p class="text-xs text-texte-3 flex items-center gap-1 mb-1">
            <i class="material-symbols-outlined text-base" aria-hidden="true">comment</i>
            Notes
          </p>
          <p class="text-sm text-texte-2 whitespace-pre-line break-words">{{ bilan.commentaire }}</p>
        </div>
      </div>

      <div v-else class="flex flex-col items-center gap-4 text-center py-6">
        <i class="material-symbols-outlined text-6xl text-trait" aria-hidden="true">event_busy</i>
        <p class="text-lg font-semibold text-texte">
          {{ aVenir ? "Ce jour n'est pas encore arrivé" : 'Aucun bilan pour ce jour' }}
        </p>
        <Button v-if="!aVenir" type="button" variant="custom" size="lg" class="gap-2" @click="emit('remplir')">
          Remplir le bilan de ce jour
        </Button>
      </div>
    </CardContent>
  </Card>
</template>
