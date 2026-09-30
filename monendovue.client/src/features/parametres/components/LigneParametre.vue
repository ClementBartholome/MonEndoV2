<template>
  <button type="button" class="flex min-h-16 w-full items-center gap-3 px-3.5 py-2 text-left text-texte disabled:opacity-60" @click="emit('ouvrir')">
    <i class="material-symbols-outlined rounded-controle p-1.5 text-titre-2" :class="teintesRubrique[teinte]" aria-hidden="true">{{ icone }}</i>
    <span class="flex min-w-0 grow flex-col">
      <span class="text-corps font-medium">{{ titre }}</span>
      <span class="text-legende text-texte-3">{{ detail }}</span>
    </span>
    <i class="material-symbols-outlined text-texte-3" aria-hidden="true">{{ chevron ? 'chevron_right' : '' }}</i>
  </button>
</template>

<script setup lang="ts">
import { teintesRubrique } from '@/shared/config/navigation';

/** Une ligne d'un groupe de réglages : tuile d'icône, titre, détail ; elle ouvre un panneau ou lance une action. */
withDefaults(defineProps<{
  titre: string;
  detail: string;
  icone: string;
  teinte?: keyof typeof teintesRubrique;
  /** Chevron « ouvre un panneau » ; absent pour une action directe (déconnexion, téléchargement). */
  chevron?: boolean;
}>(), { teinte: 'neutre', chevron: true });

const emit = defineEmits<{ ouvrir: [] }>();
</script>
