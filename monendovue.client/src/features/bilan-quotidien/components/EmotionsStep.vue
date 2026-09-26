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
  <div>
    <!-- Icône de titre à la taille des autres étapes (le CSS scoped de la page ne s'applique pas ici). -->
    <h2 class="text-2xl font-bold mb-2 flex items-center justify-center">
      <i class="material-symbols-outlined mr-2 text-[3.2rem] leading-none">mood</i>Émotions du jour
    </h2>
    <p class="text-center text-paragraph mb-6" aria-live="polite">
      Choisis jusqu'à {{ EMOTIONS_MAX }} émotions ({{ selection.length }}/{{ EMOTIONS_MAX }})
    </p>
    <div class="flex flex-wrap justify-center gap-2" role="group" aria-label="Émotions du jour">
      <Button
          v-for="emotion in emotions"
          :key="emotion.code"
          type="button"
          class="h-11 gap-2 rounded-full px-4"
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
