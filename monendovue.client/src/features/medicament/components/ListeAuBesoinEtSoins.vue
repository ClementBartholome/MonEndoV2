<template>
  <section v-if="auBesoin.length" class="flex flex-col gap-2" aria-labelledby="titre-au-besoin">
    <h2 id="titre-au-besoin" class="m-0 text-titre-carte font-semibold tracking-normal text-texte">Au besoin</h2>
    <ul class="m-0 flex list-none flex-col rounded-carte bg-surface p-0 shadow-elevation">
      <li v-for="(traitement, index) in auBesoin" :key="traitement.id"
          class="flex items-center gap-3 px-3.5 py-3" :class="{ 'border-t border-trait': index > 0 }">
        <div class="flex min-w-0 grow flex-col text-left">
          <span class="text-corps font-medium text-texte">{{ traitement.nom }}</span>
          <span class="text-legende text-texte-3">{{ dernierePrise(traitement.dernierePrise) }}</span>
        </div>
        <button type="button" class="inline-flex min-h-11 shrink-0 items-center gap-1 rounded-controle border-[1.5px] border-contour px-3 text-sm font-medium text-texte disabled:opacity-60"
                :disabled="envoi" :aria-label="`Noter une prise de ${traitement.nom}`" @click="emit('prendre', traitement)">
          <i class="material-symbols-outlined text-lg" aria-hidden="true">add</i>Noter une prise
        </button>
      </li>
    </ul>
  </section>

  <section v-if="soins.length" class="flex flex-col gap-2" aria-labelledby="titre-soins">
    <h2 id="titre-soins" class="m-0 text-titre-carte font-semibold tracking-normal text-texte">Soins</h2>
    <ul class="m-0 flex list-none flex-col rounded-carte bg-surface p-0 shadow-elevation">
      <li v-for="(soin, index) in soins" :key="soin.id"
          class="flex items-center gap-3 px-3.5 py-3" :class="{ 'border-t border-trait': index > 0 }">
        <i class="material-symbols-outlined rounded-controle bg-teinte-traitement-fond p-1.5 text-titre-2 text-teinte-traitement" aria-hidden="true">physical_therapy</i>
        <div class="flex min-w-0 grow flex-col text-left">
          <span class="text-corps font-medium text-texte">{{ soin.nom }}</span>
          <span class="text-legende text-texte-3">{{ soin.derniereSeance ? `Dernière séance le ${jour(soin.derniereSeance)}` : 'Aucune séance notée' }}</span>
        </div>
        <button type="button" class="inline-flex min-h-11 shrink-0 items-center gap-1 rounded-controle border-[1.5px] border-contour px-3 text-sm font-medium text-texte disabled:opacity-60"
                :disabled="envoi" :aria-label="`Noter une séance de ${soin.nom}`" @click="emit('seance', soin)">
          <i class="material-symbols-outlined text-lg" aria-hidden="true">add</i>Noter une séance
        </button>
      </li>
    </ul>
  </section>
</template>

<script setup lang="ts">
import { format, isToday, isYesterday } from 'date-fns';
import { fr } from 'date-fns/locale';
import type { Soin, TraitementAuBesoin } from '../types/traitements';
import { heureAffichee } from '../utils/prises';

defineProps<{ auBesoin: TraitementAuBesoin[]; soins: Soin[]; envoi: boolean }>();
const emit = defineEmits<{ prendre: [traitement: TraitementAuBesoin]; seance: [soin: Soin] }>();

const jour = (date: string) => format(new Date(date), 'd MMMM', { locale: fr });

function dernierePrise(date: string | null): string {
  if (!date) return 'Aucune prise notée';
  return `Dernière prise : ${quand(new Date(date), date)}, ${heureAffichee(date)}`;
}

function quand(moment: Date, date: string): string {
  if (isToday(moment)) return 'aujourd\'hui';
  return isYesterday(moment) ? 'hier' : `le ${jour(date)}`;
}
</script>
