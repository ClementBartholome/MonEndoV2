<template>
  <DialogRoot v-model:open="ouvert">
    <DialogPortal>
      <DialogOverlay class="panneau-voile fixed inset-0 z-50 bg-voile"/>
      <DialogContent
          class="panneau fixed inset-x-0 bottom-0 z-50 mx-auto flex max-h-[90dvh] w-full max-w-lg flex-col gap-4 overflow-y-auto rounded-t-3xl bg-fond px-5 pb-6 pt-2 shadow-elevation focus:outline-none"
          :aria-describedby="undefined">
        <div aria-hidden="true" class="mx-auto h-1 w-10 shrink-0 rounded-full bg-trait"></div>
        <div class="flex items-center justify-between">
          <DialogTitle class="text-xl font-semibold text-texte">{{ titre }}</DialogTitle>
          <DialogClose aria-label="Fermer" class="-mr-2 flex h-11 w-11 items-center justify-center rounded-full text-texte hover:bg-surface-2">
            <i class="material-symbols-outlined" aria-hidden="true">close</i>
          </DialogClose>
        </div>
        <slot/>
      </DialogContent>
    </DialogPortal>
  </DialogRoot>
</template>

<script setup lang="ts">
import {DialogClose, DialogContent, DialogOverlay, DialogPortal, DialogRoot, DialogTitle} from 'radix-vue';

/**
 * Panneau qui monte du bas de l'écran (mobile d'abord) : menu « Plus », formulaires de saisie rapide.
 * Fenêtre modale accessible (focus piégé, Échap et clic sur le voile pour fermer).
 */
defineProps<{ titre: string }>();
const ouvert = defineModel<boolean>('open', {required: true});
</script>

<style scoped>
.panneau[data-state='open'] {
  animation: monter 0.2s ease-out;
}

.panneau-voile[data-state='open'] {
  animation: apparaitre 0.2s ease-out;
}

@keyframes monter {
  from { transform: translateY(100%); }
  to { transform: translateY(0); }
}

@keyframes apparaitre {
  from { opacity: 0; }
  to { opacity: 1; }
}

@media (prefers-reduced-motion: reduce) {
  .panneau[data-state='open'],
  .panneau-voile[data-state='open'] {
    animation: none;
  }
}
</style>
