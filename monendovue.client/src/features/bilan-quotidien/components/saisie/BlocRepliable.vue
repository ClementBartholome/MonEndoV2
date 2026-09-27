<script setup lang="ts">
import { useId } from 'radix-vue';

// Bloc facultatif de la saisie : replié, il affiche un résumé de ce qui est renseigné.
defineProps<{
  titre: string;
  icone: string;
  resume: string;
}>();

const ouvert = defineModel<boolean>('ouvert', { required: true });

const idContenu = useId(undefined, 'bloc');
</script>

<template>
  <section class="rounded-2xl border border-gray-200 bg-white/70">
    <button
        type="button"
        class="w-full flex items-center gap-3 px-3 py-3 text-left min-h-[56px] rounded-2xl focus-visible:outline focus-visible:outline-2 focus-visible:outline-[var(--button)]"
        :aria-expanded="ouvert"
        :aria-controls="idContenu"
        @click="ouvert = !ouvert"
    >
      <i class="material-symbols-outlined text-button" aria-hidden="true">{{ icone }}</i>
      <span class="flex flex-col min-w-0 flex-1">
        <span class="font-semibold text-headline">{{ titre }} <span class="font-normal text-sm text-paragraph">(facultatif)</span></span>
        <span v-if="!ouvert" class="text-sm text-paragraph break-words">{{ resume }}</span>
      </span>
      <i class="material-symbols-outlined text-paragraph transition-transform" :class="ouvert ? 'rotate-180' : ''" aria-hidden="true">
        expand_more
      </i>
    </button>
    <div v-show="ouvert" :id="idContenu" class="px-3 pb-4 pt-1 flex flex-col gap-5 border-t border-gray-100">
      <slot/>
    </div>
  </section>
</template>
