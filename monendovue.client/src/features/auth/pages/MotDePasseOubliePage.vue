<template>
  <PageCompte titre="Mot de passe oublié" sous-titre="Saisis ton adresse : tu recevras un lien pour choisir un nouveau mot de passe.">
    <form v-if="!envoye" class="flex flex-col gap-4" novalidate @submit.prevent="envoyer">
      <ChampTexte v-model="email" label="Adresse e-mail" name="email" type="email" inputmode="email" autocomplete="email" :erreur="erreurEmail"/>
      <MessageCompte v-if="erreurGenerale" ton="erreur">{{ erreurGenerale }}</MessageCompte>
      <button type="submit" :disabled="envoiEnCours"
              class="inline-flex min-h-[54px] items-center justify-center rounded-[16px] bg-button text-base font-semibold text-texte disabled:opacity-70">
        {{ envoiEnCours ? 'Envoi…' : 'Recevoir le lien' }}
      </button>
    </form>

    <template v-else>
      <MessageCompte ton="info">
        Si un compte existe pour <strong class="break-all">{{ email.trim() }}</strong>, un lien vient de partir. Il est valable 1 heure.
        Pense à regarder dans les courriers indésirables.
      </MessageCompte>
      <router-link to="/login" class="inline-flex min-h-11 items-center justify-center font-medium !text-lien">Retour à la connexion</router-link>
    </template>
  </PageCompte>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import ChampTexte from '../components/ChampTexte.vue';
import MessageCompte from '../components/MessageCompte.vue';
import PageCompte from '../components/PageCompte.vue';
import { comptesService } from '../services/comptesService';
import { emailDeLaNavigation } from '../utils/lienEmail';
import { emailPlausible } from '../utils/motDePasse';

/** Demande d'un lien de réinitialisation ; la réponse ne dit jamais si l'adresse a un compte. */
const email = ref(emailDeLaNavigation());
const envoiEnCours = ref(false);
const envoye = ref(false);
const erreurEmail = ref<string | null>(null);
const erreurGenerale = ref<string | null>(null);

async function envoyer() {
  if (envoiEnCours.value) return;
  erreurGenerale.value = null;
  erreurEmail.value = emailPlausible(email.value) ? null : 'Saisis une adresse e-mail valide.';
  if (erreurEmail.value) return;

  envoiEnCours.value = true;
  const resultat = await comptesService.motDePasseOublie(email.value.trim());
  envoiEnCours.value = false;
  if (resultat.ok) envoye.value = true;
  else erreurGenerale.value = resultat.message;
}
</script>
