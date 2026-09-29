<template>
  <main class="mx-auto flex w-full max-w-xl flex-col gap-3.5 px-5 pb-40 pt-24 lg:pb-12 lg:pt-10">
    <header class="flex items-center gap-3">
      <i class="material-symbols-outlined rounded-controle bg-teinte-regles-fond p-2 text-[26px] text-teinte-regles" aria-hidden="true">menstrual_health</i>
      <h1 class="m-0 grow text-[26px] font-semibold tracking-normal text-texte">Cycle</h1>
      <button v-if="onglet === 'symptomes'" type="button"
              class="hidden min-h-11 items-center gap-2 rounded-controle bg-button px-4 font-semibold text-texte lg:inline-flex"
              @click="ouvrirAjout(false)">
        <i class="material-symbols-outlined" aria-hidden="true">add</i>Noter un symptôme
      </button>
    </header>

    <TabsRoot v-model="onglet" class="flex flex-col gap-3.5" @update:model-value="changerOnglet">
      <TabsList aria-label="Rubriques du cycle" class="grid grid-cols-3 gap-1 rounded-controle bg-surface-2 p-1">
        <TabsTrigger v-for="o in ONGLETS" :key="o.valeur" :value="o.valeur"
                     class="min-h-10 rounded-controle text-sm text-texte-2 data-[state=active]:bg-surface data-[state=active]:font-semibold data-[state=active]:text-texte data-[state=active]:shadow-elevation">
          {{ o.libelle }}
        </TabsTrigger>
      </TabsList>

      <TabsContent value="cycles" class="flex flex-col gap-3.5 focus:outline-none">
        <div v-if="regles.chargement.value" class="flex flex-col gap-3.5" aria-busy="true" aria-label="Chargement des règles">
          <Skeleton class="h-24 rounded-carte"/>
          <Skeleton class="h-80 rounded-carte"/>
        </div>
        <section v-else-if="regles.erreur.value" role="alert" class="flex flex-col items-start gap-3 rounded-carte bg-surface p-5 shadow-elevation">
          <p class="m-0 text-texte">Les règles n'ont pas pu être chargées.</p>
          <button type="button" class="min-h-11 rounded-controle border-[1.5px] border-contour px-4 text-sm font-medium text-texte"
                  @click="regles.charger">Réessayer</button>
        </section>
        <template v-else-if="regles.donnees.value">
          <CarteCycleEnCours :en-cours="regles.donnees.value.enCours" :envoi="regles.enCoursDEnvoi.value.size > 0"
                             @noter-aujourdhui="noter(regles.noterAujourdhui)"/>
          <CalendrierRegles :mois="regles.mois.value" :calendrier="regles.calendrier.value" :en-cours-d-envoi="regles.enCoursDEnvoi.value"
                            @basculer="(jour) => noter(() => regles.basculer(jour))" @changer="regles.allerAuMois"/>
          <ListeCycles :cycles="regles.donnees.value.cycles" :resume="regles.donnees.value"
                       :plus-anciens="regles.donnees.value.cyclesPlusAnciens" @voir-plus="regles.voirPlusDeCycles"/>
        </template>
      </TabsContent>

      <TabsContent value="symptomes" class="flex flex-col gap-3.5 focus:outline-none">
        <nav aria-label="Mois affiché" class="-mx-3">
          <SelecteurMois :mois="symptomes.mois.value" @changer="symptomes.allerAuMois"/>
        </nav>
        <div v-if="symptomes.chargement.value" class="flex flex-col gap-3.5" aria-busy="true" aria-label="Chargement des symptômes">
          <Skeleton class="h-20 rounded-carte"/>
          <Skeleton class="h-32 rounded-carte"/>
        </div>
        <section v-else-if="symptomes.erreur.value" role="alert" class="flex flex-col items-start gap-3 rounded-carte bg-surface p-5 shadow-elevation">
          <p class="m-0 text-texte">Les symptômes n'ont pas pu être chargés.</p>
          <button type="button" class="min-h-11 rounded-controle border-[1.5px] border-contour px-4 text-sm font-medium text-texte"
                  @click="symptomes.charger">Réessayer</button>
        </section>
        <ListeSymptomes v-else-if="symptomes.entrees.value.length" :chiffres="symptomes.chiffres.value" :groupes="symptomes.groupes.value"
                        @modifier="(entree) => ouvrirModification(entree, false)"/>
        <EmptyStateAction v-else title="Aucun symptôme noté ce mois-ci"
                          description="Spotting, nausée, fatigue… note-les quand ils arrivent, pour en parler à ton médecin."
                          action-label="Noter un symptôme" @action="ouvrirAjout(false)"/>
      </TabsContent>

      <TabsContent value="acne" class="flex flex-col gap-3.5 focus:outline-none">
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
                           @ajouter="ouvrirAjout(true)" @modifier="(suivi) => ouvrirModification(enSymptome(suivi), true)" @voir-plus="acne.voirPlusDePhotos"
                           @agrandir="(url) => (photoAgrandie = url)"/>
          <ListeEpisodesAcne :episodes="acne.donnees.value.episodes" @modifier="(episode) => ouvrirEpisode('modifier', episode)"/>
        </template>
      </TabsContent>
    </TabsRoot>

    <button v-if="onglet === 'symptomes'" type="button"
            class="fixed bottom-[calc(4.25rem+16px)] right-5 z-30 inline-flex min-h-14 items-center gap-2 rounded-controle bg-button pl-4 pr-5 text-[15px] font-semibold text-texte shadow-elevation lg:hidden"
            @click="ouvrirAjout(false)">
      <i class="material-symbols-outlined" aria-hidden="true">add</i>Noter un symptôme
    </button>

    <SaisieSymptome v-model:open="saisieOuverte" :entree="entreeModifiee" :acne="saisieAcne" :actions="actions"/>
    <SaisieEpisodeAcne v-model:open="episodeOuvert" :mode="modeEpisode" :episode="episodeModifie" :actions="actionsEpisode"/>

    <PanneauBas :open="photoAgrandie !== null" titre="Photo" @update:open="(o) => { if (!o) photoAgrandie = null; }">
      <img v-if="photoAgrandie" :src="photoAgrandie" alt="Photo de suivi" class="max-h-[70dvh] w-full rounded-carte object-contain">
    </PanneauBas>
  </main>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { TabsContent, TabsList, TabsRoot, TabsTrigger } from 'radix-vue';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { useToast } from '@/shared/components/ui/toast';
import EmptyStateAction from '@/shared/components/EmptyStateAction.vue';
import PanneauBas from '@/shared/components/PanneauBas.vue';
import SelecteurMois from '@/shared/components/SelecteurMois.vue';
import { useAuthStore } from '@/features/auth/store/auth';
import CarteEpisodeAcne from '../components/CarteEpisodeAcne.vue';
import ListeEpisodesAcne from '../components/ListeEpisodesAcne.vue';
import SaisieEpisodeAcne, { type ActionsEpisodeAcne, type ModeEpisode } from '../components/SaisieEpisodeAcne.vue';
import SuiviPhotosAcne from '../components/SuiviPhotosAcne.vue';
import CalendrierRegles from '../components/CalendrierRegles.vue';
import CarteCycleEnCours from '../components/CarteCycleEnCours.vue';
import ListeCycles from '../components/ListeCycles.vue';
import ListeSymptomes from '../components/ListeSymptomes.vue';
import SaisieSymptome, { type ActionsSaisieSymptome } from '../components/SaisieSymptome.vue';
import { useRegles } from '../composables/useRegles';
import { useSymptomes } from '../composables/useSymptomes';
import type { SymptomeCycle } from '../types/symptome-cycle';
import { useAcne } from '../composables/useAcne';
import type { EpisodeAcne } from '../types/acne';
import { enSymptome } from '../utils/acne';
import { ACNE } from '../utils/symptomes';

type Onglet = 'cycles' | 'symptomes' | 'acne';
/** Les valeurs restent celles des liens existants (accueil, bilan, rappel de photo d'acné : `/cycle?onglet=acne`). */
const ONGLETS: { valeur: Onglet; libelle: string }[] = [
  { valeur: 'cycles', libelle: 'Règles' },
  { valeur: 'symptomes', libelle: 'Symptômes' },
  { valeur: 'acne', libelle: 'Acné' },
];

const auth = useAuthStore();
const route = useRoute();
const router = useRouter();
const { toast } = useToast();
const regles = useRegles();
const symptomes = useSymptomes({ carnetSanteId: () => auth.user?.carnetSanteId });
const acne = useAcne();

const demande = route.query.onglet as Onglet | undefined;
const onglet = ref<Onglet>(ONGLETS.some((o) => o.valeur === demande) ? demande! : 'cycles');
const saisieOuverte = ref(false);
const saisieAcne = ref(false);
const entreeModifiee = ref<SymptomeCycle | null>(null);
const episodeOuvert = ref(false);
const modeEpisode = ref<ModeEpisode>('commencer');
const episodeModifie = ref<EpisodeAcne | null>(null);
const photoAgrandie = ref<string | null>(null);
const charges = new Set<Onglet>();

const actions: ActionsSaisieSymptome = {
  enregistrer: async (saisie, id) => {
    await symptomes.enregistrer(saisie, id);
    if (saisie.typeSymptome === ACNE) await acne.charger();
    toast({ title: id === null ? 'Symptôme noté' : 'Symptôme modifié', variant: 'custom' });
  },
  supprimer: async (id) => {
    await symptomes.supprimer(id);
    if (onglet.value === 'acne') await acne.charger();
    toast({ title: 'Symptôme supprimé', variant: 'custom' });
  },
};

/** Jour de règles ajouté ou retiré : un échec se signale sans quitter la page (le calendrier est déjà revenu). */
async function noter(action: () => Promise<void>) {
  try {
    await action();
  } catch {
    toast({ title: 'Le jour n\'a pas pu être enregistré. Réessaie dans un instant.', variant: 'destructive' });
  }
}

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

function ouvrirAjout(pourAcne: boolean) {
  entreeModifiee.value = null;
  saisieAcne.value = pourAcne;
  saisieOuverte.value = true;
}

function ouvrirModification(entree: SymptomeCycle, pourAcne: boolean) {
  entreeModifiee.value = entree;
  saisieAcne.value = pourAcne;
  saisieOuverte.value = true;
}

/** Chaque onglet charge ses données à sa première ouverture. */
async function changerOnglet(valeur: string | number) {
  const choisi = valeur as Onglet;
  await router.replace({ query: { ...route.query, onglet: choisi } });
  if (charges.has(choisi)) return;
  charges.add(choisi);
  if (choisi === 'cycles') await regles.charger();
  if (choisi === 'symptomes') await symptomes.charger();
  if (choisi === 'acne') await acne.charger();
}

onMounted(async () => {
  // Lien profond depuis l'accueil (tuile « Symptôme ») : ouvre directement la saisie.
  if (route.query.ajouter !== undefined) {
    ouvrirAjout(onglet.value === 'acne');
    await router.replace({ query: { onglet: onglet.value } });
  }
  await changerOnglet(onglet.value);
});
</script>
