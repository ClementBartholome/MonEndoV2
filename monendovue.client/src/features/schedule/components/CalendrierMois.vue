<template>
  <div class="rounded-carte bg-surface px-2 pb-3 pt-2 shadow-elevation">
    <div class="flex items-center justify-between">
      <button type="button" aria-label="Mois précédent" class="flex h-11 w-11 items-center justify-center text-texte" @click="emit('mois', -1)">
        <i class="material-symbols-outlined" aria-hidden="true">chevron_left</i>
      </button>
      <h2 class="m-0 text-corps font-semibold capitalize text-texte" aria-live="polite">{{ titre }}</h2>
      <button type="button" aria-label="Mois suivant" class="flex h-11 w-11 items-center justify-center text-texte" @click="emit('mois', 1)">
        <i class="material-symbols-outlined" aria-hidden="true">chevron_right</i>
      </button>
    </div>

    <div class="grid grid-cols-7 py-1 text-center text-legende font-medium text-texte-3" aria-hidden="true">
      <span v-for="(lettre, indice) in LETTRES" :key="indice">{{ lettre }}</span>
    </div>

    <div class="grid grid-cols-7 gap-y-0.5">
      <button v-for="cellule in cellules" :key="cellule.cle" type="button" :aria-label="cellule.libelle"
              :aria-pressed="cellule.choisi" :aria-current="cellule.aujourdhui ? 'date' : undefined"
              class="flex min-h-12 flex-col items-center justify-center gap-1 rounded-[14px] border-[1.5px] text-sm"
              :class="classes(cellule)" @click="emit('choisir', cellule.date)">
        <span class="leading-none">{{ cellule.numero }}</span>
        <span class="size-1.5 rounded-full" :class="cellule.rendezVous ? (cellule.choisi ? 'bg-fond' : 'bg-lien') : 'bg-transparent'"></span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { eachDayOfInterval, endOfMonth, endOfWeek, format, isSameDay, isSameMonth, startOfWeek } from 'date-fns';
import { fr } from 'date-fns/locale';
import type { EvenementAgenda } from '../types/agenda';
import { debutDe } from '../utils/rendezVous';

/** Mois en grille (lundi en premier) : un point sous les jours qui ont un rendez-vous, le jour choisi en foncé. */
const props = defineProps<{ mois: Date; evenements: EvenementAgenda[]; jourChoisi: Date; aujourdhui: Date }>();
const emit = defineEmits<{ mois: [decalage: number]; choisir: [jour: Date] }>();

const LETTRES = ['L', 'M', 'M', 'J', 'V', 'S', 'D'];

const titre = computed(() => format(props.mois, 'MMMM yyyy', { locale: fr }));

interface Cellule {
  cle: string;
  date: Date;
  numero: number;
  libelle: string;
  horsMois: boolean;
  aujourdhui: boolean;
  choisi: boolean;
  rendezVous: boolean;
}

const cellules = computed<Cellule[]>(() => {
  const jours = eachDayOfInterval({
    start: startOfWeek(props.mois, { weekStartsOn: 1 }),
    end: endOfWeek(endOfMonth(props.mois), { weekStartsOn: 1 }),
  });
  return jours.map((date) => {
    const rendezVous = props.evenements.some((evenement) => isSameDay(debutDe(evenement), date));
    return {
      cle: format(date, 'yyyy-MM-dd'),
      date,
      numero: date.getDate(),
      libelle: `${format(date, 'EEEE d MMMM', { locale: fr })}${rendezVous ? ', un rendez-vous' : ''}`,
      horsMois: !isSameMonth(date, props.mois),
      aujourdhui: isSameDay(date, props.aujourdhui),
      choisi: isSameDay(date, props.jourChoisi),
      rendezVous,
    };
  });
});

function classes(cellule: Cellule): string {
  if (cellule.choisi) return 'border-texte bg-texte font-semibold text-fond';
  if (cellule.aujourdhui) return 'border-texte font-semibold text-texte';
  return `border-transparent ${cellule.horsMois ? 'text-texte-3' : 'text-texte'}`;
}
</script>
