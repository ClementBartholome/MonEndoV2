<template>
  <PanneauBas v-model:open="ouvert" :titre="evenement?.titre ?? ''">
    <div v-if="evenement" class="flex flex-col gap-4 text-left">
      <p class="m-0 text-sm text-texte-2">{{ quand }}</p>

      <div v-if="evenement.lieu" class="flex items-center gap-3 rounded-carte bg-surface px-4 py-3 shadow-elevation">
        <i class="material-symbols-outlined rounded-controle bg-teinte-neutre-fond p-1.5 text-titre-2 text-teinte-neutre" aria-hidden="true">location_on</i>
        <span class="min-w-0 grow text-corps text-texte">{{ evenement.lieu }}</span>
        <a :href="itineraire" target="_blank" rel="noopener noreferrer"
           class="inline-flex min-h-11 shrink-0 items-center text-legende font-medium !text-lien">Itinéraire</a>
      </div>

      <div v-if="aVenir" class="flex flex-col gap-2">
        <button type="button" :disabled="preparationEnCours"
                class="inline-flex min-h-[52px] items-center justify-center gap-2 rounded-controle bg-button text-base font-semibold text-texte disabled:opacity-60"
                @click="emit('preparer')">
          <i class="material-symbols-outlined" aria-hidden="true">picture_as_pdf</i>{{ preparationEnCours ? 'Un instant…' : 'Préparer ce rendez-vous' }}
        </button>
        <p class="m-0 text-legende text-texte-2">
          Ouvre l'export réglé sur la période qui suit ton rendez-vous précédent. Tu pourras y ajouter tes questions avant de créer le PDF.
        </p>
      </div>

      <a v-if="evenement.lien" :href="evenement.lien" target="_blank" rel="noopener noreferrer"
         class="inline-flex min-h-12 items-center justify-center gap-2 rounded-controle border-[1.5px] border-contour bg-surface text-sm font-medium text-texte no-underline">
        <i class="material-symbols-outlined" aria-hidden="true">open_in_new</i>Ouvrir dans Google Agenda
      </a>
    </div>
  </PanneauBas>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { format } from 'date-fns';
import PanneauBas from '@/shared/components/PanneauBas.vue';
import type { EvenementAgenda } from '../types/agenda';
import { debutDe, heureDe, jourComplet } from '../utils/rendezVous';

/** Détail d'un rendez-vous dans un panneau : quand, où, « Préparer ce rendez-vous » (à venir seulement), lien vers Google Agenda. */
const props = defineProps<{ evenement: EvenementAgenda | null; aVenir: boolean; preparationEnCours: boolean }>();
const emit = defineEmits<{ preparer: [] }>();
const ouvert = defineModel<boolean>('open', { required: true });

const quand = computed(() => {
  if (!props.evenement) return '';
  const jour = jourComplet(debutDe(props.evenement));
  const majuscule = jour.charAt(0).toUpperCase() + jour.slice(1);
  const annee = format(debutDe(props.evenement), 'yyyy');
  return props.evenement.journeeEntiere ? `${majuscule} ${annee}` : `${majuscule} ${annee} · ${heureDe(props.evenement)}`;
});

const itineraire = computed(() =>
    `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(props.evenement?.lieu ?? '')}`);
</script>
