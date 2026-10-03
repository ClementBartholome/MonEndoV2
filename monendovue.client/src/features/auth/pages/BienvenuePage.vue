<template>
  <main class="mx-auto flex min-h-dvh w-full max-w-md flex-col px-6 pb-6 pt-7">
    <img src="@/images/MonEndo_transparent.png" alt="MonEndo" class="h-14 w-auto self-start">

    <header class="mt-7 flex flex-col gap-3.5 text-left">
      <h1 class="m-0 text-[2.125rem] font-semibold leading-[1.15] tracking-tight text-texte">Ta douleur, noir sur blanc.</h1>
      <p class="m-0 text-base leading-relaxed text-texte-2">
        Note ton quotidien, repère ce qui revient, et arrive à tes rendez-vous avec un récit clair.
      </p>
    </header>

    <div aria-hidden="true" class="mt-8 flex flex-col gap-3 rounded-[24px] bg-surface p-4 shadow-[0_8px_24px_rgba(51,39,42,0.12)]">
      <div class="flex items-center justify-between">
        <span class="text-legende font-semibold uppercase tracking-wider text-texte-3">Bilan du jour</span>
        <span class="rounded-full bg-teinte-traitement-fond px-2.5 py-0.5 text-legende font-medium text-teinte-traitement">Fait</span>
      </div>
      <div class="grid grid-cols-3 gap-3 text-left">
        <div class="flex flex-col rounded-2xl bg-teinte-douleur-fond px-3 py-2.5 text-teinte-douleur">
          <span class="text-legende">Douleur</span>
          <span class="text-titre-2 font-semibold leading-tight">4<span class="text-sm font-medium">/10</span></span>
        </div>
        <div class="flex flex-col rounded-2xl bg-teinte-symptome-fond px-3 py-2.5 text-teinte-symptome">
          <span class="text-legende">Émotion</span>
          <span class="text-corps font-semibold leading-snug">Calme</span>
        </div>
        <div class="flex flex-col rounded-2xl bg-teinte-regles-fond px-3 py-2.5 text-teinte-regles">
          <span class="text-legende">Règles</span>
          <span class="text-corps font-semibold leading-snug">Jour 2</span>
        </div>
      </div>
    </div>

    <div class="mt-auto flex flex-col gap-2.5 pt-8">
      <p class="m-0 mb-1.5 text-center text-sm text-texte-2">Gratuit · Sans publicité · Hébergé en Europe</p>
      <router-link to="/register"
                   class="inline-flex min-h-[54px] items-center justify-center rounded-[16px] bg-button text-base font-semibold text-texte no-underline">Créer mon compte</router-link>
      <router-link to="/login"
                   class="inline-flex min-h-[54px] items-center justify-center rounded-[16px] border-[1.5px] border-contour text-corps font-medium text-texte no-underline">J'ai déjà un compte</router-link>
      <LiensLegaux/>
    </div>
  </main>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { useRouter } from 'vue-router';
import LiensLegaux from '@/features/legal/components/LiensLegaux.vue';
import { useAuthStore } from '@/features/auth/store/auth';

/** Première page d'une visiteuse qui n'est jamais venue : ce que fait MonEndo, avant de lui demander quoi que ce soit. */
const router = useRouter();
const auth = useAuthStore();

onMounted(() => {
  if (auth.user) router.replace('/');
});
</script>
