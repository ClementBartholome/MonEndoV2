<template>
  <main class="mx-auto flex min-h-dvh w-full max-w-md flex-col gap-6 px-6 pb-8 pt-6">
    <router-link to="/bienvenue" aria-label="Retour" class="-ml-3 flex h-11 w-11 items-center justify-center text-texte no-underline">
      <i class="material-symbols-outlined" aria-hidden="true">arrow_back</i>
    </router-link>

    <header class="flex flex-col gap-1.5 text-left">
      <h1 class="m-0 text-titre-page font-semibold leading-tight tracking-normal text-texte">Crée ton compte</h1>
      <p class="m-0 text-corps leading-normal text-texte-2">Une adresse e-mail et un mot de passe : c'est tout.</p>
    </header>

    <EngagementsDonnees/>

    <form class="flex flex-col gap-4" novalidate @submit.prevent="soumettre">
      <ChampTexte v-model="email" label="Adresse e-mail" name="email" type="email" inputmode="email" autocomplete="email" :erreur="erreurEmail">
        <template #erreur>
          {{ erreurEmail }}
          <router-link v-if="emailDejaPris" to="/login" class="font-semibold !text-lien">Se connecter</router-link>
        </template>
      </ChampTexte>

      <ChampTexte v-model="motDePasse" label="Mot de passe" name="password" type="password" autocomplete="new-password" :erreur="erreurMotDePasse">
        <CriteresMotDePasse :criteres="criteres"/>
      </ChampTexte>

      <div class="flex flex-col gap-2">
        <CaseConsentement v-model="consentement" :erreur="erreurConsentement"/>
      </div>

      <p v-if="erreurGenerale" role="alert" class="m-0 flex items-start gap-3 rounded-carte bg-surface p-3.5 text-left text-sm leading-normal text-texte shadow-elevation">
        <i class="material-symbols-outlined rounded-controle bg-teinte-douleur-fond p-1.5 text-teinte-douleur" aria-hidden="true">cloud_off</i>
        <span>{{ erreurGenerale }}</span>
      </p>

      <button type="submit" :disabled="envoiEnCours" :aria-busy="envoiEnCours"
              class="inline-flex min-h-[54px] items-center justify-center gap-2 rounded-[16px] bg-button text-base font-semibold text-texte disabled:opacity-70">
        <i v-if="envoiEnCours" class="material-symbols-outlined animate-spin" aria-hidden="true">progress_activity</i>
        {{ envoiEnCours ? 'Création du compte…' : 'Créer mon compte' }}
      </button>
    </form>

    <p class="m-0 text-center text-sm text-texte-2">
      <router-link to="/login" class="inline-flex min-h-11 items-center font-medium !text-lien">J'ai déjà un compte</router-link>
    </p>
    <LiensLegaux/>
  </main>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { useRouter } from 'vue-router';
import LiensLegaux from '@/features/legal/components/LiensLegaux.vue';
import CaseConsentement from '@/features/legal/components/CaseConsentement.vue';
import { useAuthStore } from '@/features/auth/store/auth';
import ChampTexte from '../components/ChampTexte.vue';
import CriteresMotDePasse from '../components/CriteresMotDePasse.vue';
import EngagementsDonnees from '../components/EngagementsDonnees.vue';
import { useInscription } from '../composables/useInscription';

const router = useRouter();
const auth = useAuthStore();
const { email, motDePasse, consentement, criteres, envoiEnCours, emailDejaPris, erreurEmail, erreurMotDePasse, erreurConsentement, erreurGenerale, creer } = useInscription();

onMounted(() => {
  if (auth.user) router.push('/');
});

/** Compte créé : la page de rappel propose le rappel du soir, puis ramène à l'accueil (elle passe seule si l'appareil ne peut pas en recevoir). */
async function soumettre() {
  if (await creer()) router.push('/bienvenue/rappel');
}
</script>
