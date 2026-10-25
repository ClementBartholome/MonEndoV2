<script setup lang="ts">
import type { DouleurParType } from '../utils/douleurs';

// Section à filets : un type par ligne (jours notés, moyenne, la plus forte), bilans et crises confondus.
const props = defineProps<{ types: DouleurParType[] }>();

const largeur = (jours: number) => `${Math.round((jours / Math.max(...props.types.map((t) => t.jours))) * 100)}%`;
const nombre = (n: number) => n.toLocaleString('fr-FR');
</script>

<template>
  <section aria-labelledby="titre-par-type" class="flex flex-col gap-3 border-b border-trait pb-4 pt-1 text-left">
    <h2 id="titre-par-type" class="m-0 text-legende font-semibold uppercase tracking-wider text-texte-2">Par type</h2>
    <ul class="m-0 flex list-none flex-col gap-3 p-0">
      <li v-for="type in types" :key="type.libelle" class="flex flex-col gap-1.5 text-left">
        <div class="flex items-baseline gap-2">
          <span class="grow text-corps font-medium text-texte">{{ type.libelle }}</span>
          <span class="chiffre text-corps text-texte">{{ type.jours }} j<span class="sr-only">ours</span></span>
          <span class="min-w-28 text-right text-xs text-texte-2">moy. {{ nombre(type.moyenne) }} · max {{ type.max }}</span>
        </div>
        <div class="h-2 rounded bg-surface-2" aria-hidden="true">
          <div class="h-2 rounded" :style="{ width: largeur(type.jours), background: 'var(--intensite-6)' }"></div>
        </div>
      </li>
    </ul>
    <p class="m-0 text-xs text-texte-3">Les zones notées dans tes bilans et les crises sont comptées ensemble.</p>
  </section>
</template>
