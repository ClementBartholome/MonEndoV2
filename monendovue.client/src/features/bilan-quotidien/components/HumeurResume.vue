<script setup lang="ts">
import { computed } from 'vue';
import { anciennesHumeurs, presentationEmotion } from '@/features/bilan-quotidien/config/emotions';
import { emotionsDuBilan } from '@/features/bilan-quotidien/utils/humeur';
import type { BilanQuotidien } from '@/features/bilan-quotidien/types/bilan-quotidien';

// Émotions d'un bilan, ou humeur d'un bilan saisi avant les émotions.
const props = defineProps<{ bilan: Pick<BilanQuotidien, 'mood' | 'emotions'> }>();

const emotions = computed(() => emotionsDuBilan(props.bilan).map((code) => presentationEmotion[code]));
const ancienne = computed(() => (props.bilan.mood ? anciennesHumeurs[props.bilan.mood] : undefined));
</script>

<template>
  <div class="bg-white rounded-xl border border-gray-100 p-3">
    <p class="text-xs text-muted-foreground flex items-center gap-1">
      <i class="material-symbols-outlined text-2xl leading-6">{{ !emotions.length && ancienne ? ancienne.icone : 'mood' }}</i>
      {{ emotions.length ? 'Émotions' : 'Humeur' }}
    </p>
    <ul v-if="emotions.length" class="flex flex-wrap gap-2 mt-2">
      <li
          v-for="emotion in emotions"
          :key="emotion.code"
          class="flex items-center gap-1 rounded-full bg-button/10 px-3 py-1 text-sm font-medium text-headline"
      >
        <span aria-hidden="true">{{ emotion.emoji }}</span>{{ emotion.libelle }}
      </li>
    </ul>
    <p v-else class="text-base font-semibold text-headline mt-1">{{ ancienne?.libelle ?? '-' }}</p>
  </div>
</template>
