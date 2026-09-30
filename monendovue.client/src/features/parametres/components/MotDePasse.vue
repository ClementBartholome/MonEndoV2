<template>
  <form class="flex flex-col gap-4 pb-2" @submit.prevent="changer">
    <label v-for="champ in CHAMPS" :key="champ.cle" class="flex flex-col gap-1 text-left text-sm font-medium text-texte">
      {{ champ.libelle }}
      <input v-model="saisie[champ.cle]" type="password" :autocomplete="champ.autocomplete" required
             class="min-h-11 rounded-controle border-[1.5px] border-contour bg-champ px-3 text-corps font-normal text-texte">
    </label>
    <p v-if="erreur" role="alert" class="m-0 text-left text-sm text-danger">{{ erreur }}</p>
    <button type="submit" :disabled="enCours" class="min-h-12 rounded-controle bg-button px-4 font-semibold text-texte disabled:opacity-60">
      {{ enCours ? 'Changement…' : 'Changer le mot de passe' }}
    </button>
  </form>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue';
import authService from '@/features/auth/services/authService';
import { useToast } from '@/shared/components/ui/toast';

const emit = defineEmits<{ change: [] }>();
const { toast } = useToast();

const CHAMPS = [
  { cle: 'actuel', libelle: 'Mot de passe actuel', autocomplete: 'current-password' },
  { cle: 'nouveau', libelle: 'Nouveau mot de passe', autocomplete: 'new-password' },
  { cle: 'confirmation', libelle: 'Confirmer le nouveau mot de passe', autocomplete: 'new-password' },
] as const;

const saisie = reactive({ actuel: '', nouveau: '', confirmation: '' });
const erreur = ref<string | null>(null);
const enCours = ref(false);

async function changer() {
  if (saisie.nouveau !== saisie.confirmation) {
    erreur.value = 'Les mots de passe ne correspondent pas.';
    return;
  }
  enCours.value = true;
  erreur.value = null;
  try {
    await authService.changePassword(saisie.actuel, saisie.nouveau);
    toast({ title: 'Mot de passe changé', variant: 'custom' });
    emit('change');
  } catch (e) {
    erreur.value = (e as Error).message || 'Le mot de passe n\'a pas pu être changé.';
  } finally {
    enCours.value = false;
  }
}
</script>
