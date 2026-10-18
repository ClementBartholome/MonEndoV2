<template>
  <section class="flex flex-col gap-3 rounded-carte bg-surface p-[18px] shadow-elevation" aria-labelledby="titre-acne-en-cours">
    <div class="flex items-center gap-3.5">
      <i class="material-symbols-outlined rounded-controle bg-teinte-symptome-fond p-2.5 text-titre-page text-teinte-symptome" aria-hidden="true">face</i>
      <div class="flex min-w-0 grow flex-col gap-px">
        <span class="text-legende text-texte-3">Acné en cours</span>
        <h2 id="titre-acne-en-cours" class="m-0 text-corps font-medium tracking-normal text-texte">Depuis le {{ jourCourt(acne.debut) }} · {{ jours(acne.jours) }}</h2>
        <span class="text-legende text-texte-2">{{ dernierePhoto }}</span>
      </div>
    </div>
    <div class="flex flex-wrap items-center gap-2">
      <router-link to="/acne?ajouter"
                   class="inline-flex min-h-11 items-center gap-1.5 rounded-controle bg-button px-3.5 text-sm font-semibold !text-texte">
        <i class="material-symbols-outlined text-xl" aria-hidden="true">photo_camera</i>Ajouter une photo
      </router-link>
      <router-link to="/acne" class="inline-flex min-h-11 items-center px-2.5 text-sm font-medium !text-lien">Voir le suivi</router-link>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { differenceInCalendarDays } from 'date-fns';
import { jourCourt, jours } from '@/features/cycle/utils/cycle';
import type { AcneAujourdhui } from '../types/aujourdhui';

/** `aujourdhui` : jour local (AAAA-MM-JJ) auquel se rapporte l'accueil. */
const props = defineProps<{ acne: AcneAujourdhui; aujourdhui: string }>();

const dernierePhoto = computed(() => {
  if (!props.acne.dernierePhoto) return 'Pas encore de photo de suivi';
  const ecart = differenceInCalendarDays(new Date(`${props.aujourdhui}T12:00:00`), new Date(`${props.acne.dernierePhoto}T12:00:00`));
  if (ecart <= 0) return 'Dernière photo aujourd\'hui';
  if (ecart === 1) return 'Dernière photo hier';
  return `Dernière photo il y a ${ecart} jours`;
});
</script>
