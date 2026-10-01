<template>
  <section class="flex items-center gap-3.5 rounded-carte bg-surface px-[18px] py-4 shadow-elevation" aria-labelledby="titre-rendez-vous">
    <i class="material-symbols-outlined rounded-controle bg-teinte-neutre-fond p-2.5 text-titre-page text-teinte-neutre" aria-hidden="true">event</i>
    <div class="flex min-w-0 grow flex-col gap-px">
      <h2 id="titre-rendez-vous" class="m-0 text-legende font-normal tracking-normal text-texte-3">Prochain rendez-vous</h2>
      <span class="text-corps font-medium text-texte">{{ evenement.titre }}</span>
      <span class="text-legende text-texte-2">{{ quand }}</span>
      <a v-if="evenement.lieu" :href="itineraire" target="_blank" rel="noopener noreferrer"
         class="mt-0.5 inline-flex min-h-11 items-center text-legende font-medium !text-lien">Itinéraire</a>
      <router-link to="/agenda" class="inline-flex min-h-11 items-center text-legende font-medium !text-lien">Voir mon agenda</router-link>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { format } from 'date-fns';
import { fr } from 'date-fns/locale';
import type { EvenementAgenda } from '@/features/schedule/types/agenda';

const props = defineProps<{ evenement: EvenementAgenda }>();

/** « Jeudi 2 octobre · 14 h 30 », ou la date seule pour un événement sur la journée. */
const quand = computed(() => {
  const debut = new Date(props.evenement.debut);
  const jour = format(debut, 'EEEE d MMMM', { locale: fr });
  const jourMajuscule = jour.charAt(0).toUpperCase() + jour.slice(1);
  return props.evenement.journeeEntiere ? jourMajuscule : `${jourMajuscule} · ${format(debut, "H' h 'mm")}`;
});

const itineraire = computed(() =>
    `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(props.evenement.lieu ?? '')}`);
</script>
