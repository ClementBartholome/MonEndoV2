<script setup lang="ts">
import { Button } from '@/shared/components/ui/button';
import { useId } from 'radix-vue';

/**
 * Question facultative à réponse Oui / Non : null = non renseigné. Re-toucher la réponse choisie la retire.
 * La série de boutons porte le nom de la question pour un lecteur d'écran.
 */
defineProps<{ libelle: string; aide?: string }>();
const reponse = defineModel<boolean | null>({ required: true });

const idQuestion = useId(undefined, 'question');
const choix = [
  { valeur: true, libelle: 'Oui' },
  { valeur: false, libelle: 'Non' },
];
</script>

<template>
  <section class="flex flex-col gap-2">
    <h3 :id="idQuestion" class="m-0 text-sm font-medium text-texte">{{ libelle }}</h3>
    <p v-if="aide" class="m-0 text-xs text-texte-2">{{ aide }}</p>
    <div role="group" :aria-labelledby="idQuestion" class="grid grid-cols-2 gap-2">
      <Button v-for="option in choix" :key="option.libelle" type="button" class="h-11"
              :variant="reponse === option.valeur ? 'selected' : 'outline'" :aria-pressed="reponse === option.valeur"
              @click="reponse = reponse === option.valeur ? null : option.valeur">
        {{ option.libelle }}
      </Button>
    </div>
    <slot/>
  </section>
</template>
