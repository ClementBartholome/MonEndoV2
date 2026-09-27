<template>
  <label class="case-consentement font-text flex items-start gap-3 text-sm text-left text-paragraph cursor-pointer">
    <input v-model="accepte" type="checkbox" name="consentementDonneesSante" class="mt-1 h-5 w-5 shrink-0 cursor-pointer"/>
    <span>
      J'accepte que MonEndo enregistre les données de santé que je note, uniquement pour mon suivi, comme le décrit la
      <router-link to="/confidentialite" target="_blank" class="underline">politique de confidentialité</router-link>.
      Je peux retirer cet accord à tout moment en supprimant mon compte.
    </span>
  </label>
</template>

<script setup lang="ts">
/** Consentement explicite aux données de santé (RGPD, art. 9.2.a) : case jamais cochée par défaut. */
const accepte = defineModel<boolean>({required: true});
</script>

<style scoped>
.case-consentement input {
  accent-color: var(--button);
}
</style>
