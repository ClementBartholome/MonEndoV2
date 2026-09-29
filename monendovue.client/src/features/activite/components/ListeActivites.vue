<template>
  <section v-for="groupe in groupes" :key="groupe.cle" class="flex flex-col gap-2" :aria-labelledby="`activites-${groupe.cle}`">
    <h2 :id="`activites-${groupe.cle}`" class="m-0 mt-1 text-[13px] font-medium tracking-normal text-texte-3">{{ groupe.titre }}</h2>
    <ul class="m-0 flex list-none flex-col rounded-carte bg-surface p-0 shadow-elevation">
      <li v-for="(activite, index) in groupe.entrees" :key="activite.id" :class="{ 'border-t border-trait': index > 0 }">
        <button type="button" class="flex min-h-16 w-full items-center gap-3 px-3.5 py-3 text-left" @click="emit('modifier', activite)">
          <i class="material-symbols-outlined rounded-controle bg-teinte-bilan-fond p-2 text-[22px] text-teinte-bilan" aria-hidden="true">
            {{ iconeActivite(activite.type) }}
          </i>
          <span class="flex min-w-0 grow flex-col">
            <span class="text-[15px] font-medium text-texte">{{ activite.type }}</span>
            <span class="truncate text-[13px] text-texte-3">{{ detail(activite) }}</span>
          </span>
          <i class="material-symbols-outlined text-texte-3" aria-hidden="true">chevron_right</i>
        </button>
      </li>
    </ul>
  </section>
</template>

<script setup lang="ts">
import type { GroupeDuJour } from '@/shared/utils/jours';
import type { Activite } from '../types/activite';
import { duree, iconeActivite, libelleEffet, libelleNiveau } from '../utils/activite';

defineProps<{ groupes: GroupeDuJour<Activite>[] }>();
const emit = defineEmits<{ modifier: [activite: Activite] }>();

function detail(activite: Activite): string {
  return [duree(activite.duree), libelleNiveau(activite.niveau).toLowerCase(), libelleEffet(activite.effet), activite.commentaire]
    .filter(Boolean)
    .join(' · ');
}
</script>
