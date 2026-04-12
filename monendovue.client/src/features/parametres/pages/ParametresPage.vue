<template>
  <div class="flex-column-container">
    <BackButton/>
    <h2 class="text-2xl mr-auto ml-4">Paramètres</h2>
    <section
        class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
      <div class="flex gap-2 mb-4 items-center">
        <i class="material-symbols-outlined">flag</i>
        <h3 class="text-headline text-2xl">Objectifs bien-être</h3>
      </div>
      <p class="text-sm text-muted-foreground mb-4">Ces objectifs alimentent la section Analyse & Tendances du Bilan quotidien.</p>
      <hr class="mb-4 border-gray-300">

      <form class="grid grid-cols-1 md:grid-cols-2 gap-4" @submit.prevent="saveGoals">
        <div>
          <label class="text-sm font-medium text-headline">Hydratation cible (L/jour)</label>
          <input v-model.number="goals.hydrationLitersGoal" type="number" min="0.5" max="5" step="0.1" class="mt-1 w-full rounded-md border border-input px-3 py-2" />
        </div>
        <div>
          <label class="text-sm font-medium text-headline">Pas cibles (par jour)</label>
          <input v-model.number="goals.stepsGoal" type="number" min="1000" max="30000" step="500" class="mt-1 w-full rounded-md border border-input px-3 py-2" />
        </div>
        <div>
          <label class="text-sm font-medium text-headline">Stress max (/5)</label>
          <input v-model.number="goals.stressMaxGoal" type="number" min="1" max="5" step="0.5" class="mt-1 w-full rounded-md border border-input px-3 py-2" />
        </div>
        <div>
          <label class="text-sm font-medium text-headline">Fatigue max (/5)</label>
          <input v-model.number="goals.fatigueMaxGoal" type="number" min="1" max="5" step="0.5" class="mt-1 w-full rounded-md border border-input px-3 py-2" />
        </div>
        <div>
          <label class="text-sm font-medium text-headline">Douleur max (/10)</label>
          <input v-model.number="goals.painMaxGoal" type="number" min="1" max="10" step="1" class="mt-1 w-full rounded-md border border-input px-3 py-2" />
        </div>

        <div class="md:col-span-2 flex gap-2 justify-end mt-2">
          <Button type="button" variant="outline" @click="resetGoals">Réinitialiser</Button>
          <Button type="submit" variant="custom">Enregistrer</Button>
        </div>
      </form>
    </section>

    <section
        class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
      <div>
      </div>
      <NotificationSettings/>
    </section>
    <section
        class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
      <div class=" flex gap-2 mb-4 items-center">
        <i class="material-symbols-outlined">person</i>
        <h3 class="text-headline text-2xl">Compte utilisateur</h3>
      </div>
      <hr class="mb-4 border-gray-300">      
      <ChangePassword/>
    </section>
  </div>
</template>

<script setup lang="ts">

import BackButton from "@/shared/components/BackButton.vue";
import NotificationSettings from "@/features/parametres/components/NotificationSettings.vue";
import ChangePassword from "@/features/auth/components/ChangePassword.vue";
import { Button } from '@/shared/components/ui/button';
import { ref } from 'vue';
import { useToast } from '@/shared/components/ui/toast';
import {
  DEFAULT_WELLBEING_GOALS,
  getWellbeingGoals,
  saveWellbeingGoals,
  type WellbeingGoals,
} from '@/shared/services/wellbeingGoalsStorage';

const { toast } = useToast();
const goals = ref<WellbeingGoals>({ ...getWellbeingGoals() });

const saveGoals = () => {
  saveWellbeingGoals(goals.value);
  toast({
    title: 'Objectifs enregistrés',
    description: 'Les nouveaux objectifs seront utilisés dans Analyse & Tendances.',
    variant: 'custom'
  });
};

const resetGoals = () => {
  goals.value = { ...DEFAULT_WELLBEING_GOALS };
};

</script>

<style scoped>

</style>