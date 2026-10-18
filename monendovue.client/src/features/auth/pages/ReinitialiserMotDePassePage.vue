<template>
  <PageCompte :titre="change ? 'Mot de passe changé' : 'Nouveau mot de passe'" :icone="change ? 'lock_reset' : undefined">
    <template v-if="!lien">
      <MessageCompte ton="erreur">Ce lien est incomplet. Ouvre-le directement depuis l'e-mail reçu, ou demande un nouveau lien.</MessageCompte>
      <router-link to="/mot-de-passe-oublie" class="inline-flex min-h-11 items-center justify-center font-medium !text-lien">Demander un nouveau lien</router-link>
    </template>

    <template v-else-if="change">
      <p class="m-0 text-left text-corps leading-normal text-texte">
        Ton nouveau mot de passe est enregistré. Par sécurité, toutes tes sessions ouvertes ont été fermées.
      </p>
      <router-link :to="{ name: 'login', state: { email: lien.email } }"
                   class="inline-flex min-h-[54px] items-center justify-center rounded-[16px] bg-button text-base font-semibold !text-texte no-underline">
        Me connecter
      </router-link>
    </template>

    <form v-else class="flex flex-col gap-4" novalidate @submit.prevent="enregistrer">
      <p class="m-0 text-left text-sm text-texte-2">Pour <strong class="break-all">{{ lien.email }}</strong></p>
      <ChampTexte v-model="motDePasse" label="Nouveau mot de passe" name="password" type="password" autocomplete="new-password" :erreur="erreurMotDePasse">
        <CriteresMotDePasse :criteres="criteres"/>
      </ChampTexte>
      <MessageCompte v-if="erreurLien" ton="erreur">
        {{ erreurLien }} <router-link to="/mot-de-passe-oublie" class="font-semibold !text-lien">Demander un nouveau lien</router-link>
      </MessageCompte>
      <button type="submit" :disabled="envoiEnCours"
              class="inline-flex min-h-[54px] items-center justify-center rounded-[16px] bg-button text-base font-semibold text-texte disabled:opacity-70">
        {{ envoiEnCours ? 'Enregistrement…' : 'Enregistrer le mot de passe' }}
      </button>
    </form>
  </PageCompte>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import ChampTexte from '../components/ChampTexte.vue';
import CriteresMotDePasse from '../components/CriteresMotDePasse.vue';
import MessageCompte from '../components/MessageCompte.vue';
import PageCompte from '../components/PageCompte.vue';
import { comptesService } from '../services/comptesService';
import { lireLienDuFragment } from '../utils/lienEmail';
import { CRITERES_MOT_DE_PASSE, motDePasseValide } from '../utils/motDePasse';

/** Lien de réinitialisation reçu par e-mail : nouveau mot de passe avec les mêmes règles qu'à l'inscription. */
const lien = lireLienDuFragment();
const motDePasse = ref('');
const tentative = ref(false);
const envoiEnCours = ref(false);
const change = ref(false);
const erreurServeur = ref<string | null>(null);
const erreurLien = ref<string | null>(null);

const criteres = computed(() => CRITERES_MOT_DE_PASSE.map((c) => ({ cle: c.cle, libelle: c.libelle, respecte: c.respecte(motDePasse.value) })));
const erreurMotDePasse = computed(() => {
  if (erreurServeur.value) return erreurServeur.value;
  return tentative.value && !motDePasseValide(motDePasse.value) ? 'Ton mot de passe ne respecte pas encore toutes les règles.' : null;
});

async function enregistrer() {
  if (!lien || envoiEnCours.value) return;
  tentative.value = true;
  erreurServeur.value = null;
  erreurLien.value = null;
  if (!motDePasseValide(motDePasse.value)) return;

  envoiEnCours.value = true;
  const resultat = await comptesService.reinitialiserMotDePasse(lien.email, lien.jeton, motDePasse.value);
  envoiEnCours.value = false;
  if (resultat.ok) {
    change.value = true;
  } else if (resultat.erreurs.some((e) => /mot de passe/i.test(e))) {
    erreurServeur.value = resultat.erreurs.find((e) => /mot de passe/i.test(e)) ?? resultat.message;
  } else {
    erreurLien.value = resultat.message;
  }
}
</script>
