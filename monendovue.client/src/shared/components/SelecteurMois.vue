<template>
  <div class="flex items-center justify-between">
    <button type="button" aria-label="Mois précédent" class="flex h-11 w-11 shrink-0 items-center justify-center rounded-full text-texte hover:bg-surface-2"
            @click="emit('changer', addMonths(mois, -1))">
      <i class="material-symbols-outlined" aria-hidden="true">chevron_left</i>
    </button>
    <button type="button" class="inline-flex min-h-11 items-center gap-1 rounded-controle px-3 text-base font-semibold text-texte hover:bg-surface-2"
            aria-haspopup="dialog" :aria-label="`${titre(mois)}, choisir un autre mois`" @click="ouvrir">
      <span aria-live="polite">{{ titre(mois) }}</span>
      <i class="material-symbols-outlined text-xl text-texte-3" aria-hidden="true">expand_more</i>
    </button>
    <button type="button" aria-label="Mois suivant" :disabled="!avantLeMax(addMonths(mois, 1))"
            class="flex h-11 w-11 shrink-0 items-center justify-center rounded-full text-texte hover:bg-surface-2 disabled:text-trait"
            @click="emit('changer', addMonths(mois, 1))">
      <i class="material-symbols-outlined" aria-hidden="true">chevron_right</i>
    </button>

    <PanneauBas v-model:open="choixOuvert" titre="Choisir un mois">
      <div class="flex flex-col gap-4 pb-2">
        <div class="flex items-center justify-between">
          <button type="button" aria-label="Année précédente" class="flex h-11 w-11 items-center justify-center rounded-full text-texte hover:bg-surface-2"
                  @click="annee--">
            <i class="material-symbols-outlined" aria-hidden="true">chevron_left</i>
          </button>
          <span class="text-lg font-semibold text-texte" aria-live="polite">{{ annee }}</span>
          <button type="button" aria-label="Année suivante" :disabled="annee >= max.getFullYear()"
                  class="flex h-11 w-11 items-center justify-center rounded-full text-texte hover:bg-surface-2 disabled:text-trait"
                  @click="annee++">
            <i class="material-symbols-outlined" aria-hidden="true">chevron_right</i>
          </button>
        </div>
        <div class="grid grid-cols-3 gap-2" role="group" :aria-label="`Mois de ${annee}`">
          <button v-for="(nom, index) in NOMS" :key="nom" type="button"
                  class="min-h-12 rounded-controle border-[1.5px] text-[15px] disabled:opacity-40"
                  :class="estChoisi(index) ? 'border-texte bg-texte font-medium text-fond' : 'border-trait bg-surface text-texte'"
                  :disabled="!avantLeMax(new Date(annee, index, 1))"
                  :aria-pressed="estChoisi(index)"
                  @click="choisir(index)">{{ nom }}</button>
        </div>
      </div>
    </PanneauBas>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { addMonths, format, startOfMonth } from 'date-fns';
import { fr } from 'date-fns/locale';
import PanneauBas from '@/shared/components/PanneauBas.vue';

/**
 * Mois affiché : flèches pour le mois voisin, et le titre ouvre un choix direct (année puis mois) pour revenir vite loin
 * dans l'historique. Aucun mois après `max` (par défaut le mois en cours).
 */
const props = withDefaults(defineProps<{ mois: Date; max?: Date }>(), { max: () => startOfMonth(new Date()) });
const emit = defineEmits<{ changer: [mois: Date] }>();

const NOMS = ['janv.', 'févr.', 'mars', 'avr.', 'mai', 'juin', 'juil.', 'août', 'sept.', 'oct.', 'nov.', 'déc.'];

const choixOuvert = ref(false);
const annee = ref(props.mois.getFullYear());

const titre = (mois: Date) => {
  const texte = format(mois, 'MMMM yyyy', { locale: fr });
  return texte.charAt(0).toUpperCase() + texte.slice(1);
};
const avantLeMax = (mois: Date) => startOfMonth(mois) <= startOfMonth(props.max);
const estChoisi = (index: number) => props.mois.getFullYear() === annee.value && props.mois.getMonth() === index;

function ouvrir() {
  annee.value = props.mois.getFullYear();
  choixOuvert.value = true;
}

function choisir(index: number) {
  choixOuvert.value = false;
  emit('changer', new Date(annee.value, index, 1));
}
</script>
