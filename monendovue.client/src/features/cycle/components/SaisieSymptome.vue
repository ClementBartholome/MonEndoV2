<template>
  <PanneauBas v-model:open="ouvert" :titre="titre">
    <form class="flex flex-col gap-[18px]" @submit.prevent="valider">
      <fieldset v-if="!acne" class="m-0 border-0 p-0">
        <legend class="mb-2.5 p-0 text-sm font-semibold text-texte">Quel symptôme ?</legend>
        <div class="grid grid-cols-2 gap-2">
          <button v-for="t in TYPES_SYMPTOME" :key="t.valeur" type="button"
                  class="inline-flex min-h-12 items-center justify-center gap-2 rounded-controle border-[1.5px] px-3 text-sm"
                  :class="form.type.value === t.valeur ? 'border-texte bg-texte font-medium text-fond' : 'border-trait bg-surface text-texte'"
                  :aria-pressed="form.type.value === t.valeur"
                  @click="form.type.value = t.valeur">
            <i class="material-symbols-outlined text-xl" aria-hidden="true">{{ t.icone }}</i>{{ t.valeur }}
          </button>
        </div>
      </fieldset>

      <ChoixIntensite v-model="form.intensite.value" minimum="léger" maximum="très fort"/>

      <fieldset class="m-0 border-0 p-0">
        <legend class="mb-2.5 p-0 text-sm font-semibold text-texte">Quand ?</legend>
        <div class="grid grid-cols-3 gap-2">
          <button v-for="m in moments" :key="m.valeur" type="button"
                  class="min-h-11 rounded-controle border-[1.5px] text-sm"
                  :class="form.moment.value === m.valeur ? 'border-texte bg-texte font-medium text-fond' : 'border-trait bg-surface text-texte'"
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

      <fieldset v-if="acne" class="m-0 border-0 p-0">
        <legend class="mb-2.5 p-0 text-sm font-semibold text-texte">Photo <span class="font-normal text-texte-3">(facultative)</span></legend>
        <div v-if="photo.apercu.value || form.photoExistante.value" class="mb-2.5 flex items-center gap-3">
          <img :src="photo.apercu.value || form.photoExistante.value!" alt="Photo choisie" class="h-16 w-16 rounded-controle object-cover">
          <span class="grow text-[13px] text-texte-2">{{ photo.apercu.value ? 'Nouvelle photo' : 'Photo enregistrée' }}</span>
          <button v-if="photo.apercu.value" type="button" class="min-h-11 px-2 text-sm font-medium text-lien" @click="photo.retirer">Retirer</button>
        </div>
        <div class="grid grid-cols-2 gap-2">
          <label class="inline-flex min-h-11 cursor-pointer items-center justify-center gap-2 rounded-controle border-[1.5px] border-contour text-sm font-medium text-texte"
                 :class="{ 'pointer-events-none opacity-60': photo.preparation.value }">
            <i class="material-symbols-outlined text-xl" aria-hidden="true">photo_camera</i>Prendre
            <input class="sr-only" type="file" accept="image/*" capture="environment"
                   @change="(e) => choisirPhoto(e, 'camera')">
          </label>
          <label class="inline-flex min-h-11 cursor-pointer items-center justify-center gap-2 rounded-controle border-[1.5px] border-contour text-sm font-medium text-texte"
                 :class="{ 'pointer-events-none opacity-60': photo.preparation.value }">
            <i class="material-symbols-outlined text-xl" aria-hidden="true">photo_library</i>Galerie
            <input class="sr-only" type="file" accept="image/*,.heic,.heif"
                   @change="(e) => choisirPhoto(e, 'gallery')">
          </label>
        </div>
        <p v-if="photo.preparation.value" class="m-0 mt-2 text-[13px] text-texte-2" aria-live="polite">Préparation de la photo…</p>
        <p v-else-if="photo.message.value" class="m-0 mt-2 text-[13px] text-danger" role="alert">{{ photo.message.value }}</p>
      </fieldset>

      <label class="flex flex-col gap-1.5 text-sm font-semibold text-texte">
        <span>Commentaire <span class="font-normal text-texte-3">(facultatif)</span></span>
        <textarea v-model="form.commentaire.value" rows="2" maxlength="500"
                  :placeholder="acne ? 'Zone, nouveau soin…' : 'Ce qui l\'a déclenché, ce qui a aidé…'"
                  class="resize-none rounded-controle border-[1.5px] border-contour bg-white px-3 py-2.5 text-[15px] font-normal text-texte"></textarea>
      </label>

      <p v-if="erreur" role="alert" class="m-0 text-sm text-danger">{{ erreur }}</p>

      <button type="submit" :disabled="!form.complete.value || envoi || photo.preparation.value"
              class="min-h-[52px] rounded-controle bg-button text-base font-semibold text-texte disabled:opacity-50">
        {{ envoi ? 'Enregistrement…' : 'Enregistrer' }}
      </button>

      <button v-if="form.enModification.value" type="button"
              class="min-h-11 rounded-controle text-sm font-medium text-danger"
              :class="{ 'border-[1.5px] border-danger': confirmerSuppression }"
              @click="supprimer">
        {{ confirmerSuppression ? 'Confirmer la suppression' : 'Supprimer ce symptôme' }}
      </button>
    </form>
  </PanneauBas>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { format } from 'date-fns';
import PanneauBas from '@/shared/components/PanneauBas.vue';
import ChoixIntensite from '@/shared/components/ChoixIntensite.vue';
import type { SymptomeCycle, SymptomeSaisie } from '../types/symptome-cycle';
import { ACNE, TYPES_SYMPTOME } from '../utils/symptomes';
import { useSaisieSymptome, type Moment } from '../composables/useSaisieSymptome';
import { usePhotoSymptome, type SourcePhoto } from '../composables/usePhotoSymptome';

const ouvert = defineModel<boolean>('open', { required: true });
/** Enregistrement et suppression, fournis par la page (le composant n'appelle jamais l'API). */
export interface ActionsSaisieSymptome {
  enregistrer: (saisie: SymptomeSaisie, id: number | null) => Promise<void>;
  supprimer: (id: number) => Promise<void>;
}

/** `acne` : saisie depuis l'onglet Acné (type imposé, photo proposée). */
const props = defineProps<{ entree: SymptomeCycle | null; acne: boolean; actions: ActionsSaisieSymptome }>();

const form = useSaisieSymptome();
const photo = usePhotoSymptome();
const envoi = ref(false);
const erreur = ref<string | null>(null);
const confirmerSuppression = ref(false);
const aujourdhui = format(new Date(), 'yyyy-MM-dd');

const moments: { valeur: Moment; libelle: string }[] = [
  { valeur: 'maintenant', libelle: 'Maintenant' },
  { valeur: 'matin', libelle: 'Ce matin' },
  { valeur: 'autre', libelle: 'Autre' },
];

const titre = computed(() => {
  if (props.acne) return form.enModification.value ? 'Modifier le suivi' : 'Noter mon acné';
  return form.enModification.value ? 'Modifier le symptôme' : 'Noter un symptôme';
});

watch(ouvert, (estOuvert) => {
  if (!estOuvert) return;
  erreur.value = null;
  confirmerSuppression.value = false;
  photo.retirer();
  if (props.entree) form.preparerModification(props.entree);
  else form.preparerAjout(props.acne ? ACNE : null);
}, { immediate: true });

async function choisirPhoto(evenement: Event, source: SourcePhoto) {
  const champ = evenement.target as HTMLInputElement;
  await photo.choisir(champ.files?.[0] ?? null, source);
  // Permet de choisir à nouveau le même fichier après un retrait.
  champ.value = '';
}

async function executer(action: () => Promise<void>, message: string) {
  envoi.value = true;
  erreur.value = null;
  try {
    await action();
    ouvert.value = false;
  } catch (e) {
    const statut = (e as { response?: { status?: number } })?.response?.status;
    erreur.value = statut === 413 ? 'La photo est trop lourde pour être envoyée. Choisis-en une plus légère.' : message;
  } finally {
    envoi.value = false;
  }
}

async function valider() {
  if (!form.complete.value || envoi.value) return;
  const saisie: SymptomeSaisie = {
    typeSymptome: form.type.value!,
    date: form.dateChoisie(),
    intensite: form.intensite.value!,
    commentaire: form.commentaire.value.trim() || null,
    photo: photo.fichier.value,
    photoSource: photo.source.value,
  };
  await executer(() => props.actions.enregistrer(saisie, form.id.value),
      'Le symptôme n\'a pas pu être enregistré. Réessaie dans un instant.');
}

async function supprimer() {
  if (!confirmerSuppression.value) {
    confirmerSuppression.value = true;
    return;
  }
  await executer(() => props.actions.supprimer(form.id.value!),
      'Le symptôme n\'a pas pu être supprimé. Réessaie dans un instant.');
}
</script>
