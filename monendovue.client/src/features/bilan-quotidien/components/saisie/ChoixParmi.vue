<script setup lang="ts">
import { Button } from '@/shared/components/ui/button';
import { useId } from 'radix-vue';
import type { OptionChoix } from '@/features/bilan-quotidien/config/questions';

/** Choix unique parmi quelques réponses (facultatif) : null = non renseigné, re-toucher le choix le retire. */
withDefaults(defineProps<{ libelle: string; aide?: string; options: OptionChoix[]; titreNiveau?: 'h3' | 'p' }>(), { titreNiveau: 'h3' });
const choisi = defineModel<string | null>({ required: true });

const idQuestion = useId(undefined, 'choix');
</script>

<template>
  <section class="flex flex-col gap-2">
    <component :is="titreNiveau" :id="idQuestion" class="m-0 text-sm font-medium text-texte">{{ libelle }}</component>
    <p v-if="aide" class="m-0 text-xs text-texte-2">{{ aide }}</p>
    <div role="group" :aria-labelledby="idQuestion" class="grid grid-cols-3 gap-2">
      <Button v-for="option in options" :key="option.valeur" type="button" class="h-auto min-h-11 whitespace-normal px-2 py-1.5 text-center leading-tight"
              :variant="choisi === option.valeur ? 'selected' : 'outline'" :aria-pressed="choisi === option.valeur"
              @click="choisi = choisi === option.valeur ? null : option.valeur">
        {{ option.libelle }}
      </Button>
    </div>
  </section>
</template>
