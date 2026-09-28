<template>
  <PanneauBas v-model:open="ouvert" :titre="form.enModification.value ? 'Modifier le traitement' : 'Ajouter un traitement'">
    <form class="flex flex-col gap-[18px]" @submit.prevent="valider">
      <fieldset v-if="!form.enModification.value" class="m-0 border-0 p-0">
        <legend class="mb-2.5 p-0 text-sm font-semibold text-texte">Type</legend>
        <div class="grid grid-cols-2 gap-2">
          <button v-for="t in types" :key="t.valeur" type="button"
                  class="min-h-11 rounded-xl text-sm"
                  :class="form.type.value === t.valeur ? 'border-[1.5px] border-texte bg-texte font-medium text-fond' : 'border-[1.5px] border-trait bg-surface text-texte'"
                  :aria-pressed="form.type.value === t.valeur"
                  @click="choisirType(t.valeur)">{{ t.libelle }}</button>
        </div>
      </fieldset>

      <label class="flex flex-col gap-1.5 text-sm font-semibold text-texte">
        Nom
        <input v-model="form.nom.value" type="text" required maxlength="100"
               :placeholder="medicament ? 'Dienogest, ibuprofène…' : 'Kiné, ostéopathie, TENS…'"
               class="min-h-11 rounded-xl border-[1.5px] border-contour bg-white px-3 text-[15px] font-normal text-texte">
      </label>

      <template v-if="medicament">
        <label class="flex flex-col gap-1.5 text-sm font-semibold text-texte">
          <span>Dose <span class="font-normal text-texte-3">(facultatif)</span></span>
          <input v-model="form.dose.value" type="text" maxlength="100" placeholder="1 comprimé, 400 mg…"
                 class="min-h-11 rounded-xl border-[1.5px] border-contour bg-white px-3 text-[15px] font-normal text-texte">
        </label>

        <fieldset class="m-0 border-0 p-0">
          <legend class="mb-2.5 p-0 text-sm font-semibold text-texte">Fréquence</legend>
          <div class="grid grid-cols-2 gap-2">
            <button v-for="f in frequences" :key="f.valeur" type="button"
                    class="min-h-11 rounded-xl px-2 text-sm"
                    :class="form.frequence.value === f.valeur ? 'border-[1.5px] border-texte bg-texte font-medium text-fond' : 'border-[1.5px] border-trait bg-surface text-texte'"
                    :aria-pressed="form.frequence.value === f.valeur"
                    @click="form.frequence.value = f.valeur">{{ f.libelle }}</button>
          </div>

          <div v-if="form.frequence.value === 'CertainsJours'" class="mt-3 grid grid-cols-7 gap-1" role="group" aria-label="Jours de prise">
            <button v-for="jour in JOURS_SEMAINE" :key="jour" type="button"
                    class="min-h-11 rounded-full p-0 text-[13px]"
                    :class="form.joursSemaine.value.includes(jour) ? 'bg-texte font-semibold text-fond' : 'border-[1.5px] border-trait bg-surface text-texte'"
                    :aria-label="jour" :aria-pressed="form.joursSemaine.value.includes(jour)"
                    @click="form.basculerJour(jour)">{{ jour.charAt(0) }}</button>
          </div>

          <label v-if="form.frequence.value === 'TousLesNJours'" class="mt-3 flex items-center gap-2 text-[15px] text-texte">
            Tous les
            <input v-model.number="form.intervalleJours.value" type="number" min="2" max="30" required
                   class="min-h-11 w-20 rounded-xl border-[1.5px] border-contour bg-white px-3 text-center text-[15px] text-texte">
            jours, à partir du début
          </label>
        </fieldset>

        <fieldset v-if="form.avecHoraires.value" class="m-0 border-0 p-0">
          <legend class="mb-2.5 p-0 text-sm font-semibold text-texte">Horaires</legend>
          <div class="flex flex-col gap-2">
            <div v-for="(_, index) in form.horaires.value" :key="index" class="flex items-center gap-2">
              <input v-model="form.horaires.value[index]" type="time" required :aria-label="`Horaire ${index + 1}`"
                     class="min-h-11 grow rounded-xl border-[1.5px] border-contour bg-white px-3 text-[15px] text-texte">
              <button v-if="form.horaires.value.length > 1" type="button"
                      class="flex h-11 w-11 shrink-0 items-center justify-center rounded-full text-texte-3 hover:bg-surface-2"
                      :aria-label="`Retirer l'horaire ${index + 1}`" @click="form.retirerHoraire(index)">
                <i class="material-symbols-outlined" aria-hidden="true">close</i>
              </button>
            </div>
            <button v-if="form.horaires.value.length < 6" type="button"
                    class="inline-flex min-h-11 items-center gap-1.5 self-start text-sm font-medium text-lien"
                    @click="form.ajouterHoraire">
              <i class="material-symbols-outlined text-lg" aria-hidden="true">add</i>Ajouter un horaire
            </button>
          </div>
        </fieldset>
      </template>

      <div class="grid grid-cols-2 gap-2">
        <label class="flex flex-col gap-1 text-[13px] text-texte-2">Début
          <input v-model="form.dateDebut.value" type="date" required
                 class="min-h-11 rounded-xl border-[1.5px] border-contour bg-white px-3 text-[15px] text-texte">
        </label>
        <label class="flex flex-col gap-1 text-[13px] text-texte-2"><span>Fin <span class="text-texte-3">(facultatif)</span></span>
          <input v-model="form.dateFin.value" type="date" :min="form.dateDebut.value"
                 class="min-h-11 rounded-xl border-[1.5px] border-contour bg-white px-3 text-[15px] text-texte">
        </label>
      </div>

      <p v-if="erreur" role="alert" class="m-0 text-sm text-danger">{{ erreur }}</p>

      <button type="submit" :disabled="!form.complete.value || envoi"
              class="min-h-[52px] rounded-[14px] bg-button text-base font-semibold text-texte disabled:opacity-50">
        {{ envoi ? 'Enregistrement…' : 'Enregistrer' }}
      </button>

      <button v-if="form.enModification.value && !props.traitement?.dateFin" type="button"
              class="min-h-11 rounded-[14px] text-sm font-medium text-danger"
              :class="{ 'border-[1.5px] border-danger': confirmerArret }"
              @click="arreter">
        {{ confirmerArret ? 'Confirmer l\'arrêt aujourd\'hui' : 'Arrêter ce traitement' }}
      </button>
    </form>
  </PanneauBas>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import PanneauBas from '@/shared/components/PanneauBas.vue';
import { JOURS_SEMAINE, type FrequencePrise, type Traitement, type TraitementSaisie, type TypeTraitement } from '../types/traitements';
import { useSaisieTraitement } from '../composables/useSaisieTraitement';

const ouvert = defineModel<boolean>('open', { required: true });
/** Enregistrement et arrêt, fournis par la page (le composant n'appelle jamais l'API). */
export interface ActionsSaisieTraitement {
  enregistrer: (saisie: TraitementSaisie, id: number | null) => Promise<void>;
  arreter: (id: number) => Promise<void>;
}

const props = defineProps<{ traitement: Traitement | null; actions: ActionsSaisieTraitement }>();

const form = useSaisieTraitement();
const envoi = ref(false);
const erreur = ref<string | null>(null);
const confirmerArret = ref(false);
const medicament = computed(() => form.type.value === 'Medicamenteux');

const types: { valeur: TypeTraitement; libelle: string }[] = [
  { valeur: 'Medicamenteux', libelle: 'Médicament' },
  { valeur: 'NonMedicamenteux', libelle: 'Soin' },
];

const frequences: { valeur: FrequencePrise; libelle: string }[] = [
  { valeur: 'ChaqueJour', libelle: 'Tous les jours' },
  { valeur: 'CertainsJours', libelle: 'Certains jours' },
  { valeur: 'TousLesNJours', libelle: 'Tous les N jours' },
  { valeur: 'AuBesoin', libelle: 'Au besoin' },
];

/** Un soin (kiné, ostéo…) se note par séance : il n'a ni dose ni horaires. */
function choisirType(type: TypeTraitement) {
  form.type.value = type;
  form.frequence.value = type === 'Medicamenteux' ? 'ChaqueJour' : 'AuBesoin';
}

watch(ouvert, (estOuvert) => {
  if (!estOuvert) return;
  erreur.value = null;
  confirmerArret.value = false;
  if (props.traitement) form.preparerModification(props.traitement);
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
  await executer(() => props.actions.enregistrer(form.saisie(), form.id.value),
      'Le traitement n\'a pas pu être enregistré. Vérifie les champs et réessaie.');
}

async function arreter() {
  if (!confirmerArret.value) {
    confirmerArret.value = true;
    return;
  }
  await executer(() => props.actions.arreter(form.id.value!),
      'Le traitement n\'a pas pu être arrêté. Réessaie dans un instant.');
}
</script>
