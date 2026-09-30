<template>
  <div>
    <BackButton class="absolute top-6 left-6 m-0 !w-fit"/>
    <form
        class="flex flex-col rounded-card items-center justify-center gap-4 w-full max-w-md mx-auto mt-20 mb-20 bg-[var(--background-clearer)] p-14"
        @submit="onSubmitRegister">
      <FormField v-slot="{ componentField }" name="email">
        <img src="@/images/MonEndo_transparent.png" alt="Logo MonEndo" class="max-w-44 max-h-44">
        <FormItem class="w-full">
          <FormLabel>Email</FormLabel>
          <FormControl>
            <Input type="text" placeholder="mail@gmail.com" v-bind="componentField"/>
          </FormControl>
          <FormMessage/>
        </FormItem>
      </FormField>
      <FormField v-slot="{ componentField }" name="password">
        <FormItem class="w-full">
          <FormLabel>Mot de passe</FormLabel>
          <FormControl>
            <Input type="password" placeholder="********" v-bind="componentField"/>
          </FormControl>
          <FormMessage/>
        </FormItem>
      </FormField>
      <CaseConsentement v-model="consentement"/>
      <Button type="submit" :disabled="!consentement">
        Créer un compte
      </Button>
      <p class="mt-4">
        Déjà inscrit ? <router-link to="/login" class="!text-highlight">Connectez-vous</router-link>
      </p>
      <LiensLegaux/>
    </form>
  </div>
</template>

<script setup lang="ts">
import LiensLegaux from '@/features/legal/components/LiensLegaux.vue';
import { onMounted, ref } from 'vue';
import CaseConsentement from '@/features/legal/components/CaseConsentement.vue';
import {Button} from '@/shared/components/ui/button'
import {
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/shared/components/ui/form'
import {Input} from '@/shared/components/ui/input'
import {useToast} from '@/shared/components/ui/toast'
import {useAuthStore} from '@/features/auth/store/auth';
import router from "@/router";
import BackButton from "@/shared/components/BackButton.vue";

const auth = useAuthStore();
const {toast} = useToast();
/** Case non cochée par défaut : l'accord doit être un geste explicite. */
const consentement = ref(false);

onMounted(() => {
  if (auth.user) {
    router.push('/');
  }
});

const onSubmitRegister = async (event: any) => {
  event.preventDefault();
  const form = event.target;
  const email = form.email.value;
  const password = form.password.value;

  let user;
  try {
    user = await auth.register(email, password, consentement.value);
    toast({
      title: 'Inscription réussie',
      description: `Bienvenue ${user.email}`,
      variant: 'custom',
    });
  } catch (error: any) {
    console.error(error);
    toast({
      title: 'Inscription échouée',
      description: `${error.message}`,
      variant: 'custom',
    });
  }
  if (user) {
    router.push('/');
  } 
}
</script>