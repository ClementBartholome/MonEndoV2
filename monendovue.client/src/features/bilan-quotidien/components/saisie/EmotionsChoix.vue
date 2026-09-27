<script setup lang="ts">
import { computed } from 'vue';
import { Button } from '@/shared/components/ui/button';
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
  <div class="flex flex-col gap-2">
    <div class="flex items-center justify-between gap-2">
      <p id="libelle-emotions" class="font-semibold text-headline flex items-center gap-2">
        <i class="material-symbols-outlined text-button" aria-hidden="true">mood</i>Émotions du jour
      </p>
      <span class="text-sm text-paragraph" aria-live="polite">{{ selection.length }}/{{ EMOTIONS_MAX }}</span>
    </div>
    <div class="grid grid-cols-2 sm:grid-cols-3 gap-2" role="group" aria-labelledby="libelle-emotions">
      <Button
          v-for="emotion in emotions"
          :key="emotion.code"
          type="button"
          class="h-11 w-full justify-start gap-2 rounded-full px-3"
          :variant="selection.includes(emotion.code) ? 'selected' : 'outline'"
          :aria-pressed="selection.includes(emotion.code)"
          :disabled="complet && !selection.includes(emotion.code)"
          @click="basculer(emotion.code)"
      >
        <span class="text-lg" aria-hidden="true">{{ emotion.emoji }}</span>
        {{ emotion.libelle }}
      </Button>
    </div>
  </div>
</template>
