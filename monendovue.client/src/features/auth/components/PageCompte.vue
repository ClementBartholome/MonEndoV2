<template>
  <main class="mx-auto flex min-h-dvh w-full max-w-md flex-col gap-6 px-6 pb-8 pt-6">
    <router-link :to="retour" aria-label="Retour" class="-ml-3 flex h-11 w-11 items-center justify-center text-texte no-underline">
      <i class="material-symbols-outlined" aria-hidden="true">arrow_back</i>
    </router-link>

    <header class="flex flex-col gap-1.5 text-left">
      <i v-if="icone" class="material-symbols-outlined mb-1 self-start rounded-controle bg-teinte-bilan-fond p-2 text-titre-page text-teinte-bilan" aria-hidden="true">{{ icone }}</i>
      <h1 class="m-0 text-titre-page font-semibold leading-tight tracking-normal text-texte">{{ titre }}</h1>
      <p v-if="sousTitre" class="m-0 text-corps leading-normal text-texte-2">{{ sousTitre }}</p>
    </header>

    <slot/>

    <LiensLegaux/>
  </main>
</template>

<script setup lang="ts">
import LiensLegaux from '@/features/legal/components/LiensLegaux.vue';

/** Mise en page des écrans de compte sans session (lien envoyé, confirmation, mot de passe oublié), comme la connexion. */
withDefaults(defineProps<{ titre: string; sousTitre?: string; icone?: string; retour?: string }>(), { retour: '/login' });
</script>
