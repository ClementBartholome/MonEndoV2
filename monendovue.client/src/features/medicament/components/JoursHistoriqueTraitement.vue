<template>
  <section v-for="jour in jours" :key="jour.jour" class="flex flex-col gap-2" :aria-labelledby="`historique-${jour.jour}`">
    <h2 :id="`historique-${jour.jour}`" class="m-0 mt-1 text-[13px] font-medium tracking-normal text-texte-3">{{ titreDuJour(jour.jour) }}</h2>
    <ul class="m-0 flex list-none flex-col rounded-carte bg-surface p-0 shadow-elevation">
      <li v-for="(entree, index) in jour.entrees" :key="entree.id"
          class="flex min-h-14 items-center gap-3 px-3.5 py-2 text-left" :class="{ 'border-t border-trait': index > 0 }">
        <span class="inline-flex grow items-center gap-1.5 text-[15px]" :class="entree.nature === 'Ignore' ? 'text-texte-3' : 'text-texte'">
          <i class="material-symbols-outlined icone-pleine text-xl" :class="icone(entree).classe" aria-hidden="true">{{ icone(entree).nom }}</i>
          {{ libelle(entree) }}
        </span>
        <button v-if="aConfirmer !== entree.id" type="button"
                class="flex h-11 w-11 shrink-0 items-center justify-center rounded-full text-texte-3 hover:bg-surface-2"
                :aria-label="`Retirer : ${libelle(entree)}, ${titreDuJour(jour.jour)}`" :disabled="envoi"
                @click="aConfirmer = entree.id">
          <i class="material-symbols-outlined" aria-hidden="true">delete</i>
        </button>
        <button v-else type="button" :disabled="envoi"
                class="min-h-11 shrink-0 rounded-controle border-[1.5px] border-danger px-3 text-sm font-medium text-danger"
                @click="emit('retirer', entree)">Confirmer</button>
      </li>
    </ul>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { heure, titreDuJour } from '@/shared/utils/jours';
import type { EntreeHistoriqueTraitement, JourHistoriqueTraitement } from '../types/traitements';
import { heureAffichee } from '../utils/prises';

defineProps<{ jours: JourHistoriqueTraitement[]; envoi: boolean }>();
const emit = defineEmits<{ retirer: [entree: EntreeHistoriqueTraitement] }>();

/** Retrait en deux temps : le premier toucher demande confirmation sur la ligne même. */
const aConfirmer = ref<number | null>(null);

function libelle(entree: EntreeHistoriqueTraitement): string {
  if (entree.nature === 'Seance') return `Séance à ${heure(entree.date)}`;
  if (entree.nature === 'Ignore') return entree.heurePrevue ? `Prise de ${heureAffichee(entree.heurePrevue)} ignorée` : 'Prise ignorée';
  const prevue = entree.heurePrevue ? ` (prévue ${heureAffichee(entree.heurePrevue)})` : '';
  return `Pris à ${heure(entree.date)}${prevue}`;
}

function icone(entree: EntreeHistoriqueTraitement): { nom: string; classe: string } {
  if (entree.nature === 'Ignore') return { nom: 'do_not_disturb_on', classe: 'text-texte-3' };
  if (entree.nature === 'Seance') return { nom: 'check_circle', classe: 'text-teinte-bilan' };
  return { nom: 'check_circle', classe: 'text-etat-fait' };
}
</script>
