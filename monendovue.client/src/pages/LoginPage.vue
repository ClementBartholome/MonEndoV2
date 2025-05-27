
<template>
  <form
      class="flex flex-col rounded-card items-center justify-center gap-4 w-full max-w-md mx-auto mt-20 mb-20 bg-[var(--background-clearer)] p-14"
      @submit="onSubmit">
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
      Connexion
    </Button>
    <p class="mt-4">
      Pas de compte ? <router-link to="/register" class="!text-highlight">Créez-en un</router-link>
    </p>
  </form>
</template>

<script setup lang="ts">
import {Button} from '@/components/ui/button'
import {
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import {Input} from '@/components/ui/input'
import {useToast} from '@/components/ui/toast'
import {useAuthStore} from '@/store/auth';
import router from "@/router";
import { onMounted } from 'vue';

const auth = useAuthStore();
const {toast} = useToast();

onMounted(() => {
  if (auth.user) {
    router.push('/');
  }
});
const onSubmit = async (event: any) => {
  event.preventDefault();
  const form = event.target;
  const email = form.email.value;
  const password = form.password.value;

  const user = await auth.login(email, password);
  if (user) {
    router.push('/');
  } else {
    toast({
      title: 'Connexion échouée',
      description: 'Veuillez vérifier votre email et votre mot de passe.',
    });
  }
}
</script>