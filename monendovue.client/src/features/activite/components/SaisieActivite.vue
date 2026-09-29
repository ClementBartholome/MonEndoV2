<template>
  <PanneauBas v-model:open="ouvert" :titre="form.enModification.value ? 'Modifier l\'activité' : 'Noter une activité'">
    <form class="flex flex-col gap-[18px]" @submit.prevent="valider">
      <fieldset class="m-0 border-0 p-0">
        <legend class="mb-2.5 p-0 text-sm font-semibold text-texte">Quelle activité ?</legend>
        <div class="grid grid-cols-3 gap-2">
          <button v-for="t in types" :key="t.valeur" type="button"
                  class="flex min-h-14 flex-col items-center justify-center gap-0.5 rounded-controle border-[1.5px] px-1 text-[13px]"
                  :class="form.type.value === t.valeur ? 'border-texte bg-texte font-medium text-fond' : 'border-trait bg-surface text-texte'"
                  :aria-pressed="form.type.value === t.valeur"
                  :aria-label="t.valeur === 'Autre' ? 'Autre activité' : undefined"
                  @click="form.type.value = t.valeur">
            <i class="material-symbols-outlined text-xl" aria-hidden="true">{{ t.icone }}</i>{{ t.valeur }}
          </button>
        </div>
        <label v-if="form.type.value === 'Autre'" class="mt-3 flex flex-col gap-1 text-[13px] text-texte-2">Laquelle ?
          <input v-model="form.typeLibre.value" type="text" required maxlength="50" placeholder="Danse, pilates…"
                 class="min-h-11 rounded-controle border-[1.5px] border-contour bg-white px-3 text-[15px] text-texte">
        </label>
      </fieldset>

      <fieldset class="m-0 border-0 p-0">
        <legend class="mb-2.5 p-0 text-sm font-semibold text-texte">Durée</legend>
        <div class="grid grid-cols-4 gap-2">
          <button v-for="minutes in DUREES" :key="minutes" type="button"
                  class="min-h-11 rounded-controle border-[1.5px] text-sm"
                  :class="form.duree.value === minutes ? 'border-texte bg-texte font-medium text-fond' : 'border-trait bg-surface text-texte'"
                  :aria-pressed="form.duree.value === minutes"
                  @click="form.duree.value = minutes">{{ duree(minutes) }}</button>
        </div>
        <label class="mt-3 flex items-center gap-2 text-[13px] text-texte-2">Autre durée
          <input v-model.number="form.duree.value" type="number" min="1" max="600" inputmode="numeric"
                 class="min-h-11 w-24 rounded-controle border-[1.5px] border-contour bg-white px-3 text-center text-[15px] text-texte">
          minutes
        </label>
      </fieldset>

      <fieldset class="m-0 border-0 p-0">
        <legend class="mb-2.5 p-0 text-sm font-semibold text-texte">Intensité ressentie</legend>
        <div class="grid grid-cols-3 gap-2">
          <button v-for="n in NIVEAUX" :key="n.valeur" type="button"
                  class="min-h-11 rounded-controle border-[1.5px] text-sm"
                  :class="form.niveau.value === n.valeur ? 'border-texte bg-texte font-medium text-fond' : 'border-trait bg-surface text-texte'"
                  :aria-pressed="form.niveau.value === n.valeur"
                  @click="form.niveau.value = n.valeur">{{ n.libelle }}</button>
        </div>
      </fieldset>

      <fieldset class="m-0 border-0 p-0">
        <legend class="mb-2.5 p-0 text-sm font-semibold text-texte">Et la douleur, après ? <span class="font-normal text-texte-3">(facultatif)</span></legend>
        <div class="grid grid-cols-3 gap-2">
          <button v-for="e in EFFETS" :key="e.valeur" type="button"
                  class="min-h-11 rounded-controle border-[1.5px] text-sm"
                  :class="form.effet.value === e.valeur ? 'border-texte bg-texte font-medium text-fond' : 'border-trait bg-surface text-texte'"
                  :aria-pressed="form.effet.value === e.valeur"
                  @click="form.effet.value = form.effet.value === e.valeur ? 'NonRenseigne' : e.valeur">{{ e.libelle }}</button>
        </div>
      </fieldset>

      <fieldset class="m-0 border-0 p-0">
        <legend class="mb-2.5 p-0 text-sm font-semibold text-texte">Quand ?</legend>
        <div class="grid grid-cols-3 gap-2">
          <button v-for="m in moments" :key="m.valeur" type="button"
                  class="min-h-11 rounded-controle border-[1.5px] text-sm"
                  :class="form.moment.value === m.valeur ? 'border-texte bg-texte font-medium text-fond' : 'border-trait bg-surface text-texte'"
                  :aria-pressed="form.moment.value === m.valeur"
                  :aria-label="m.valeur === 'autre' ? 'Autre jour' : undefined"
                  @click="form.moment.value = m.valeur">{{ m.libelle }}</button>
        </div>
        <label v-if="form.moment.value === 'autre'" class="mt-3 flex flex-col gap-1 text-[13px] text-texte-2">Jour
          <input v-model="form.jour.value" type="date" required :max="aujourdhui"
                 class="min-h-11 rounded-controle border-[1.5px] border-contour bg-white px-3 text-[15px] text-texte">
        </label>
      </fieldset>

      <label class="flex flex-col gap-1.5 text-sm font-semibold text-texte">
        <span>Commentaire <span class="font-normal text-texte-3">(facultatif)</span></span>
        <textarea v-model="form.commentaire.value" rows="2" maxlength="500" placeholder="Comment tu t'es sentie…"
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
        {{ confirmerSuppression ? 'Confirmer la suppression' : 'Supprimer cette activité' }}
      </button>
    </form>
  </PanneauBas>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { format } from 'date-fns';
import PanneauBas from '@/shared/components/PanneauBas.vue';
import type { Activite, ActiviteSaisie } from '../types/activite';
import { DUREES, EFFETS, NIVEAUX, TYPES_ACTIVITE, duree } from '../utils/activite';
import { useSaisieActivite, type Moment } from '../composables/useSaisieActivite';

const ouvert = defineModel<boolean>('open', { required: true });
/** Enregistrement et suppression, fournis par la page (le composant n'appelle jamais l'API). */
export interface ActionsSaisieActivite {
  enregistrer: (saisie: ActiviteSaisie, id: number | null) => Promise<void>;
  supprimer: (id: number) => Promise<void>;
}

const props = defineProps<{ activite: Activite | null; actions: ActionsSaisieActivite }>();

const form = useSaisieActivite();
const envoi = ref(false);
const erreur = ref<string | null>(null);
const confirmerSuppression = ref(false);
const aujourdhui = format(new Date(), 'yyyy-MM-dd');

const types = [...TYPES_ACTIVITE, { valeur: 'Autre', icone: 'more_horiz' }];
const moments: { valeur: Moment; libelle: string }[] = [
  { valeur: 'aujourdhui', libelle: 'Aujourd\'hui' },
  { valeur: 'hier', libelle: 'Hier' },
  { valeur: 'autre', libelle: 'Autre' },
];

watch(ouvert, (estOuvert) => {
  if (!estOuvert) return;
  erreur.value = null;
  confirmerSuppression.value = false;
  if (props.activite) form.preparerModification(props.activite);
  else form.preparerAjout();
}, { immediate: true });

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
  await executer(() => props.actions.enregistrer(form.saisie(props.activite?.date ?? null), form.id.value),
      'L\'activité n\'a pas pu être enregistrée. Réessaie dans un instant.');
}

async function supprimer() {
  if (!confirmerSuppression.value) {
    confirmerSuppression.value = true;
    return;
  }
  await executer(() => props.actions.supprimer(form.id.value!),
      'L\'activité n\'a pas pu être supprimée. Réessaie dans un instant.');
}
</script>
