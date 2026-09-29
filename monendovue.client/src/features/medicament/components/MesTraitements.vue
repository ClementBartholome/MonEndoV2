<template>
  <section class="flex flex-col gap-2" aria-labelledby="titre-mes-traitements">
    <h2 id="titre-mes-traitements" class="m-0 text-[17px] font-semibold tracking-normal text-texte">Mes traitements</h2>
    <ul v-if="enCours.length" class="m-0 flex list-none flex-col rounded-carte bg-surface p-0 shadow-elevation">
      <li v-for="(traitement, index) in enCours" :key="traitement.id" :class="{ 'border-t border-trait': index > 0 }">
        <button type="button" class="flex min-h-[62px] w-full items-center gap-3 px-3.5 text-left" @click="emit('modifier', traitement)">
          <span class="flex min-w-0 grow flex-col">
            <span class="text-[15px] font-medium text-texte">{{ traitement.nom }}</span>
            <span class="truncate text-[13px] text-texte-3">{{ detail(traitement) }}</span>
          </span>
          <i class="material-symbols-outlined text-texte-3" aria-hidden="true">chevron_right</i>
        </button>
      </li>
    </ul>

    <button v-if="termines.length" type="button" class="inline-flex min-h-11 items-center gap-1.5 self-start text-sm font-medium text-lien"
            :aria-expanded="voirTermines" @click="voirTermines = !voirTermines">
      <i class="material-symbols-outlined text-lg" aria-hidden="true">history</i>Traitements terminés ({{ termines.length }})
    </button>
    <ul v-if="voirTermines" class="m-0 flex list-none flex-col rounded-carte bg-surface-2 p-0">
      <li v-for="(traitement, index) in termines" :key="traitement.id" :class="{ 'border-t border-trait': index > 0 }">
        <button type="button" class="flex min-h-14 w-full items-center gap-3 px-3.5 text-left" @click="emit('modifier', traitement)">
          <span class="flex min-w-0 grow flex-col">
            <span class="text-[15px] text-texte">{{ traitement.nom }}</span>
            <span class="text-[13px] text-texte-3">{{ periode(traitement) }}</span>
          </span>
          <i class="material-symbols-outlined text-texte-3" aria-hidden="true">chevron_right</i>
        </button>
      </li>
    </ul>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { format } from 'date-fns';
import { fr } from 'date-fns/locale';
import type { Traitement } from '../types/traitements';
import { resumeFrequence } from '../utils/prises';

defineProps<{ enCours: Traitement[]; termines: Traitement[] }>();
const emit = defineEmits<{ modifier: [traitement: Traitement] }>();

const voirTermines = ref(false);

function detail(traitement: Traitement): string {
  return traitement.type === 'NonMedicamenteux' ? 'Soin' : [traitement.dose, resumeFrequence(traitement)].filter(Boolean).join(' · ');
}

function periode(traitement: Traitement): string {
  const date = (valeur: string) => format(new Date(`${valeur}T12:00:00`), 'd MMM yyyy', { locale: fr });
  return traitement.dateFin ? `Du ${date(traitement.dateDebut)} au ${date(traitement.dateFin)}` : `Depuis le ${date(traitement.dateDebut)}`;
}
</script>
