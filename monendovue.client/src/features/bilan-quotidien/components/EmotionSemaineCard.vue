<script setup lang="ts">
import { computed } from 'vue';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { presentationEmotion } from '@/features/bilan-quotidien/config/emotions';
import { emotionDeLaSemaine } from '@/features/bilan-quotidien/utils/humeur';
import type { BilanQuotidien } from '@/features/bilan-quotidien/types/bilan-quotidien';
import type { ModePeriode } from '@/features/bilan-quotidien/types/historique';

const props = defineProps<{ bilans: BilanQuotidien[]; mode: ModePeriode }>();

const periode = computed(() => (props.mode === 'mois' ? { de: 'du mois', cette: 'ce mois-ci' } : { de: 'de la semaine', cette: 'cette semaine' }));

const semaine = computed(() => emotionDeLaSemaine(props.bilans));
</script>

<template>
  <Card class="flex w-full flex-col rounded-carte border-0 bg-surface shadow-elevation">
    <CardHeader class="p-4 md:p-6">
      <CardTitle class="m-0 flex items-center gap-2 text-titre-carte font-semibold leading-tight tracking-normal text-texte">
        <i class="material-symbols-outlined rounded-controle bg-teinte-symptome-fond p-1.5 text-titre-2 text-teinte-symptome" aria-hidden="true">mood</i>Émotion {{ periode.de }}
      </CardTitle>
    </CardHeader>
    <CardContent class="px-4 pb-4 md:px-6 md:pb-6 text-left">
      <div v-if="semaine" class="flex items-center gap-4">
        <span class="text-5xl" aria-hidden="true">{{ semaine.emoji }}</span>
        <div class="flex flex-col gap-2 min-w-0">
          <p class="text-base font-semibold text-texte">{{ semaine.libelle }}</p>
          <ul v-if="semaine.principales.length" class="flex flex-wrap gap-2" aria-label="Émotions les plus fréquentes">
            <li
                v-for="code in semaine.principales"
                :key="code"
                class="inline-flex items-center gap-1 rounded-controle bg-surface-2 px-2 py-1 text-xs text-texte"
            >
              <span aria-hidden="true">{{ presentationEmotion[code].emoji }}</span>{{ presentationEmotion[code].libelle }}
            </li>
          </ul>
          <p class="text-sm text-texte-3">
            D'après {{ semaine.nombreBilans }} bilan{{ semaine.nombreBilans > 1 ? 's' : '' }} {{ periode.de }}
          </p>
        </div>
      </div>
      <p v-else class="text-texte-2">Aucune émotion renseignée {{ periode.cette }}.</p>
    </CardContent>
  </Card>
</template>
