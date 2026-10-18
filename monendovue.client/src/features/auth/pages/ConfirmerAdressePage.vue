<template>
  <PageCompte :titre="titre" :icone="etat === 'confirmee' ? 'verified' : 'mark_email_unread'">
    <p v-if="etat === 'en-cours'" class="m-0 text-left text-corps text-texte-2" role="status" aria-busy="true">Vérification du lien…</p>

    <template v-else-if="etat === 'confirmee'">
      <p class="m-0 text-left text-corps leading-normal text-texte">Ton compte est activé. Tu peux maintenant te connecter.</p>
      <router-link :to="{ name: 'login', state: { email: lien?.email } }"
                   class="inline-flex min-h-[54px] items-center justify-center rounded-[16px] bg-button text-base font-semibold !text-texte no-underline">
        Me connecter
      </router-link>
    </template>

    <template v-else>
      <MessageCompte ton="erreur">{{ messageErreur }}</MessageCompte>
      <button v-if="lien" type="button" :disabled="renvoiEnCours"
              class="inline-flex min-h-12 items-center justify-center rounded-controle border-[1.5px] border-contour px-4 font-medium text-texte disabled:opacity-60"
              @click="renvoyer">
        {{ renvoiEnCours ? 'Envoi…' : 'Recevoir un nouveau lien' }}
      </button>
      <MessageCompte v-if="messageRenvoi" ton="info">{{ messageRenvoi }}</MessageCompte>
      <router-link to="/login" class="inline-flex min-h-11 items-center justify-center font-medium !text-lien">Aller à la connexion</router-link>
    </template>
  </PageCompte>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import MessageCompte from '../components/MessageCompte.vue';
import PageCompte from '../components/PageCompte.vue';
import { comptesService } from '../services/comptesService';
import { lireLienDuFragment } from '../utils/lienEmail';

/** Lien de confirmation reçu par e-mail : l'adresse et le jeton sont lus dans le fragment, puis envoyés au serveur. */
const lien = lireLienDuFragment();
const etat = ref<'en-cours' | 'confirmee' | 'erreur'>(lien ? 'en-cours' : 'erreur');
const messageErreur = ref("Ce lien est incomplet. Ouvre-le directement depuis l'e-mail reçu.");
const renvoiEnCours = ref(false);
const messageRenvoi = ref<string | null>(null);

const titre = computed(() => ({ 'en-cours': 'Confirmation de ton adresse', confirmee: 'Adresse confirmée', erreur: 'Confirmation impossible' })[etat.value]);

onMounted(async () => {
  if (!lien) return;
  const resultat = await comptesService.confirmerAdresse(lien.email, lien.jeton);
  if (resultat.ok) {
    etat.value = 'confirmee';
  } else {
    messageErreur.value = resultat.message;
    etat.value = 'erreur';
  }
});

async function renvoyer() {
  if (!lien || renvoiEnCours.value) return;
  renvoiEnCours.value = true;
  const resultat = await comptesService.renvoyerConfirmation(lien.email);
  renvoiEnCours.value = false;
  messageRenvoi.value = resultat.ok ? 'Si un compte attend une confirmation à cette adresse, un nouveau lien vient de partir.' : resultat.message;
}
</script>
