<script setup lang="ts">
import { computed, ref, toRef } from 'vue';
import { format, isToday } from 'date-fns';
import { fr } from 'date-fns/locale';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/shared/components/ui/dialog';
import EchellePastilles from '@/features/bilan-quotidien/components/saisie/EchellePastilles.vue';
import EmotionsChoix from '@/features/bilan-quotidien/components/saisie/EmotionsChoix.vue';
import BlocRepliable from '@/features/bilan-quotidien/components/saisie/BlocRepliable.vue';
import BlocCorps from '@/features/bilan-quotidien/components/saisie/BlocCorps.vue';
import { corpsDe, resumeCorps, useSaisieBilan } from '@/features/bilan-quotidien/composables/useSaisieBilan';
import { useConfirmationSortie } from '@/features/bilan-quotidien/composables/useConfirmationSortie';
import { DOULEUR_MAX, ECHELLE_MAX, reperesEchelles } from '@/features/bilan-quotidien/config/saisie';
import { anciennesHumeurs, COMMENTAIRE_MAX } from '@/features/bilan-quotidien/config/emotions';
import type { BilanQuotidien } from '@/features/bilan-quotidien/types/bilan-quotidien';
import type { CorpsBilan } from '@/features/bilan-quotidien/types/saisie-bilan';

// Bilan quotidien en un écran : Essentiel (douleur et émotions obligatoires), Corps et Notes facultatifs.
const props = defineProps<{
  carnetSanteId: number;
  date: Date;
  /** Bilan à modifier ; absent = création. */
  bilan?: BilanQuotidien;
  bilanVeille?: BilanQuotidien;
  /** Affiche « Annuler » (il existe un écran où revenir). */
  annulable: boolean;
}>();

const emit = defineEmits<{
  enregistre: [bilan: BilanQuotidien];
  annule: [];
}>();

const saisie = useSaisieBilan({
  carnetSanteId: props.carnetSanteId,
  date: props.date,
  bilan: props.bilan,
  bilanVeille: toRef(props, 'bilanVeille'),
  onEnregistre: (bilan) => emit('enregistre', bilan),
});
const { formulaire, manquants, transitComplet, estEnregistrable, estModifie, enregistrement, veilleDisponible } = saisie;

const confirmation = useConfirmationSortie(estModifie);

const corpsOuvert = ref(saisie.corpsInitialRenseigne);
const notesOuvertes = ref(formulaire.value.commentaire !== '');

const corps = computed<CorpsBilan>({
  get: () => corpsDe(formulaire.value),
  set: (valeur) => {
    formulaire.value = { ...formulaire.value, ...valeur };
  },
});

const titre = computed(() => {
  const jour = isToday(props.date) ? "d'aujourd'hui" : `du ${format(props.date, 'd MMMM', { locale: fr })}`;
  return saisie.estModification ? `Modifier le bilan ${jour}` : `Bilan ${jour}`;
});

const ancienneHumeur = computed(() => (saisie.ancienneHumeur ? anciennesHumeurs[saisie.ancienneHumeur] : undefined));

const aide = computed(() => {
  if (manquants.value.length) {
    const libelles = { douleur: 'douleur', emotions: 'une émotion' };
    return `À renseigner : ${manquants.value.map((m) => libelles[m]).join(', ')}`;
  }
  return transitComplet.value ? null : 'Transit : choisis une intensité';
});

const reprendreHier = () => {
  saisie.reprendreHier();
  corpsOuvert.value = true;
};

const annuler = () => confirmation.demanderSiNecessaire(() => emit('annule'));
</script>

<template>
  <section class="flex w-full flex-col rounded-carte bg-surface shadow-elevation" aria-labelledby="titre-saisie-bilan">
    <div class="flex flex-col gap-6 px-4 pb-6 pt-5 md:p-6">
      <header class="flex flex-col gap-1">
        <h2 id="titre-saisie-bilan" class="m-0 text-xl font-semibold tracking-normal text-texte">{{ titre }}</h2>
        <p class="m-0 text-sm text-texte-2">Seules la douleur et les émotions sont nécessaires, le reste est facultatif.</p>
      </header>

      <!-- Essentiel -->
      <div class="flex flex-col gap-5">
        <EchellePastilles
            v-model="formulaire.douleurMoyenne"
            couleurs
            libelle="Douleur"
            icone="sick"
            :max="DOULEUR_MAX"
            :repere-min="reperesEchelles.douleur.min"
            :repere-max="reperesEchelles.douleur.max"
        />

        <div class="flex flex-col gap-2">
          <EmotionsChoix v-model="formulaire.emotions"/>
          <p v-if="ancienneHumeur && !formulaire.emotions.length" class="m-0 text-xs text-texte-2">
            Humeur enregistrée à l'époque : {{ ancienneHumeur.libelle }}. Tu peux la préciser avec des émotions, sans obligation.
          </p>
        </div>

        <EchellePastilles
            v-model="formulaire.fatigue"
            libelle="Fatigue"
            icone="bedtime"
            :max="ECHELLE_MAX"
            :repere-min="reperesEchelles.fatigue.min"
            :repere-max="reperesEchelles.fatigue.max"
        />
        <EchellePastilles
            v-model="formulaire.stressPro"
            libelle="Stress · vie pro"
            icone="work"
            :max="ECHELLE_MAX"
            :repere-min="reperesEchelles.stress.min"
            :repere-max="reperesEchelles.stress.max"
        />
        <EchellePastilles
            v-model="formulaire.stressPerso"
            libelle="Stress · vie perso"
            icone="home"
            :max="ECHELLE_MAX"
            :repere-min="reperesEchelles.stress.min"
            :repere-max="reperesEchelles.stress.max"
        />
      </div>

      <div v-if="veilleDisponible" class="flex flex-wrap items-center gap-2">
        <button type="button" class="inline-flex min-h-11 items-center justify-center gap-2 rounded-controle border-[1.5px] border-contour px-3.5 text-sm font-medium text-texte" @click="reprendreHier">
          <i class="material-symbols-outlined text-lg" aria-hidden="true">content_copy</i>Comme hier
        </button>
        <span class="text-xs text-texte-2">Reprend le transit, l'alimentation, les pas et l'hydratation de la veille.</span>
      </div>

      <BlocRepliable v-model:ouvert="corpsOuvert" titre="Corps" icone="accessibility_new" :resume="resumeCorps(corps)">
        <BlocCorps v-model="corps"/>
      </BlocRepliable>

      <BlocRepliable
          v-model:ouvert="notesOuvertes"
          titre="Notes"
          icone="edit_note"
          :resume="formulaire.commentaire || 'Un événement, un ressenti, un repas…'"
      >
        <label for="notes-bilan" class="sr-only">Notes personnelles</label>
        <textarea
            id="notes-bilan"
            v-model="formulaire.commentaire"
            placeholder="Un événement, un ressenti, un repas…"
            class="min-h-[120px] w-full resize-y rounded-controle border-[1.5px] border-contour bg-champ p-3 text-[15px] text-texte"
            :maxlength="COMMENTAIRE_MAX"
        ></textarea>
        <p class="-mt-4 text-right text-xs text-texte-3">{{ formulaire.commentaire.length }}/{{ COMMENTAIRE_MAX }}</p>
      </BlocRepliable>
    </div>

    <!-- Barre d'enregistrement toujours visible -->
    <div class="barre-enregistrement sticky z-10 flex flex-col gap-2 rounded-b-carte border-t border-trait px-4 py-3 md:px-6">
      <p v-if="aide" class="m-0 truncate text-sm text-texte-2" aria-live="polite">{{ aide }}</p>
      <div class="flex gap-2">
        <button v-if="annulable" type="button" class="inline-flex min-h-11 items-center justify-center gap-2 rounded-controle border-[1.5px] border-contour px-3.5 text-sm font-medium text-texte min-h-12 flex-1" @click="annuler">
          Annuler
        </button>
        <button
            type="button"
            class="inline-flex min-h-12 items-center justify-center gap-2 rounded-controle bg-button px-4 text-base font-semibold text-texte disabled:opacity-50 flex-[2]"
            :disabled="!estEnregistrable"
            @click="saisie.enregistrer"
        >
          {{ enregistrement ? 'Enregistrement…' : 'Enregistrer' }}
        </button>
      </div>
    </div>

    <Dialog :open="confirmation.demandeOuverte.value" @update:open="(ouvert) => !ouvert && confirmation.rester()">
      <DialogContent class="max-w-[calc(100vw-2rem)] rounded-carte sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Quitter sans enregistrer ?</DialogTitle>
          <DialogDescription>Les réponses de ce bilan qui ne sont pas enregistrées seront perdues.</DialogDescription>
        </DialogHeader>
        <DialogFooter class="flex flex-col-reverse gap-2 sm:flex-row">
          <button type="button" class="inline-flex min-h-11 items-center justify-center gap-2 rounded-controle border-[1.5px] border-contour px-3.5 text-sm font-medium text-texte" @click="confirmation.quitter">Quitter sans enregistrer</button>
          <button type="button" class="inline-flex min-h-11 items-center justify-center rounded-controle bg-button px-4 text-sm font-semibold text-texte" @click="confirmation.rester">Continuer la saisie</button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  </section>
</template>

<style scoped>
.barre-enregistrement {
  bottom: 0;
  background: var(--couleur-surface);
  box-shadow: 0 -4px 8px -6px rgb(51 39 42 / 0.15);
}

/* Sous 1024px, la navigation est fixée en bas de l'écran (BarreNavigation.vue, 4.25rem) : la barre se pose au-dessus. */
@media (max-width: 1023px) {
  .barre-enregistrement {
    bottom: 4.25rem;
  }
}
</style>
