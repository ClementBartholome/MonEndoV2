<template>
  <Dialog v-model:open="ouvert">
    <DialogTrigger as-child>
      <Button type="button" variant="outline" class="min-h-11 w-full md:w-fit text-destructive border-destructive/40">
        <i class="material-symbols-outlined mr-2" aria-hidden="true">delete_forever</i>
        Supprimer mon compte
      </Button>
    </DialogTrigger>
    <DialogContent class="max-w-md">
      <DialogHeader>
        <DialogTitle>Supprimer ton compte ?</DialogTitle>
        <DialogDescription>
          Ton compte et tout ce que tu as noté (suivis, bilans, traitements, photos, rappels) seront supprimés
          définitivement. Cette action est irréversible : pense à télécharger tes données avant si tu veux les garder.
        </DialogDescription>
      </DialogHeader>
      <form class="flex flex-col gap-4" @submit.prevent="supprimerMonCompte">
        <div class="flex flex-col gap-1">
          <label for="suppression-mot-de-passe" class="text-sm font-medium text-headline">Ton mot de passe, pour confirmer</label>
          <Input id="suppression-mot-de-passe" v-model="motDePasse" type="password" autocomplete="current-password"/>
        </div>
        <p v-if="erreurSuppression" role="alert" class="text-sm text-destructive">{{ erreurSuppression }}</p>
        <DialogFooter class="gap-2">
          <DialogClose as-child>
            <Button type="button" variant="outline" class="min-h-11">Annuler</Button>
          </DialogClose>
          <Button type="submit" variant="destructive" class="min-h-11" :disabled="!motDePasse || suppressionEnCours">
            {{ suppressionEnCours ? 'Suppression…' : 'Supprimer définitivement' }}
          </Button>
        </DialogFooter>
      </form>
    </DialogContent>
  </Dialog>
</template>

<script setup lang="ts">
import {ref, watch} from 'vue';
import {useRouter} from 'vue-router';
import {Button} from '@/shared/components/ui/button';
import {Input} from '@/shared/components/ui/input';
import {Dialog, DialogClose, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger} from '@/shared/components/ui/dialog';
import {useToast} from '@/shared/components/ui/toast';
import {useSuppressionCompte} from '../composables/useSuppressionCompte';

const router = useRouter();
const {toast} = useToast();
const ouvert = ref(false);

const {motDePasse, suppressionEnCours, erreurSuppression, reinitialiser, supprimerMonCompte} = useSuppressionCompte({
  surSuppression: () => {
    ouvert.value = false;
    toast({title: 'Compte supprimé', description: 'Ton compte et toutes tes données ont été supprimés.', variant: 'custom'});
    router.push('/login');
  },
});

watch(ouvert, (estOuvert) => {
  if (!estOuvert) reinitialiser();
});
</script>
