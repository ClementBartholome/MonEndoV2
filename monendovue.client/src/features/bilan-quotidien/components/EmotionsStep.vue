<script setup lang="ts">
import { computed } from 'vue';
import { EMOTIONS_MAX, emotions } from '@/features/bilan-quotidien/config/emotions';
import type { CodeEmotion } from '@/features/bilan-quotidien/types/bilan-quotidien';

const selection = defineModel<CodeEmotion[]>({ required: true });

const complet = computed(() => selection.value.length >= EMOTIONS_MAX);

const basculer = (code: CodeEmotion) => {
  if (selection.value.includes(code)) {
    selection.value = selection.value.filter((c) => c !== code);
  } else if (!complet.value) {
    selection.value = [...selection.value, code];
  }
};
</script>

<template>
  <div>
    <h2 class="text-2xl font-bold mb-2 flex items-center justify-center">
      <i class="material-symbols-outlined mr-2">mood</i>Émotions du jour
    </h2>
    <p class="text-center text-paragraph mb-6" aria-live="polite">
      Choisis jusqu'à {{ EMOTIONS_MAX }} émotions ({{ selection.length }}/{{ EMOTIONS_MAX }})
    </p>
    <div class="flex flex-wrap justify-center gap-2" role="group" aria-label="Émotions du jour">
      <button
          v-for="emotion in emotions"
          :key="emotion.code"
          type="button"
          :aria-pressed="selection.includes(emotion.code)"
          :disabled="complet && !selection.includes(emotion.code)"
          class="min-h-11 flex items-center gap-2 rounded-full border-2 px-4 py-2 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-button disabled:cursor-not-allowed disabled:opacity-40"
          :class="selection.includes(emotion.code)
            ? 'border-button bg-button/10 font-semibold text-headline'
            : 'border-gray-200 bg-white text-paragraph hover:border-button/50'"
          @click="basculer(emotion.code)"
      >
        <span class="text-2xl" aria-hidden="true">{{ emotion.emoji }}</span>
        <span>{{ emotion.libelle }}</span>
      </button>
    </div>
  </div>
</template>
