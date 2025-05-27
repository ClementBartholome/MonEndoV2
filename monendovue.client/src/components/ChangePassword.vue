<template>

  <details class="summary-after">
    <summary class="flex gap-2">
      <span class="material-symbols-outlined">
      lock
      </span>
      <h4>Modifier le mot de passe</h4>
    </summary>

    <form @submit.prevent="handleChangePassword">
      <FormField v-slot="{ componentField }" name="currentPassword">
        <FormItem class="w-full">
          <FormLabel>Mot de passe actuel</FormLabel>
          <FormControl>
            <Input type="password" v-model="currentPassword" v-bind="componentField"/>
          </FormControl>
          <FormMessage/>
        </FormItem>
      </FormField>
      <FormField v-slot="{ componentField }" name="newPassword">
        <FormItem class="w-full">
          <FormLabel>Nouveau mot de passe</FormLabel>
          <FormControl>
            <Input type="password" v-model="newPassword" v-bind="componentField"/>
          </FormControl>
          <FormMessage/>
        </FormItem>
      </FormField>
      <FormField v-slot="{ componentField }" name="confirmPassword">
        <FormItem class="w-full">
          <FormLabel>Confirmer le nouveau mot de passe</FormLabel>
          <FormControl>
            <Input type="password" v-model="confirmPassword" v-bind="componentField"/>
          </FormControl>
          <FormMessage/>
        </FormItem>
      </FormField>
      <Button type="submit" variant="custom" class="mt-4">
        Changer le mot de passe
      </Button>
    </form>
  </details>
</template>

<script setup lang="ts">
import {ref} from 'vue';
import {Button} from '@/components/ui/button';
import {
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import {Input} from '@/components/ui/input';
import {useToast} from "@/components/ui/toast";
import authService from "@/services/authService";

const currentPassword = ref('');
const newPassword = ref('');
const confirmPassword = ref('');
const {toast} = useToast();


const handleChangePassword = async () => {
  if (newPassword.value !== confirmPassword.value) {
    toast({
      title: 'Erreur',
      description: 'Les mots de passe ne correspondent pas',
      variant: 'custom',
    });
    return;
  }

  try {
    await authService.changePassword(currentPassword.value, newPassword.value);
    toast({
      title: 'Succès',
      description: 'Mot de passe changé avec succès',
      variant: 'custom',
    });
    currentPassword.value = '';
    newPassword.value = '';
    confirmPassword.value = '';
  } catch (error: any) {
    console.error('Erreur lors du changement de mot de passe:', error);
    toast({
      title: 'Erreur',
      description: `${error.message}`,
      variant: 'custom',
    });
  }
};
</script>

<style scoped>
summary {
  list-style: none;
  display: inline-flex;
  align-items: center;
}

summary::after {
  content: '';
  width: 16px;
  height: 9px;
  background: url('https://uploads.sitepoint.com/wp-content/uploads/2023/10/1697699669arrow.svg') no-repeat;
  background-size: cover;
  margin-left: .75em;
  transition: 0.2s;
  transform: rotate(270deg);
}

details[open] > summary::after {
  transform: rotate(360deg);
}

summary::-webkit-details-marker {
  display: none;
}

summary {
  border-radius: 5px;
}

details[open] summary {
  border-radius: 5px 5px 0 0;
}

details {
  border-radius: 5px;
}

</style>