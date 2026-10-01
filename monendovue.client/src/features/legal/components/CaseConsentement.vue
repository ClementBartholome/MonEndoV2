<template>
  <div class="flex flex-col gap-2 text-left">
    <label class="case-consentement flex cursor-pointer items-start gap-3 rounded-carte bg-surface p-3.5 text-corps leading-normal text-texte shadow-elevation"
           :class="erreur ? 'border-2 border-danger' : ''">
      <input v-model="accepte" type="checkbox" name="consentementDonneesSante" class="mt-0.5 h-6 w-6 shrink-0 cursor-pointer"
             :aria-invalid="erreur ? 'true' : undefined" :aria-describedby="erreur ? 'erreur-consentement' : undefined"/>
      <span>
        J'accepte que MonEndo enregistre les données de santé que je note, uniquement pour mon suivi, comme le décrit la
        <router-link to="/confidentialite" target="_blank" rel="noopener" class="underline">politique de confidentialité</router-link>.
        Je peux retirer cet accord à tout moment en supprimant mon compte.
      </span>
    </label>
    <p v-if="erreur" id="erreur-consentement" role="alert" class="m-0 flex items-center gap-2 text-sm text-danger">
      <i class="material-symbols-outlined text-xl" aria-hidden="true">error</i>{{ erreur }}
    </p>
  </div>
</template>

<script setup lang="ts">
/** Consentement explicite aux données de santé (RGPD, art. 9.2.a) : case jamais cochée par défaut ; la politique s'ouvre dans un nouvel onglet pour ne rien perdre de la saisie. */
const accepte = defineModel<boolean>({required: true});
defineProps<{ erreur?: string | null }>();
</script>

<style scoped>
.case-consentement input {
  accent-color: var(--button);
}
</style>
