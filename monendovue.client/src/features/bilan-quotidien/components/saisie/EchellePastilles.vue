<script setup lang="ts">
import { computed } from 'vue';
import { useId } from 'radix-vue';

// Échelle de 0 à max en pastilles : un tap choisit, un second tap sur la même pastille efface (non renseigné).
const props = defineProps<{
  libelle: string;
  icone: string;
  max: number;
  repereMin: string;
  repereMax: string;
}>();

const valeur = defineModel<number | null>({ required: true });

const idLibelle = useId(undefined, 'echelle');
const pastilles = computed(() => Array.from({ length: props.max + 1 }, (_, i) => i));

const choisir = (niveau: number) => {
  valeur.value = valeur.value === niveau ? null : niveau;
};
</script>

<template>
  <div class="flex flex-col gap-2">
    <div class="flex items-center justify-between gap-2">
      <p :id="idLibelle" class="font-semibold text-headline flex items-center gap-2">
        <i class="material-symbols-outlined text-button" aria-hidden="true">{{ icone }}</i>{{ libelle }}
      </p>
      <span class="text-sm text-paragraph" aria-hidden="true">
        {{ valeur === null ? '—' : `${valeur}/${max}` }}
      </span>
    </div>
    <div
        role="group"
        :aria-labelledby="idLibelle"
        class="pastilles"
        :class="max > 5 ? 'pastilles--longue' : ''"
        :style="{ '--colonnes': max + 1 }"
    >
      <button
          v-for="niveau in pastilles"
          :key="niveau"
          type="button"
          class="pastille"
          :class="valeur === niveau ? 'pastille--choisie' : ''"
          :aria-pressed="valeur === niveau"
          :aria-label="`${libelle} ${niveau} sur ${max}`"
          @click="choisir(niveau)"
      >
        {{ niveau }}
      </button>
    </div>
    <div class="flex justify-between text-xs text-paragraph" aria-hidden="true">
      <span>{{ repereMin }}</span>
      <span>{{ repereMax }}</span>
    </div>
  </div>
</template>

<style scoped>
.pastilles {
  display: grid;
  grid-template-columns: repeat(var(--colonnes), minmax(0, 1fr));
  gap: 0.375rem;
}

/* 11 pastilles ne tiennent pas en 44px sur 375px : deux rangées (0-5, 6-10) sur petit écran. */
@media (max-width: 639px) {
  .pastilles--longue {
    grid-template-columns: repeat(6, minmax(0, 1fr));
  }
}

.pastille {
  min-height: 44px;
  border-radius: 9999px;
  border: 2px solid rgb(229 231 235);
  background: white;
  color: var(--headline);
  font-weight: 600;
  transition: background-color 0.15s, border-color 0.15s, transform 0.1s;
}

.pastille:hover {
  border-color: var(--button);
}

.pastille:focus-visible {
  outline: 2px solid var(--button);
  outline-offset: 2px;
}

.pastille:active {
  transform: scale(0.95);
}

.pastille--choisie {
  background: var(--button);
  border-color: var(--button);
  color: var(--button-text);
}
</style>
