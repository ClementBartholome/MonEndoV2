<template>
  <section aria-labelledby="titre-point-semaine" class="flex flex-col gap-3 rounded-carte bg-surface px-[18px] py-4 shadow-elevation">
    <div class="flex items-center gap-3.5">
      <i class="material-symbols-outlined rounded-controle bg-teinte-symptome-fond p-2.5 text-titre-page text-teinte-symptome" aria-hidden="true">photo_camera</i>
      <div class="flex min-w-0 flex-col">
        <h2 id="titre-point-semaine" class="m-0 text-titre-carte font-semibold tracking-normal text-texte">Point de la semaine</h2>
        <span class="text-legende text-texte-2">{{ suivis.length ? `Dernière photo ${ilYA(suivis[0].date, maintenant)}` : 'Aucune photo pour l\'instant' }}</span>
      </div>
    </div>
    <button type="button" class="inline-flex min-h-12 items-center justify-center gap-2 rounded-controle bg-button text-corps font-semibold text-texte"
            @click="emit('ajouter')">
      <i class="material-symbols-outlined text-xl" aria-hidden="true">add_a_photo</i>Ajouter une photo
    </button>
  </section>

  <section v-if="comparaison" aria-labelledby="titre-avant-apres" class="flex flex-col gap-2.5">
    <div class="flex items-center justify-between gap-2">
      <h2 id="titre-avant-apres" class="m-0 text-titre-carte font-semibold tracking-normal text-texte">Avant / après</h2>
      <div role="radiogroup" aria-label="Écart entre les photos" class="flex gap-1 rounded-controle bg-surface-2 p-1">
        <button v-for="valeur in ECARTS" :key="valeur" type="button" role="radio" :aria-checked="ecart === valeur"
                class="min-h-11 rounded-controle px-2.5 text-legende"
                :class="ecart === valeur ? 'bg-surface font-semibold text-texte shadow-elevation' : 'text-texte-2'"
                @click="ecart = valeur">{{ valeur }} mois</button>
      </div>
    </div>
    <div class="grid grid-cols-2 gap-2.5">
      <figure v-for="photo in [comparaison.avant, comparaison.apres]" :key="photo.id" class="m-0 flex flex-col gap-1.5">
        <button type="button" class="overflow-hidden rounded-carte" :aria-label="`Agrandir la photo du ${jour(photo.date)}`"
                @click="emit('agrandir', photo.photoUrl)">
          <img :src="photo.photoUrl" :alt="`Photo du ${jour(photo.date)}`" loading="lazy" class="aspect-[3/4] w-full bg-surface-2 object-cover object-top">
        </button>
        <figcaption class="text-center text-legende text-texte-2">{{ jour(photo.date) }}</figcaption>
      </figure>
    </div>
  </section>

  <section v-if="suivis.length" aria-labelledby="titre-photos" class="flex flex-col gap-2.5">
    <h2 id="titre-photos" class="m-0 text-titre-carte font-semibold tracking-normal text-texte">Toutes les photos</h2>
    <ul class="m-0 grid list-none grid-cols-3 gap-2 p-0">
      <li v-for="suivi in suivis" :key="suivi.id">
        <button type="button" class="flex w-full flex-col items-stretch gap-1 text-left" :aria-label="`Photo du ${jour(suivi.date)}, intensité ${suivi.intensite} sur 10`"
                @click="emit('modifier', suivi)">
          <img :src="suivi.photoUrl" alt="" loading="lazy" class="aspect-square w-full rounded-controle bg-surface-2 object-cover object-top">
          <span class="text-xs text-texte-2">{{ jourAbrege(suivi.date) }}</span>
        </button>
      </li>
    </ul>
    <button v-if="plusAnciennes > 0" type="button" class="inline-flex min-h-11 items-center gap-1.5 self-start text-sm font-medium text-lien" @click="emit('voirPlus')">
      <i class="material-symbols-outlined text-lg" aria-hidden="true">expand_more</i>Voir les photos plus anciennes ({{ plusAnciennes }})
    </button>
  </section>
</template>

<script setup lang="ts">
import { format } from 'date-fns';
import { fr } from 'date-fns/locale';
import type { SuiviAcne } from '../types/acne';
import { ilYA, type EcartComparaison } from '../utils/acne';

defineProps<{ suivis: SuiviAcne[]; plusAnciennes: number; comparaison: { avant: SuiviAcne; apres: SuiviAcne } | null; maintenant: Date }>();
const ecart = defineModel<EcartComparaison>('ecart', { required: true });
const emit = defineEmits<{ ajouter: []; modifier: [suivi: SuiviAcne]; agrandir: [url: string]; voirPlus: [] }>();

const ECARTS: EcartComparaison[] = [1, 3, 6];
const jour = (date: string) => format(new Date(date), 'd MMMM yyyy', { locale: fr });
const jourAbrege = (date: string) => format(new Date(date), 'd MMM', { locale: fr });
</script>
