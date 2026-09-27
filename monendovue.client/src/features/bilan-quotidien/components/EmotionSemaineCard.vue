<script setup lang="ts">
import { computed } from 'vue';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { presentationEmotion } from '@/features/bilan-quotidien/config/emotions';
import { emotionDeLaSemaine } from '@/features/bilan-quotidien/utils/humeur';
import type { BilanQuotidien } from '@/features/bilan-quotidien/types/bilan-quotidien';

const props = defineProps<{ bilans: BilanQuotidien[] }>();

const semaine = computed(() => emotionDeLaSemaine(props.bilans));
</script>

<template>
  <Card class="container !mx-0 mt-4 w-full bg-clearer rounded-3xl shadow-xl ml-auto flex flex-col">
    <CardHeader class="p-4 md:p-6">
      <CardTitle class="m-0 text-lg leading-tight flex items-center gap-2">
        <i class="material-symbols-outlined" aria-hidden="true">mood</i>Émotion de la semaine
      </CardTitle>
    </CardHeader>
    <CardContent class="px-4 pb-4 md:px-6 md:pb-6 text-left">
      <div v-if="semaine" class="flex items-center gap-4">
        <span class="text-5xl" aria-hidden="true">{{ semaine.emoji }}</span>
        <div class="flex flex-col gap-2 min-w-0">
          <p class="text-base font-semibold text-headline">{{ semaine.libelle }}</p>
          <ul v-if="semaine.principales.length" class="flex flex-wrap gap-2" aria-label="Émotions les plus fréquentes">
            <li
                v-for="code in semaine.principales"
                :key="code"
                class="inline-flex items-center gap-1 px-2 py-1 rounded-full bg-pink-100 text-pink-700 text-xs"
            >
              <span aria-hidden="true">{{ presentationEmotion[code].emoji }}</span>{{ presentationEmotion[code].libelle }}
            </li>
          </ul>
          <p class="text-sm text-muted-foreground">
            D'après {{ semaine.nombreBilans }} bilan{{ semaine.nombreBilans > 1 ? 's' : '' }} de la semaine
          </p>
        </div>
      </div>
      <p v-else class="text-paragraph">Aucune émotion renseignée cette semaine.</p>
    </CardContent>
  </Card>
</template>
