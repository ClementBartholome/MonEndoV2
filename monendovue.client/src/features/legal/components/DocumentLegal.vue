<template>
  <article class="document-legal mx-auto w-full max-w-3xl px-5 pb-40 pt-24 lg:pb-12 lg:pt-10">
    <router-link :to="retour.lien" class="inline-flex min-h-11 items-center gap-1 text-sm font-medium !text-lien no-underline">
      <i class="material-symbols-outlined text-base" aria-hidden="true">arrow_back</i>
      <span class="underline">{{ retour.libelle }}</span>
    </router-link>

    <header class="mt-6 mb-8">
      <h1 class="m-0 text-[26px] font-semibold tracking-normal text-texte">{{ titre }}</h1>
      <p class="m-0 mt-2 text-sm text-texte-3">Dernière mise à jour : {{ MISE_A_JOUR_DOCUMENTS_LEGAUX }}</p>
    </header>

    <div class="contenu flex flex-col gap-8 rounded-carte bg-surface p-5 leading-relaxed text-texte-2 shadow-elevation md:p-8">
      <slot/>
    </div>
  </article>
</template>

<script setup lang="ts">
import {computed} from 'vue';
import {useAuthStore} from '@/features/auth/store/auth';
import {MISE_A_JOUR_DOCUMENTS_LEGAUX} from '../config/editeur';

defineProps<{ titre: string }>();

const auth = useAuthStore();

/** Page publique : une personne non connectée revient à la connexion, une utilisatrice connectée à l'accueil. */
const retour = computed(() => auth.user
    ? {lien: '/', libelle: 'Retour à l\'accueil'}
    : {lien: '/login', libelle: 'Retour à la connexion'});
</script>

<style scoped>
.document-legal :deep(h2) {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--headline);
  margin-bottom: 0.5rem;
}

.document-legal :deep(p + p),
.document-legal :deep(p + ul),
.document-legal :deep(ul + p) {
  margin-top: 0.5rem;
}

.document-legal :deep(ul) {
  list-style: disc;
  padding-left: 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

/* Liens du texte seulement (le lien de retour a son propre style). */
.contenu :deep(a) {
  text-decoration: underline;
  overflow-wrap: anywhere;
}
</style>
