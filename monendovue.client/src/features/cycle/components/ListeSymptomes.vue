<template>
  <section aria-label="Ce mois-ci" class="grid grid-cols-2 rounded-carte bg-surface shadow-elevation">
    <div class="flex flex-col gap-0.5 px-4 py-3.5">
      <span class="text-2xl font-semibold leading-tight text-texte">{{ chiffres.nombre }}</span>
      <span class="text-xs leading-snug text-texte-3">{{ chiffres.nombre > 1 ? 'symptômes notés' : 'symptôme noté' }}</span>
    </div>
    <div class="flex flex-col gap-0.5 border-l border-trait px-4 py-3.5">
      <span class="text-2xl font-semibold leading-tight text-texte">{{ chiffres.jours }}</span>
      <span class="text-xs leading-snug text-texte-3">{{ chiffres.jours > 1 ? 'jours concernés' : 'jour concerné' }}</span>
    </div>
  </section>

  <section v-for="groupe in groupes" :key="groupe.cle" class="flex flex-col gap-2" :aria-labelledby="`symptomes-${groupe.cle}`">
    <h2 :id="`symptomes-${groupe.cle}`" class="m-0 mt-1 text-[13px] font-medium tracking-normal text-texte-3">{{ groupe.titre }}</h2>
    <ul class="m-0 flex list-none flex-col rounded-carte bg-surface p-0 shadow-elevation">
      <li v-for="(entree, index) in groupe.entrees" :key="entree.id" :class="{ 'border-t border-trait': index > 0 }">
        <button type="button" class="flex min-h-16 w-full items-center gap-3 px-3.5 py-3 text-left" @click="emit('modifier', entree)">
          <i class="material-symbols-outlined rounded-controle bg-teinte-symptome-fond p-2 text-[22px] text-teinte-symptome" aria-hidden="true">
            {{ iconeSymptome(entree.typeSymptome) }}
          </i>
          <span class="flex min-w-0 grow flex-col">
            <span class="text-[15px] font-medium text-texte">{{ entree.typeSymptome }}</span>
            <span class="truncate text-[13px] text-texte-3">{{ detail(entree) }}</span>
          </span>
          <span class="flex h-9 min-w-9 shrink-0 items-center justify-center rounded-controle px-1 text-sm font-semibold"
                :style="couleursIntensite(entree.intensite)">{{ entree.intensite }}<span class="sr-only"> sur 10</span></span>
        </button>
      </li>
    </ul>
  </section>
</template>

<script setup lang="ts">
import { heure, type GroupeDuJour } from '@/shared/utils/jours';
import { couleursIntensite } from '@/shared/utils/intensite';
import type { SymptomeCycle } from '../types/symptome-cycle';
import { iconeSymptome, type ChiffresSymptomes } from '../utils/symptomes';

defineProps<{ chiffres: ChiffresSymptomes; groupes: GroupeDuJour<SymptomeCycle>[] }>();
const emit = defineEmits<{ modifier: [entree: SymptomeCycle] }>();

function detail(entree: SymptomeCycle): string {
  const commentaire = entree.commentaire?.trim();
  return commentaire ? `${heure(entree.date)} · ${commentaire}` : heure(entree.date);
}
</script>
