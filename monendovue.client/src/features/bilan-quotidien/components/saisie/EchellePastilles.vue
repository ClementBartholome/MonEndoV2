<script setup lang="ts">
import { computed } from 'vue';
import { useId } from 'radix-vue';
import { couleursIntensite } from '@/shared/utils/intensite';

/**
 * Échelle de 0 à max : un tap choisit, un second tap sur la même valeur efface (non renseigné).
 * `couleurs` (douleur) : la valeur choisie prend sa couleur d'intensité, comme dans la saisie d'une douleur ;
 * sinon (fatigue, stress), le fond foncé commun à tous les choix de formulaire.
 */
const props = defineProps<{
  libelle: string;
  icone: string;
  max: number;
  repereMin: string;
  repereMax: string;
  couleurs?: boolean;
}>();

const valeur = defineModel<number | null>({ required: true });

const idLibelle = useId(undefined, 'echelle');
const niveaux = computed(() => Array.from({ length: props.max + 1 }, (_, i) => i));

const choisir = (niveau: number) => {
  valeur.value = valeur.value === niveau ? null : niveau;
};

function style(niveau: number) {
  if (valeur.value !== niveau || !props.couleurs) return undefined;
  return couleursIntensite(niveau);
}

function classes(niveau: number): string {
  if (valeur.value !== niveau) return 'border-contour bg-surface text-texte-2';
  return props.couleurs ? 'font-semibold' : 'border-texte bg-texte font-semibold text-fond';
}
</script>

<template>
  <div class="flex flex-col gap-2">
    <div class="flex items-center justify-between gap-2">
      <p :id="idLibelle" class="m-0 flex items-center gap-2 text-corps font-semibold text-texte">
        <i class="material-symbols-outlined text-teinte-bilan" aria-hidden="true">{{ icone }}</i>{{ libelle }}
      </p>
      <span class="text-sm text-texte-2" aria-hidden="true">
        {{ valeur === null ? '—' : `${valeur}/${max}` }}
      </span>
    </div>
    <div
        role="group"
        :aria-labelledby="idLibelle"
        class="grid gap-1.5"
        :class="max > 5 ? 'grid-cols-6 sm:grid-cols-11' : 'grid-cols-6'"
    >
      <button
          v-for="niveau in niveaux"
          :key="niveau"
          type="button"
          class="relative min-h-11 overflow-hidden rounded-controle border-[1.5px] text-corps"
          :class="classes(niveau)"
          :style="style(niveau)"
          :aria-pressed="valeur === niveau"
          :aria-label="`${libelle} ${niveau} sur ${max}`"
          @click="choisir(niveau)"
      >
        {{ niveau }}
        <!-- Comme ChoixIntensite : un choix non sélectionné garde un trait de sa couleur pour lire l'échelle. -->
        <span v-if="couleurs && valeur !== niveau" aria-hidden="true" class="absolute inset-x-0 bottom-0 h-1" :style="{ background: `var(--intensite-${niveau})` }"></span>
      </button>
    </div>
    <div class="flex justify-between text-xs text-texte-3" aria-hidden="true">
      <span>{{ repereMin }}</span>
      <span>{{ repereMax }}</span>
    </div>
  </div>
</template>
