<template>
  <section v-for="groupe in groupes" :key="groupe.cle" class="flex flex-col gap-2" :aria-labelledby="`jour-${groupe.cle}`">
    <h2 :id="`jour-${groupe.cle}`" class="m-0 mt-1 text-legende font-medium tracking-normal text-texte-3">{{ groupe.titre }}</h2>
    <ul class="m-0 flex list-none flex-col rounded-carte bg-surface p-0 shadow-elevation">
      <li v-for="(entree, index) in groupe.entrees" :key="entree.id" :class="{ 'border-t border-trait': index > 0 }">
        <button type="button" class="flex min-h-16 w-full items-center gap-3 px-3.5 py-3 text-left" @click="emit('modifier', entree)">
          <span class="flex h-10 w-10 shrink-0 items-center justify-center rounded-controle text-sm font-semibold"
                :style="couleursIntensite(entree.intensite)">{{ entree.intensite }}<span class="sr-only"> sur 10</span></span>
          <span class="flex min-w-0 grow flex-col">
            <span class="text-corps font-medium text-texte">{{ libelleCourt(entree.typeDouleur) }}</span>
            <span class="truncate text-legende text-texte-3">{{ detail(entree) }}</span>
          </span>
          <i class="material-symbols-outlined text-texte-3" aria-hidden="true">chevron_right</i>
        </button>
      </li>
    </ul>
  </section>
</template>

<script setup lang="ts">
import type { DonneesDouleur } from '../types/donnees-douleur';
import { heure, type GroupeDuJour } from '@/shared/utils/jours';
import { couleursIntensite } from '@/shared/utils/intensite';
import { commentaireAffiche, libelleCourt } from '../utils/douleurs';

defineProps<{ groupes: GroupeDuJour<DonneesDouleur>[] }>();
const emit = defineEmits<{ modifier: [entree: DonneesDouleur] }>();

function detail(entree: DonneesDouleur): string {
  const commentaire = commentaireAffiche(entree.commentaire);
  return commentaire ? `${heure(entree.date)} · ${commentaire}` : heure(entree.date);
}
</script>
