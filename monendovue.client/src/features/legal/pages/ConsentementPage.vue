<template>
  <section class="mx-auto w-full max-w-xl px-4 py-10 md:py-16">
    <div class="bg-clearer rounded-3xl p-6 md:p-8 flex flex-col gap-5">
      <div class="flex items-center gap-3">
        <i class="material-symbols-outlined text-headline" aria-hidden="true">verified_user</i>
        <h1 class="text-2xl text-headline">Ton accord pour tes données de santé</h1>
      </div>

      <p class="text-paragraph leading-relaxed">
        Les douleurs, cycles, traitements et bilans que tu notes dans MonEndo sont des données de santé. La loi demande ton
        accord explicite pour les enregistrer. Elles servent uniquement à ton suivi : ni publicité, ni revente, ni partage.
      </p>

      <form class="flex flex-col gap-5" @submit.prevent="donnerAccord">
        <CaseConsentement v-model="accepte"/>
        <p v-if="erreur" role="alert" class="text-sm text-destructive">{{ erreur }}</p>
        <Button type="submit" variant="custom" class="min-h-11" :disabled="!accepte || envoiEnCours">
          J'accepte et je continue
        </Button>
      </form>

      <div class="flex flex-col gap-3 border-t border-gray-300 pt-5">
        <p class="text-sm text-muted-foreground">
          Tu ne souhaites pas donner ton accord ? Pour supprimer ton compte et toutes tes données, écris à
          <a :href="`mailto:${EDITEUR.email}`" class="underline">{{ EDITEUR.email }}</a>.
        </p>
        <Button type="button" variant="outline" class="min-h-11" @click="seDeconnecter">Me déconnecter</Button>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import {useRouter} from 'vue-router';
import {Button} from '@/shared/components/ui/button';
import {useAuthStore} from '@/features/auth/store/auth';
import CaseConsentement from '../components/CaseConsentement.vue';
import {useConsentement} from '../composables/useConsentement';
import {EDITEUR} from '../config/editeur';

const router = useRouter();
const auth = useAuthStore();

const {accepte, envoiEnCours, erreur, donnerAccord} = useConsentement({surAccord: () => router.push('/')});

async function seDeconnecter() {
  await auth.logout();
  await router.push('/login');
}
</script>
