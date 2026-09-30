<template>
  <PanneauBas v-model:open="ouvert" :titre="titre">
    <form class="flex flex-col gap-[18px]" @submit.prevent="valider">
      <div class="grid gap-2" :class="mode === 'modifier' ? 'grid-cols-2' : 'grid-cols-1'">
        <label v-if="mode !== 'terminer'" class="flex flex-col gap-1 text-legende text-texte-2">
          {{ mode === 'commencer' ? 'Depuis le' : 'Début' }}
          <input v-model="debut" type="date" required :max="aujourdhui"
                 class="min-h-11 rounded-controle border-[1.5px] border-contour bg-champ px-3 text-corps text-texte">
        </label>
        <label v-if="mode !== 'commencer'" class="flex flex-col gap-1 text-legende text-texte-2">
          <span>{{ mode === 'terminer' ? 'Dernier jour' : 'Fin' }}
            <span v-if="mode === 'modifier'" class="text-texte-3">(vide = en cours)</span></span>
          <input v-model="fin" type="date" :required="mode === 'terminer'" :min="debut" :max="aujourdhui"
                 class="min-h-11 rounded-controle border-[1.5px] border-contour bg-champ px-3 text-corps text-texte">
        </label>
      </div>

      <p v-if="erreur" role="alert" class="m-0 text-sm text-danger">{{ erreur }}</p>

      <button type="submit" :disabled="envoi || !debut || (mode === 'terminer' && !fin)"
              class="min-h-[52px] rounded-controle bg-button text-base font-semibold text-texte disabled:opacity-50">
        {{ envoi ? 'Enregistrement…' : 'Enregistrer' }}
      </button>

      <button v-if="mode === 'modifier'" type="button"
              class="min-h-11 rounded-controle text-sm font-medium text-danger"
              :class="{ 'border-[1.5px] border-danger': confirmerSuppression }"
              @click="supprimer">
        {{ confirmerSuppression ? 'Confirmer la suppression' : 'Supprimer cet épisode' }}
      </button>
    </form>
  </PanneauBas>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { format } from 'date-fns';
import PanneauBas from '@/shared/components/PanneauBas.vue';
import type { EpisodeAcne, EpisodeAcneSaisie } from '../types/acne';

export type ModeEpisode = 'commencer' | 'terminer' | 'modifier';

/** Écritures fournies par la page (le composant n'appelle jamais l'API). */
export interface ActionsEpisodeAcne {
  commencer: (debut: string) => Promise<void>;
  terminer: (id: number, fin: string) => Promise<void>;
  modifier: (id: number, saisie: EpisodeAcneSaisie) => Promise<void>;
  supprimer: (id: number) => Promise<void>;
}

const ouvert = defineModel<boolean>('open', { required: true });
const props = defineProps<{ mode: ModeEpisode; episode: EpisodeAcne | null; actions: ActionsEpisodeAcne }>();

const aujourdhui = format(new Date(), 'yyyy-MM-dd');
const debut = ref('');
const fin = ref('');
const envoi = ref(false);
const erreur = ref<string | null>(null);
const confirmerSuppression = ref(false);

const titre = computed(() => ({
  commencer: 'L\'acné revient',
  terminer: 'Ça s\'est calmé',
  modifier: 'Modifier l\'épisode',
}[props.mode]));

watch(ouvert, (estOuvert) => {
  if (!estOuvert) return;
  erreur.value = null;
  confirmerSuppression.value = false;
  debut.value = props.mode === 'commencer' ? aujourdhui : props.episode?.debut ?? aujourdhui;
  fin.value = props.mode === 'terminer' ? aujourdhui : props.episode?.fin ?? '';
}, { immediate: true });

/** Le serveur explique un refus (chevauchement, épisode déjà en cours) : le message est affiché tel quel. */
function messageDe(e: unknown, secours: string): string {
  const message = (e as { response?: { data?: { message?: unknown } } })?.response?.data?.message;
  return typeof message === 'string' && message ? message : secours;
}

async function executer(action: () => Promise<void>) {
  envoi.value = true;
  erreur.value = null;
  try {
    await action();
    ouvert.value = false;
  } catch (e) {
    erreur.value = messageDe(e, 'L\'épisode n\'a pas pu être enregistré. Réessaie dans un instant.');
  } finally {
    envoi.value = false;
  }
}

async function valider() {
  if (envoi.value) return;
  if (props.mode === 'commencer') await executer(() => props.actions.commencer(debut.value));
  else if (props.mode === 'terminer') await executer(() => props.actions.terminer(props.episode!.id, fin.value));
  else await executer(() => props.actions.modifier(props.episode!.id, { debut: debut.value, fin: fin.value || null }));
}

async function supprimer() {
  if (!confirmerSuppression.value) {
    confirmerSuppression.value = true;
    return;
  }
  await executer(() => props.actions.supprimer(props.episode!.id));
}
</script>
