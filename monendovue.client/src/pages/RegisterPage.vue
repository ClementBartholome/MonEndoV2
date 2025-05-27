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
      <Button type="submit">
        Créer un compte
      </Button>
      <p class="mt-4">
        Déjà inscrit ? <router-link to="/login" class="!text-highlight">Connectez-vous</router-link>
      </p>
    </form>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import {Button} from '@/components/ui/button'
import {
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import {Input} from '@/components/ui/input'
import {useToast} from '@/components/ui/toast'
import {useAuthStore} from '@/store/auth';
import router from "@/router";
import BackButton from "@/components/BackButton.vue";

const auth = useAuthStore();
const {toast} = useToast();

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
    user = await auth.register(email, password);
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