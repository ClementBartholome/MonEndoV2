<template>
  <PanneauBas v-model:open="ouvert" :titre="form.enModification.value ? 'Modifier la douleur' : 'Noter une douleur'">
    <form class="flex flex-col gap-[18px]" @submit.prevent="valider">
      <fieldset class="m-0 border-0 p-0">
        <legend class="mb-2.5 p-0 text-sm font-semibold text-texte">Où as-tu mal ?</legend>
        <div class="grid grid-cols-2 gap-2">
          <button v-for="t in TYPES_DOULEUR" :key="t.valeur" type="button"
                  class="min-h-12 rounded-controle px-3 text-sm"
                  :class="form.type.value === t.valeur ? 'border-[1.5px] border-texte bg-texte font-medium text-fond' : 'border-[1.5px] border-trait bg-surface text-texte'"
                  :aria-pressed="form.type.value === t.valeur"
                  @click="form.type.value = t.valeur">{{ t.libelle }}</button>
        </div>
      </fieldset>

      <fieldset class="m-0 border-0 p-0">
        <legend class="mb-2.5 p-0 text-sm font-semibold text-texte">
          Intensité<span v-if="form.intensite.value !== null" class="font-medium text-texte-2"> : {{ form.intensite.value }}/10</span>
        </legend>
        <div class="grid grid-cols-10 gap-[3px]">
          <button v-for="n in 10" :key="n" type="button"
                  class="min-h-12 rounded-controle p-0 text-sm font-semibold"
                  :class="form.intensite.value === n ? 'border-2 border-texte' : 'border-[1.5px] border-trait bg-surface text-texte-2'"
                  :style="form.intensite.value === n ? pastille(n) : undefined"
                  :aria-label="`Intensité ${n} sur 10`" :aria-pressed="form.intensite.value === n"
                  @click="form.intensite.value = n">{{ n }}</button>
        </div>
        <div class="mt-1.5 flex justify-between text-xs text-texte-3"><span>légère</span><span>la pire imaginable</span></div>
      </fieldset>

      <fieldset class="m-0 border-0 p-0">
        <legend class="mb-2.5 p-0 text-sm font-semibold text-texte">Quand ?</legend>
        <div class="grid grid-cols-3 gap-2">
          <button v-for="m in moments" :key="m.valeur" type="button"
                  class="min-h-11 rounded-controle text-sm"
                  :class="form.moment.value === m.valeur ? 'border-[1.5px] border-texte bg-texte font-medium text-fond' : 'border-[1.5px] border-trait bg-surface text-texte'"
                  :aria-pressed="form.moment.value === m.valeur"
                  @click="form.moment.value = m.valeur">{{ m.libelle }}</button>
        </div>
        <div v-if="form.moment.value === 'autre'" class="mt-3 grid grid-cols-2 gap-2">
          <label class="flex flex-col gap-1 text-[13px] text-texte-2">Jour
            <input v-model="form.jour.value" type="date" required :max="aujourdhui"
                   class="min-h-11 rounded-controle border-[1.5px] border-contour bg-white px-3 text-[15px] text-texte">
          </label>
          <label class="flex flex-col gap-1 text-[13px] text-texte-2">Heure
            <input v-model="form.heure.value" type="time" required
                   class="min-h-11 rounded-controle border-[1.5px] border-contour bg-white px-3 text-[15px] text-texte">
          </label>
        </div>
      </fieldset>

      <label class="flex flex-col gap-1.5 text-sm font-semibold text-texte">
        <span>Commentaire <span class="font-normal text-texte-3">(facultatif)</span></span>
        <textarea v-model="form.commentaire.value" rows="2" maxlength="500"
                  placeholder="Ce qui l'a déclenchée, ce qui a soulagé…"
                  class="resize-none rounded-controle border-[1.5px] border-contour bg-white px-3 py-2.5 text-[15px] font-normal text-texte"></textarea>
      </label>

      <p v-if="erreur" role="alert" class="m-0 text-sm text-danger">{{ erreur }}</p>

      <button type="submit" :disabled="!form.complete.value || envoi"
              class="min-h-[52px] rounded-controle bg-button text-base font-semibold text-texte disabled:opacity-50">
        {{ envoi ? 'Enregistrement…' : 'Enregistrer' }}
      </button>

      <button v-if="form.enModification.value" type="button"
              class="min-h-11 rounded-controle text-sm font-medium text-danger"
              :class="{ 'border-[1.5px] border-danger': confirmerSuppression }"
              @click="supprimer">
        {{ confirmerSuppression ? 'Confirmer la suppression' : 'Supprimer cette douleur' }}
      </button>
    </form>
  </PanneauBas>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { format } from 'date-fns';
import PanneauBas from '@/shared/components/PanneauBas.vue';
import type { DonneesDouleur, DonneesDouleurModification } from '../types/donnees-douleur';
import { TYPES_DOULEUR } from '../utils/douleurs';
import { useSaisieDouleur, type Moment } from '../composables/useSaisieDouleur';

const ouvert = defineModel<boolean>('open', { required: true });
/** Enregistrement et suppression, fournis par la page (le composant n'appelle jamais l'API). */
export interface ActionsSaisieDouleur {
  enregistrer: (saisie: DonneesDouleurModification, id: number | null) => Promise<void>;
  supprimer: (id: number) => Promise<void>;
}

const props = defineProps<{ entree: DonneesDouleur | null; actions: ActionsSaisieDouleur }>();

const form = useSaisieDouleur();
const envoi = ref(false);
const erreur = ref<string | null>(null);
const confirmerSuppression = ref(false);
const aujourdhui = format(new Date(), 'yyyy-MM-dd');

const moments: { valeur: Moment; libelle: string }[] = [
  { valeur: 'maintenant', libelle: 'Maintenant' },
  { valeur: 'matin', libelle: 'Ce matin' },
  { valeur: 'autre', libelle: 'Autre' },
];

watch(ouvert, (estOuvert) => {
  if (!estOuvert) return;
  erreur.value = null;
  confirmerSuppression.value = false;
  if (props.entree) form.preparerModification(props.entree);
  else form.preparerAjout();
}, { immediate: true });

function pastille(intensite: number) {
  return { background: `var(--intensite-${intensite})`, color: intensite >= 6 ? '#ffffff' : 'var(--couleur-texte)' };
}

/** Lance l'action ; ferme le panneau si elle réussit, affiche le message sinon. */
async function executer(action: () => Promise<void>, message: string) {
  envoi.value = true;
  erreur.value = null;
  try {
    await action();
    ouvert.value = false;
  } catch {
    erreur.value = message;
  } finally {
    envoi.value = false;
  }
}

async function valider() {
  if (!form.complete.value || envoi.value) return;
  await executer(() => props.actions.enregistrer(form.saisie(), form.id.value),
      'La douleur n\'a pas pu être enregistrée. Réessaie dans un instant.');
}

async function supprimer() {
  if (!confirmerSuppression.value) {
    confirmerSuppression.value = true;
    return;
  }
  await executer(() => props.actions.supprimer(form.id.value!),
      'La douleur n\'a pas pu être supprimée. Réessaie dans un instant.');
}
</script>
