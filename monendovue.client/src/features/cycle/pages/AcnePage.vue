<template>
  <main class="mx-auto flex w-full max-w-xl flex-col gap-3.5 px-5 pb-40 pt-24 lg:pb-12 lg:pt-10">
    <header class="flex items-center gap-3">
      <h1 class="m-0 grow text-titre-page font-medium tracking-normal text-texte">Acné</h1>
    </header>

    <div v-if="acne.chargement.value" class="flex flex-col gap-3.5" aria-busy="true" aria-label="Chargement du suivi de l'acné">
      <Skeleton class="h-24 rounded-carte"/>
      <Skeleton class="h-36 rounded-carte"/>
    </div>
    <section v-else-if="acne.erreur.value" role="alert" class="flex flex-col items-start gap-3 rounded-carte bg-surface p-5 shadow-elevation">
      <p class="m-0 text-texte">Le suivi de l'acné n'a pas pu être chargé.</p>
      <button type="button" class="min-h-11 rounded-controle border-[1.5px] border-contour px-4 text-sm font-medium text-texte"
              @click="acne.charger">Réessayer</button>
    </section>
    <template v-else-if="acne.donnees.value">
      <CarteEpisodeAcne :en-cours="acne.enCours.value" :dernier="acne.donnees.value.episodes[0] ?? null"
                        @commencer="ouvrirEpisode('commencer', null)" @terminer="ouvrirEpisode('terminer', acne.enCours.value)"/>
      <SuiviPhotosAcne v-model:ecart="acne.ecart.value" :suivis="acne.donnees.value.suivis" :plus-anciennes="acne.donnees.value.suivisPlusAnciens"
                       :comparaison="acne.comparaison.value"
                       :maintenant="new Date()"
                       @ajouter="ouvrirSaisie(null)" @modifier="(suivi) => ouvrirSaisie(enSymptome(suivi))" @voir-plus="acne.voirPlusDePhotos"
                       @agrandir="(url) => (photoAgrandie = url)"/>
      <ListeEpisodesAcne :episodes="acne.donnees.value.episodes" @modifier="(episode) => ouvrirEpisode('modifier', episode)"/>
    </template>

    <SaisieSymptome v-model:open="saisieOuverte" :entree="suiviModifie" :acne="true" :actions="actions"/>
    <SaisieEpisodeAcne v-model:open="episodeOuvert" :mode="modeEpisode" :episode="episodeModifie" :actions="actionsEpisode"/>

    <PanneauBas :open="photoAgrandie !== null" titre="Photo" @update:open="(o) => { if (!o) photoAgrandie = null; }">
      <img v-if="photoAgrandie" :src="photoAgrandie" alt="Photo de suivi" class="max-h-[70dvh] w-full rounded-carte object-contain">
    </PanneauBas>
  </main>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { useToast } from '@/shared/components/ui/toast';
import PanneauBas from '@/shared/components/PanneauBas.vue';
import { useAuthStore } from '@/features/auth/store/auth';
import CarteEpisodeAcne from '../components/CarteEpisodeAcne.vue';
import ListeEpisodesAcne from '../components/ListeEpisodesAcne.vue';
import SaisieEpisodeAcne, { type ActionsEpisodeAcne, type ModeEpisode } from '../components/SaisieEpisodeAcne.vue';
import SaisieSymptome, { type ActionsSaisieSymptome } from '../components/SaisieSymptome.vue';
import SuiviPhotosAcne from '../components/SuiviPhotosAcne.vue';
import { useAcne } from '../composables/useAcne';
import { useSymptomes } from '../composables/useSymptomes';
import type { EpisodeAcne } from '../types/acne';
import type { SymptomeCycle } from '../types/symptome-cycle';
import { enSymptome } from '../utils/acne';

const auth = useAuthStore();
const route = useRoute();
const router = useRouter();
const { toast } = useToast();
const acne = useAcne();
// Le point de suivi (photo, intensité) reste un symptôme « Acné » : seules ses écritures servent ici.
const symptomes = useSymptomes({ carnetSanteId: () => auth.user?.carnetSanteId });

const saisieOuverte = ref(false);
const suiviModifie = ref<SymptomeCycle | null>(null);
const episodeOuvert = ref(false);
const modeEpisode = ref<ModeEpisode>('commencer');
const episodeModifie = ref<EpisodeAcne | null>(null);
const photoAgrandie = ref<string | null>(null);

const actions: ActionsSaisieSymptome = {
  enregistrer: async (saisie, id) => {
    await symptomes.enregistrer(saisie, id);
    await acne.charger();
    toast({ title: id === null ? 'Photo de suivi ajoutée' : 'Suivi modifié', variant: 'custom' });
  },
  supprimer: async (id) => {
    await symptomes.supprimer(id);
    await acne.charger();
    toast({ title: 'Suivi supprimé', variant: 'custom' });
  },
};

const actionsEpisode: ActionsEpisodeAcne = {
  commencer: async (debut) => {
    await acne.commencer(debut);
    toast({ title: 'Épisode noté', variant: 'custom' });
  },
  terminer: async (id, fin) => {
    await acne.terminer(id, fin);
    toast({ title: 'Épisode terminé', variant: 'custom' });
  },
  modifier: async (id, saisie) => {
    await acne.modifier(id, saisie);
    toast({ title: 'Épisode modifié', variant: 'custom' });
  },
  supprimer: async (id) => {
    await acne.supprimer(id);
    toast({ title: 'Épisode supprimé', variant: 'custom' });
  },
};

function ouvrirEpisode(mode: ModeEpisode, episode: EpisodeAcne | null) {
  modeEpisode.value = mode;
  episodeModifie.value = episode;
  episodeOuvert.value = true;
}

function ouvrirSaisie(suivi: SymptomeCycle | null) {
  suiviModifie.value = suivi;
  saisieOuverte.value = true;
}

onMounted(async () => {
  // Lien profond depuis l'accueil (« Ajouter une photo ») : ouvre directement la saisie.
  if (route.query.ajouter !== undefined) {
    ouvrirSaisie(null);
    await router.replace({ query: {} });
  }
  await acne.charger();
});
</script>
