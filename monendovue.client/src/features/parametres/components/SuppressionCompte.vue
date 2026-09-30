<template>
  <PanneauBas v-model:open="ouvert" titre="Supprimer ton compte ?">
    <p class="m-0 text-left text-sm text-texte-2">
      Ton compte et tout ce que tu as noté (suivis, bilans, traitements, photos, rappels) seront supprimés définitivement.
      Cette action est irréversible : pense à télécharger tes données avant si tu veux les garder. C'est aussi la façon
      de retirer ton accord.
    </p>
    <form class="flex flex-col gap-4 pb-2" @submit.prevent="supprimerMonCompte">
      <label class="flex flex-col gap-1 text-left text-sm font-medium text-texte">
        Ton mot de passe, pour confirmer
        <input v-model="motDePasse" type="password" autocomplete="current-password"
               class="min-h-11 rounded-controle border-[1.5px] border-contour bg-champ px-3 text-corps font-normal text-texte">
      </label>
      <p v-if="erreurSuppression" role="alert" class="m-0 text-left text-sm text-danger">{{ erreurSuppression }}</p>
      <div class="flex gap-2">
        <button type="button" class="min-h-12 grow rounded-controle border-[1.5px] border-contour px-4 font-medium text-texte" @click="ouvert = false">Annuler</button>
        <button type="submit" :disabled="!motDePasse || suppressionEnCours"
                class="min-h-12 grow rounded-controle bg-danger px-4 font-semibold text-sur-fonce disabled:opacity-60">
          {{ suppressionEnCours ? 'Suppression…' : 'Supprimer définitivement' }}
        </button>
      </div>
    </form>
  </PanneauBas>
</template>

<script setup lang="ts">
import {watch} from 'vue';
import {useRouter} from 'vue-router';
import PanneauBas from '@/shared/components/PanneauBas.vue';
import {useToast} from '@/shared/components/ui/toast';
import {useSuppressionCompte} from '../composables/useSuppressionCompte';

/** Suppression définitive du compte, dans un panneau ouvert depuis la ligne « Supprimer mon compte ». */
const ouvert = defineModel<boolean>('open', {required: true});
const router = useRouter();
const {toast} = useToast();

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
