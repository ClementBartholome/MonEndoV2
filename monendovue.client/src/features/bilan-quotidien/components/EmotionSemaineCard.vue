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
    <CardHeader>
      <CardTitle class="flex items-center">
        <i class="material-symbols-outlined mr-2">mood</i> Émotion de la semaine
      </CardTitle>
    </CardHeader>
    <CardContent>
      <div v-if="semaine" class="flex items-center gap-4">
        <span class="text-5xl" aria-hidden="true">{{ semaine.emoji }}</span>
        <div class="flex flex-col gap-2 min-w-0">
          <p class="text-lg font-semibold text-headline">{{ semaine.libelle }}</p>
          <ul v-if="semaine.principales.length" class="flex flex-wrap gap-2" aria-label="Émotions les plus fréquentes">
            <li
                v-for="code in semaine.principales"
                :key="code"
                class="flex items-center gap-1 rounded-full bg-button/10 px-3 py-1 text-sm text-headline"
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
