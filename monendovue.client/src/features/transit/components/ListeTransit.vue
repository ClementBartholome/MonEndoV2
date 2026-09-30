<template>
  <section v-for="groupe in groupes" :key="groupe.cle" class="flex flex-col gap-2" :aria-labelledby="`transit-${groupe.cle}`">
    <h2 :id="`transit-${groupe.cle}`" class="m-0 mt-1 text-legende font-medium tracking-normal text-texte-3">{{ groupe.titre }}</h2>
    <ul class="m-0 flex list-none flex-col rounded-carte bg-surface p-0 shadow-elevation">
      <li v-for="(entree, index) in groupe.entrees" :key="entree.id"
          class="flex min-h-16 items-center gap-3 py-2 pl-3.5 pr-1.5 text-left" :class="{ 'border-t border-trait': index > 0 }">
        <i class="material-symbols-outlined rounded-controle bg-teinte-neutre-fond p-2 text-titre-2 text-teinte-neutre" aria-hidden="true">gastroenterology</i>
        <span class="flex min-w-0 grow flex-col">
          <span class="text-corps font-medium text-texte">{{ entree.typeEvenement }}</span>
          <span class="text-legende text-texte-3">{{ detail(entree) }}</span>
        </span>
        <button v-if="aConfirmer !== entree.id" type="button"
                class="flex h-11 w-11 shrink-0 items-center justify-center rounded-full text-texte-3 hover:bg-surface-2"
                :aria-label="`Supprimer : ${entree.typeEvenement}, ${titreDuJour(cleJour(entree.date))}`" :disabled="envoi"
                @click="aConfirmer = entree.id">
          <i class="material-symbols-outlined text-xl" aria-hidden="true">delete</i>
        </button>
        <button v-else type="button" :disabled="envoi"
                class="min-h-11 shrink-0 rounded-controle border-[1.5px] border-danger px-3 text-sm font-medium text-danger"
                @click="emit('supprimer', entree)">Confirmer</button>
      </li>
    </ul>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { cleJour, heure, titreDuJour, type GroupeDuJour } from '@/shared/utils/jours';
import type { DonneesTransit } from '../types/donnees-transit';

defineProps<{ groupes: GroupeDuJour<DonneesTransit>[]; envoi: boolean }>();
const emit = defineEmits<{ supprimer: [entree: DonneesTransit] }>();

/** Suppression en deux temps : le premier toucher demande confirmation sur la ligne même. */
const aConfirmer = ref<number | null>(null);

/** « 8 h 40 · légère · saignement · douleur · note » */
function detail(entree: DonneesTransit): string {
  return [heure(entree.date), entree.intensite?.toLowerCase(), entree.saignement ? 'saignement' : '', entree.douleur ? 'douleur' : '', entree.commentaires?.trim()]
    .filter(Boolean)
    .join(' · ');
}
</script>
