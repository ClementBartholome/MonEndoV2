<template>
  <PageCompte titre="Vérifie ta boîte mail" icone="mark_email_unread" retour="/register"
              sous-titre="Il reste une étape : confirmer ton adresse e-mail.">
    <p class="m-0 text-left text-corps leading-normal text-texte">
      Nous avons envoyé un lien <template v-if="email">à <strong class="break-all">{{ email }}</strong></template> pour confirmer ton adresse.
      Ouvre-le pour activer ton compte : il est valable 24 heures.
    </p>
    <p class="m-0 text-left text-sm leading-normal text-texte-2">
      Rien reçu après quelques minutes ? Regarde dans les courriers indésirables, puis demande un nouveau lien.
    </p>

    <form class="flex flex-col gap-4" novalidate @submit.prevent="renvoyer">
      <ChampTexte v-if="!emailConnu" v-model="email" label="Adresse e-mail" name="email" type="email" inputmode="email" autocomplete="email"/>
      <button type="submit" :disabled="envoiEnCours || !email.trim()"
              class="inline-flex min-h-12 items-center justify-center rounded-controle border-[1.5px] border-contour px-4 font-medium text-texte disabled:opacity-60">
        {{ envoiEnCours ? 'Envoi…' : 'Renvoyer le lien' }}
      </button>
    </form>
    <MessageCompte v-if="message" :ton="erreur ? 'erreur' : 'info'">{{ message }}</MessageCompte>

    <router-link to="/login" class="inline-flex min-h-[54px] items-center justify-center rounded-[16px] bg-button text-base font-semibold !text-texte no-underline">
      J'ai confirmé, me connecter
    </router-link>
  </PageCompte>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import ChampTexte from '../components/ChampTexte.vue';
import MessageCompte from '../components/MessageCompte.vue';
import PageCompte from '../components/PageCompte.vue';
import { comptesService } from '../services/comptesService';
import { emailDeLaNavigation } from '../utils/lienEmail';

/** Après l'inscription : le compte attend la confirmation de l'adresse ; aucune session n'est ouverte. */
const email = ref(emailDeLaNavigation());
/** Adresse connue (venant de l'inscription) : pas besoin de la ressaisir pour renvoyer le lien. */
const emailConnu = email.value !== '';
const envoiEnCours = ref(false);
const message = ref<string | null>(null);
const erreur = ref(false);

async function renvoyer() {
  if (envoiEnCours.value || !email.value.trim()) return;
  envoiEnCours.value = true;
  const resultat = await comptesService.renvoyerConfirmation(email.value.trim());
  envoiEnCours.value = false;
  erreur.value = !resultat.ok;
  message.value = resultat.ok
    ? 'Si un compte attend une confirmation à cette adresse, un nouveau lien vient de partir.'
    : resultat.message;
}
</script>
