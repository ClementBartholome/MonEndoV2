<template>
  <!-- Une seule liste compacte (un traitement quotidien donne une trentaine de lignes par mois) : une ligne par prise. -->
  <section aria-labelledby="titre-historique-prises" class="flex flex-col gap-2">
    <h2 id="titre-historique-prises" class="m-0 text-titre-carte font-semibold tracking-normal text-texte">{{ titre }}</h2>
    <ul class="m-0 flex list-none flex-col rounded-carte bg-surface p-0 shadow-elevation">
      <template v-for="(jour, indexJour) in jours" :key="jour.jour">
        <li v-for="(entree, index) in jour.entrees" :key="entree.id"
            class="flex min-h-[52px] items-center gap-3 py-1.5 pl-3.5 pr-1.5 text-left"
            :class="{ 'border-t border-trait': indexJour > 0 || index > 0 }">
          <span class="w-14 shrink-0 whitespace-nowrap text-legende text-texte-3">{{ index === 0 ? jourCourt(jour.jour) : '' }}</span>
          <span class="inline-flex min-w-0 grow items-center gap-1.5 text-corps" :class="entree.nature === 'Ignore' ? 'text-texte-3' : 'text-texte'">
            <i class="material-symbols-outlined icone-pleine text-xl" :class="icone(entree).classe" aria-hidden="true">{{ icone(entree).nom }}</i>
            <span class="truncate">{{ libelle(entree) }}</span>
          </span>
          <button v-if="aConfirmer !== entree.id" type="button"
                  class="flex h-11 w-11 shrink-0 items-center justify-center rounded-full text-texte-3 hover:bg-surface-2"
                  :aria-label="`Retirer : ${libelle(entree)}, ${titreDuJour(jour.jour)}`" :disabled="envoi"
                  @click="aConfirmer = entree.id">
            <i class="material-symbols-outlined text-xl" aria-hidden="true">delete</i>
          </button>
          <button v-else type="button" :disabled="envoi"
                  class="min-h-11 shrink-0 rounded-controle border-[1.5px] border-danger px-3 text-sm font-medium text-danger"
                  @click="emit('retirer', entree)">Confirmer</button>
        </li>
      </template>
    </ul>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { format } from 'date-fns';
import { fr } from 'date-fns/locale';
import { heure, titreDuJour } from '@/shared/utils/jours';
import type { EntreeHistoriqueTraitement, JourHistoriqueTraitement } from '../types/traitements';
import { heureAffichee } from '../utils/prises';

/** `plusieursHoraires` : l'horaire prévu est rappelé sur la ligne (sinon il figure déjà sous le nom du traitement). */
const props = defineProps<{ jours: JourHistoriqueTraitement[]; envoi: boolean; titre: string; plusieursHoraires: boolean }>();
const emit = defineEmits<{ retirer: [entree: EntreeHistoriqueTraitement] }>();

/** Retrait en deux temps : le premier toucher demande confirmation sur la ligne même. */
const aConfirmer = ref<number | null>(null);

/** « mar. 29 » : le mois est celui du sélecteur. */
const jourCourt = (cle: string) => format(new Date(`${cle}T12:00:00`), 'EEE d', { locale: fr });

function libelle(entree: EntreeHistoriqueTraitement): string {
  if (entree.nature === 'Seance') return `Séance à ${heure(entree.date)}`;
  const prevue = props.plusieursHoraires && entree.heurePrevue ? `${heureAffichee(entree.heurePrevue)} · ` : '';
  if (entree.nature === 'Ignore') return `${prevue}Ignorée`;
  return `${prevue}Pris à ${heure(entree.date)}`;
}

function icone(entree: EntreeHistoriqueTraitement): { nom: string; classe: string } {
  if (entree.nature === 'Ignore') return { nom: 'do_not_disturb_on', classe: 'text-texte-3' };
  if (entree.nature === 'Seance') return { nom: 'check_circle', classe: 'text-teinte-bilan' };
  return { nom: 'check_circle', classe: 'text-etat-fait' };
}
</script>
