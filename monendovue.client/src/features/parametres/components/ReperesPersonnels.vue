<template>
  <form class="flex flex-col gap-4 pb-2" @submit.prevent="enregistrer">
    <p class="m-0 text-left text-sm text-texte-2">
      Des repères que tu choisis : « Tendances » du bilan quotidien indique combien de jours tu les atteins, sans note ni jugement.
    </p>
    <label v-for="champ in CHAMPS" :key="champ.cle" class="flex flex-col gap-1 text-left text-sm font-medium text-texte">
      {{ champ.libelle }}
      <input v-model.number="reperes[champ.cle]" type="number" :min="champ.min" :max="champ.max" :step="champ.pas"
             class="min-h-11 rounded-controle border-[1.5px] border-contour bg-champ px-3 text-corps font-normal text-texte">
    </label>
    <div class="flex gap-2">
      <button type="button" class="min-h-12 grow rounded-controle border-[1.5px] border-contour px-4 font-medium text-texte" @click="reinitialiser">Réinitialiser</button>
      <button type="submit" class="min-h-12 grow rounded-controle bg-button px-4 font-semibold text-texte">Enregistrer</button>
    </div>
  </form>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useToast } from '@/shared/components/ui/toast';
import {
  DEFAULT_WELLBEING_GOALS,
  getWellbeingGoals,
  saveWellbeingGoals,
  type WellbeingGoals,
} from '@/shared/services/wellbeingGoalsStorage';

const emit = defineEmits<{ enregistre: [] }>();
const { toast } = useToast();
const reperes = ref<WellbeingGoals>({ ...getWellbeingGoals() });

const CHAMPS: { cle: keyof WellbeingGoals; libelle: string; min: number; max: number; pas: number }[] = [
  { cle: 'hydrationLitersGoal', libelle: 'Hydratation (L par jour, au moins)', min: 0.5, max: 5, pas: 0.1 },
  { cle: 'stepsGoal', libelle: 'Pas (par jour, au moins)', min: 1000, max: 30000, pas: 500 },
  { cle: 'stressMaxGoal', libelle: 'Stress (sur 5, au plus)', min: 1, max: 5, pas: 0.5 },
  { cle: 'fatigueMaxGoal', libelle: 'Fatigue (sur 5, au plus)', min: 1, max: 5, pas: 0.5 },
  { cle: 'painMaxGoal', libelle: 'Douleur (sur 10, au plus)', min: 1, max: 10, pas: 1 },
];

function enregistrer() {
  saveWellbeingGoals(reperes.value);
  toast({ title: 'Repères enregistrés', description: 'Ils sont utilisés dans les Tendances du bilan quotidien.', variant: 'custom' });
  emit('enregistre');
}

const reinitialiser = () => {
  reperes.value = { ...DEFAULT_WELLBEING_GOALS };
};
</script>
