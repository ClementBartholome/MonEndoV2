<script setup lang="ts">
import { computed, ref, toRef } from 'vue';
import { format, isToday } from 'date-fns';
import { fr } from 'date-fns/locale';
import { Button } from '@/shared/components/ui/button';
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
  <section class="w-full max-w-2xl mx-auto bg-clearer rounded-3xl shadow-xl flex flex-col" aria-labelledby="titre-saisie-bilan">
    <div class="px-4 pt-5 pb-6 md:p-6 flex flex-col gap-6">
      <header class="flex flex-col gap-1">
        <h2 id="titre-saisie-bilan" class="text-xl font-bold text-headline">{{ titre }}</h2>
        <p class="text-sm text-paragraph">Seules la douleur et les émotions sont nécessaires, le reste est facultatif.</p>
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
          <p v-if="ancienneHumeur && !formulaire.emotions.length" class="text-xs text-paragraph">
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
        <Button type="button" variant="outline" class="h-11 gap-2" @click="reprendreHier">
          <i class="material-symbols-outlined text-lg" aria-hidden="true">content_copy</i>Comme hier
        </Button>
        <span class="text-xs text-paragraph">Reprend le transit, l'alimentation, les pas et l'hydratation de la veille.</span>
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
            class="w-full p-3 rounded-xl border-2 border-gray-200 focus:border-button focus:outline-none resize-y min-h-[120px] bg-form-input text-paragraph"
            :maxlength="COMMENTAIRE_MAX"
        ></textarea>
        <p class="text-xs text-paragraph text-right -mt-4">{{ formulaire.commentaire.length }}/{{ COMMENTAIRE_MAX }}</p>
      </BlocRepliable>
    </div>

    <!-- Barre d'enregistrement toujours visible -->
    <div class="barre-enregistrement sticky z-10 px-4 py-3 md:px-6 rounded-b-3xl border-t border-gray-200 flex flex-col gap-2">
      <p v-if="aide" class="text-sm text-paragraph truncate" aria-live="polite">{{ aide }}</p>
      <div class="flex gap-2">
        <Button v-if="annulable" type="button" variant="outline" class="h-12 flex-1 px-3" @click="annuler">
          Annuler
        </Button>
        <Button
            type="button"
            variant="custom"
            class="h-12 flex-[2] gap-2 text-base"
            :disabled="!estEnregistrable"
            @click="saisie.enregistrer"
        >
          <i class="material-symbols-outlined" aria-hidden="true">check_circle</i>
          {{ enregistrement ? 'Enregistrement…' : 'Enregistrer' }}
        </Button>
      </div>
    </div>

    <Dialog :open="confirmation.demandeOuverte.value" @update:open="(ouvert) => !ouvert && confirmation.rester()">
      <DialogContent class="max-w-[calc(100vw-2rem)] sm:max-w-md rounded-2xl">
        <DialogHeader>
          <DialogTitle>Quitter sans enregistrer ?</DialogTitle>
          <DialogDescription>Les réponses de ce bilan qui ne sont pas enregistrées seront perdues.</DialogDescription>
        </DialogHeader>
        <DialogFooter class="flex flex-col-reverse gap-2 sm:flex-row">
          <Button type="button" variant="outline" class="h-11" @click="confirmation.quitter">Quitter sans enregistrer</Button>
          <Button type="button" variant="custom" class="h-11" @click="confirmation.rester">Continuer la saisie</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  </section>
</template>

<style scoped>
.barre-enregistrement {
  bottom: 0;
  background: var(--couleur-fond);
  box-shadow: 0 -4px 8px -6px rgba(0, 0, 0, 0.15);
}

/* Sous 1024px, la navigation est fixée en bas de l'écran (BarreNavigation.vue, 4.25rem) : la barre se pose au-dessus. */
@media (max-width: 1023px) {
  .barre-enregistrement {
    bottom: 4.25rem;
  }
}
</style>
