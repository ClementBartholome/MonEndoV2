<template>
  <section aria-labelledby="titre-coup-doeil" class="flex flex-col gap-3 border-y border-trait py-4 text-left">
    <h2 id="titre-coup-doeil" class="m-0 text-legende font-semibold uppercase tracking-wider text-texte-2">En un coup d'œil</h2>

    <div v-if="chargement && !synthese" class="flex flex-col gap-3" aria-busy="true" aria-label="Chargement de l'aperçu">
      <Skeleton class="h-10 w-full rounded-controle"/>
      <Skeleton class="h-24 w-full rounded-controle"/>
    </div>
    <p v-else-if="erreur" class="m-0 text-sm text-texte-2">L'aperçu n'a pas pu être chargé. Tu peux quand même créer le PDF.</p>

    <template v-else-if="synthese && chiffres">
      <Frise :jours="frise"/>
      <dl class="m-0 grid grid-cols-2 gap-x-4 gap-y-3.5">
        <div class="flex flex-col gap-1">
          <dt class="order-2 text-legende text-texte-2">jours avec une douleur notée</dt>
          <dd class="order-1 m-0 text-texte"><span class="chiffre text-2xl font-semibold">{{ chiffres.joursDouleur }}</span><span class="text-sm text-texte-2"> / {{ chiffres.jours }}</span></dd>
        </div>
        <div class="flex flex-col gap-1">
          <dt class="order-2 text-legende text-texte-2">jours à 6/10 ou plus<template v-if="chiffres.joursForts">, dont {{ chiffres.fortsPendantRegles }} pendant les règles</template></dt>
          <dd class="order-1 m-0 text-texte"><span class="chiffre text-2xl font-semibold">{{ chiffres.joursForts }}</span></dd>
        </div>
        <div class="flex flex-col gap-1">
          <dt class="order-2 text-legende text-texte-2">jours de règles<template v-if="chiffres.debutsRegles">, {{ debuts }}</template></dt>
          <dd class="order-1 m-0 text-texte"><span class="chiffre text-2xl font-semibold">{{ chiffres.joursRegles }}</span></dd>
        </div>
        <div class="flex flex-col gap-1">
          <template v-if="chiffres.journeesNotees">
            <dt class="order-2 text-legende text-texte-2">journées limitées<template v-if="chiffres.absences">, dont {{ absences }}</template></dt>
            <dd class="order-1 m-0 text-texte"><span class="chiffre text-2xl font-semibold">{{ chiffres.journeesLimitees }}</span><span class="text-sm text-texte-2"> / {{ chiffres.journeesNotees }}</span></dd>
          </template>
          <template v-else>
            <dt class="order-2 text-legende text-texte-2">journées limitées : rien de noté</dt>
            <dd class="order-1 m-0 text-texte"><span class="chiffre text-2xl font-semibold text-texte-3">—</span></dd>
          </template>
        </div>
      </dl>
      <p class="m-0 text-legende text-texte-3">Ce que tu as noté sur la période. Rien n'est enregistré ni envoyé tant que tu ne crées pas le PDF.</p>
    </template>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import Frise from '@/shared/components/Frise.vue';
import { Skeleton } from '@/shared/components/ui/skeleton';
import type { SyntheseRendezVous } from '../types/synthese';
import { coupDOeil } from '../utils/coupDOeil';
import { friseDeLaSynthese } from '../utils/pdfRendezVous';

/** Aperçu de la synthèse de la période choisie : frise et chiffres clés, avant de créer le PDF. */
const props = defineProps<{ synthese: SyntheseRendezVous | null; chargement: boolean; erreur: boolean }>();

const chiffres = computed(() => (props.synthese ? coupDOeil(props.synthese) : null));
const frise = computed(() => (props.synthese ? friseDeLaSynthese(props.synthese) : []));
const debuts = computed(() => {
  const n = chiffres.value?.debutsRegles ?? 0;
  return n > 1 ? `${n} débuts` : '1 début';
});
const absences = computed(() => {
  const n = chiffres.value?.absences ?? 0;
  return n > 1 ? `${n} absences` : '1 absence';
});
</script>
