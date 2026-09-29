<template>
  <section aria-labelledby="titre-calendrier" class="flex flex-col gap-2 rounded-carte bg-surface px-3.5 pb-4 pt-3 shadow-elevation">
    <h2 id="titre-calendrier" class="sr-only">Calendrier des règles, {{ titreMois }}</h2>
    <SelecteurMois :mois="mois" @changer="(choisi) => emit('changer', choisi)"/>

    <div class="grid grid-cols-7 gap-1 text-center text-xs text-texte-3" aria-hidden="true">
      <span v-for="(initiale, index) in INITIALES" :key="index">{{ initiale }}</span>
    </div>
    <div class="grid grid-cols-7 gap-1">
      <span v-for="n in calendrier.decalage" :key="`vide-${n}`" aria-hidden="true"></span>
      <button v-for="jour in calendrier.cases" :key="jour.cle" type="button"
              class="flex min-h-10 items-center justify-center rounded-full p-0 text-sm disabled:cursor-default"
              :class="classes(jour)"
              :disabled="jour.futur || enCoursDEnvoi.has(jour.cle)"
              :aria-pressed="jour.regles"
              :aria-label="libelle(jour)"
              @click="emit('basculer', jour)">{{ jour.numero }}</button>
    </div>
    <p class="m-0 mt-1 flex items-center gap-1.5 text-xs text-texte-2">
      <span aria-hidden="true" class="h-3 w-3 rounded-full bg-teinte-regles"></span>Règles · touche un jour pour l'ajouter ou le retirer.
    </p>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { format } from 'date-fns';
import { fr } from 'date-fns/locale';
import SelecteurMois from '@/shared/components/SelecteurMois.vue';
import type { CaseCalendrier } from '../utils/cycle';
import { jourEnToutesLettres } from '../utils/cycle';

const props = defineProps<{
  mois: Date;
  calendrier: { decalage: number; cases: CaseCalendrier[] };
  enCoursDEnvoi: Set<string>;
}>();
const emit = defineEmits<{ basculer: [jour: CaseCalendrier]; changer: [mois: Date] }>();

const titreMois = computed(() => format(props.mois, 'MMMM yyyy', { locale: fr }));

const INITIALES = ['L', 'M', 'M', 'J', 'V', 'S', 'D'];

function classes(jour: CaseCalendrier): string[] {
  const liste: string[] = [];
  if (jour.regles) liste.push('bg-teinte-regles font-semibold text-white');
  else if (jour.futur) liste.push('text-texte-3');
  else liste.push('text-texte hover:bg-surface-2');
  // Aujourd'hui : un contour, jamais une couleur de fond (réservée aux règles).
  if (jour.aujourdhui) liste.push('outline outline-2 -outline-offset-2 outline-texte');
  return liste;
}

function libelle(jour: CaseCalendrier): string {
  const texte = jourEnToutesLettres(jour.cle);
  return `${texte.charAt(0).toUpperCase()}${texte.slice(1)}${jour.aujourdhui ? ', aujourd\'hui' : ''}${jour.regles ? ', règles' : ''}`;
}
</script>
