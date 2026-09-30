<script setup lang="ts">
import { computed } from 'vue';
import { useId } from 'radix-vue';
import { Button } from '@/shared/components/ui/button';
import { Input } from '@/shared/components/ui/input';
import type { Prereglage } from '@/features/bilan-quotidien/config/saisie';

// Mesure facultative : un tap sur un préréglage, ou une valeur libre. Retaper le préréglage choisi l'efface.
const props = defineProps<{
  libelle: string;
  icone: string;
  unite: string;
  prereglages: Prereglage[];
  max: number;
  pas?: number;
}>();

const valeur = defineModel<number | null>({ required: true });

const idChamp = useId(undefined, 'mesure');

const choisir = (prereglage: Prereglage) => {
  valeur.value = valeur.value === prereglage.valeur ? null : prereglage.valeur;
};

const saisieLibre = computed({
  get: () => (valeur.value === null ? '' : String(valeur.value)),
  set: (texte: string | number) => {
    const nombre = Number(String(texte).replace(',', '.'));
    if (String(texte).trim() === '' || Number.isNaN(nombre)) {
      valeur.value = null;
      return;
    }
    const borne = Math.min(Math.max(nombre, 0), props.max);
    // Sans pas décimal, la mesure est entière (nombre de pas).
    valeur.value = props.pas ? borne : Math.round(borne);
  },
});
</script>

<template>
  <div class="flex flex-col gap-2">
    <p class="font-semibold text-texte flex items-center gap-2">
      <i class="material-symbols-outlined text-teinte-bilan" aria-hidden="true">{{ icone }}</i>{{ libelle }}
    </p>
    <div class="grid gap-2" :style="{ gridTemplateColumns: `repeat(${prereglages.length}, minmax(0, 1fr))` }">
      <Button
          v-for="prereglage in prereglages"
          :key="prereglage.valeur"
          type="button"
          class="h-auto min-h-[48px] px-1 py-1 flex flex-col gap-0.5 leading-tight"
          :variant="valeur === prereglage.valeur ? 'selected' : 'outline'"
          :aria-pressed="valeur === prereglage.valeur"
          @click="choisir(prereglage)"
      >
        <span>{{ prereglage.libelle }}</span>
        <span v-if="prereglage.detail" class="text-[0.7rem] font-normal opacity-80">{{ prereglage.detail }}</span>
      </Button>
    </div>
    <div class="flex items-center gap-2">
      <label :for="idChamp" class="text-sm text-texte-2 shrink-0">Ou valeur exacte</label>
      <Input
          :id="idChamp"
          v-model="saisieLibre"
          type="number"
          inputmode="decimal"
          min="0"
          :max="max"
          :step="pas ?? 1"
          class="h-11 w-28"
      />
      <span class="text-sm text-texte-2">{{ unite }}</span>
    </div>
  </div>
</template>
