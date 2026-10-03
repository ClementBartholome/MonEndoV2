<template>
  <main class="mx-auto flex min-h-dvh w-full max-w-md flex-col gap-6 px-6 pb-8 pt-6">
    <router-link to="/bienvenue" aria-label="Retour" class="-ml-3 flex h-11 w-11 items-center justify-center text-texte no-underline">
      <i class="material-symbols-outlined" aria-hidden="true">arrow_back</i>
    </router-link>

    <header class="flex flex-col gap-1.5 text-left">
      <h1 class="m-0 text-titre-page font-semibold leading-tight tracking-normal text-texte">Content de te revoir</h1>
      <p class="m-0 text-corps leading-normal text-texte-2">Connecte-toi pour retrouver ton suivi.</p>
    </header>

    <form class="flex flex-col gap-4" novalidate @submit.prevent="onSubmit">
      <ChampTexte v-model="email" label="Adresse e-mail" name="email" type="email" inputmode="email" autocomplete="username"/>
      <ChampTexte v-model="password" label="Mot de passe" name="password" type="password" autocomplete="current-password"/>
      <button type="submit" :disabled="envoiEnCours"
              class="inline-flex min-h-[54px] items-center justify-center rounded-[16px] bg-button text-base font-semibold text-texte disabled:opacity-70">
        Connexion
      </button>
    </form>

    <p class="m-0 text-center text-sm text-texte-2">
      Pas encore de compte ? <router-link to="/register" class="inline-flex min-h-11 items-center font-medium !text-lien">Créer mon compte</router-link>
    </p>
    <LiensLegaux/>
  </main>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import LiensLegaux from '@/features/legal/components/LiensLegaux.vue';
import { useToast } from '@/shared/components/ui/toast';
import { useAuthStore } from '@/features/auth/store/auth';
import router from '@/router';
import ChampTexte from '../components/ChampTexte.vue';

const auth = useAuthStore();
const { toast } = useToast();

const email = ref('');
const password = ref('');
const envoiEnCours = ref(false);

onMounted(() => {
  if (auth.user) {
    router.push('/');
  }
});

async function onSubmit() {
  if (envoiEnCours.value) return;
  envoiEnCours.value = true;
  let user;
  try {
    user = await auth.login(email.value.trim(), password.value);
  } catch (error: any) {
    toast({
      title: 'Connexion impossible pour le moment',
      description: error.response?.status === 429
        ? 'Trop de tentatives. Veuillez patienter une minute avant de réessayer.'
        : 'Le service est momentanément indisponible. Veuillez réessayer dans un instant.',
      variant: 'custom'
    });
    return;
  } finally {
    envoiEnCours.value = false;
  }
  if (user) {
    router.push('/');
  } else {
    toast({
      title: 'Connexion échouée',
      description: 'Veuillez vérifier votre email et votre mot de passe.',
      variant: 'custom'
    });
  }
}
</script>
