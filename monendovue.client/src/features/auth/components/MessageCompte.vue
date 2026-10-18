<template>
  <p :role="ton === 'erreur' ? 'alert' : 'status'"
     class="m-0 flex items-start gap-3 rounded-carte bg-surface p-3.5 text-left text-sm leading-normal text-texte shadow-elevation">
    <i class="material-symbols-outlined rounded-controle p-1.5" :class="ton === 'erreur' ? 'bg-teinte-douleur-fond text-teinte-douleur' : 'bg-teinte-traitement-fond text-teinte-traitement'"
       aria-hidden="true">{{ ton === 'erreur' ? 'error' : 'mark_email_read' }}</i>
    <span><slot/></span>
  </p>
</template>

<script setup lang="ts">
/** Message d'information ou d'erreur des écrans de compte (annoncé par un lecteur d'écran). */
defineProps<{ ton: 'info' | 'erreur' }>();
</script>
