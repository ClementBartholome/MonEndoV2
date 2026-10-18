<template>
  <main class="mx-auto flex w-full max-w-xl flex-col gap-3.5 px-5 pb-40 pt-24 lg:pb-12 lg:pt-10">
    <header class="flex items-center gap-3">
      <h1 class="m-0 grow text-titre-page font-medium tracking-normal text-texte">Cycle</h1>
      <button v-if="onglet === 'symptomes'" type="button"
              class="hidden min-h-11 items-center gap-2 rounded-controle bg-button px-4 font-semibold text-texte lg:inline-flex"
              @click="ouvrirAjout">
        <i class="material-symbols-outlined" aria-hidden="true">add</i>Noter un symptôme
      </button>
    </header>

    <TabsRoot v-model="onglet" class="flex flex-col gap-3.5" @update:model-value="changerOnglet">
      <TabsList aria-label="Rubriques du cycle" class="grid grid-cols-2 gap-1 rounded-controle bg-surface-2 p-1">
        <TabsTrigger v-for="o in ONGLETS" :key="o.valeur" :value="o.valeur"
                     class="min-h-11 rounded-controle text-sm text-texte-2 data-[state=active]:bg-surface data-[state=active]:font-semibold data-[state=active]:text-texte data-[state=active]:shadow-elevation">
          {{ o.libelle }}
        </TabsTrigger>
      </TabsList>

      <TabsContent value="cycles" class="flex flex-col gap-3.5">
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
          <section v-if="regles.frise.value.length" aria-labelledby="titre-frise" class="flex flex-col gap-2.5 border-y border-trait py-3.5">
            <div class="flex items-baseline justify-between gap-2">
              <h2 id="titre-frise" class="m-0 text-legende font-semibold uppercase tracking-wider text-texte-2">Les 30 derniers jours</h2>
              <span class="text-legende text-texte-2">Douleur · règles</span>
            </div>
            <Frise :jours="regles.frise.value"/>
          </section>
          <CalendrierRegles :mois="regles.mois.value" :calendrier="regles.calendrier.value" :en-cours-d-envoi="regles.enCoursDEnvoi.value"
                            @basculer="(jour) => noter(() => regles.basculer(jour))" @changer="regles.allerAuMois"/>
          <DetailJourRegles v-if="regles.jourDetail.value" :jours="regles.joursNotes.value" :selection="regles.jourDetail.value"
                            :detail="regles.detailDuJour.value" :envoi="regles.enCoursDeDetail.value"
                            @selectionner="regles.choisirJour" @changer="(changement) => noter(() => regles.preciser(changement))"/>
          <ListeCycles :cycles="regles.donnees.value.cycles" :resume="regles.donnees.value"
                       :plus-anciens="regles.donnees.value.cyclesPlusAnciens" @voir-plus="regles.voirPlusDeCycles"/>
        </template>
      </TabsContent>

      <TabsContent value="symptomes" class="flex flex-col gap-3.5">
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
                        @modifier="ouvrirModification"/>
        <EmptyStateAction v-else title="Aucun symptôme noté ce mois-ci"
                          description="Spotting, nausée, fatigue… note-les quand ils arrivent, pour en parler à ton médecin."
                          action-label="Noter un symptôme" @action="ouvrirAjout"/>
        <!-- L'acné avait un onglet ici : ce raccourci mène à sa page, là où on la cherchait. -->
        <router-link to="/acne" class="flex min-h-16 items-center gap-3 rounded-carte bg-surface px-4 !text-texte shadow-elevation">
          <i class="material-symbols-outlined rounded-controle bg-teinte-symptome-fond p-1.5 text-titre-2 text-teinte-symptome" aria-hidden="true">face</i>
          <span class="flex grow flex-col">
            <span class="text-corps font-medium">Suivi de l'acné</span>
            <span class="text-legende text-texte-3">Épisodes et photos de suivi</span>
          </span>
          <i class="material-symbols-outlined text-texte-3" aria-hidden="true">chevron_right</i>
        </router-link>
      </TabsContent>
    </TabsRoot>

    <button v-if="onglet === 'symptomes'" type="button"
            class="fixed bottom-[calc(4.25rem+16px)] right-5 z-30 inline-flex min-h-14 items-center gap-2 rounded-controle bg-button pl-4 pr-5 text-corps font-semibold text-texte shadow-elevation lg:hidden"
            @click="ouvrirAjout">
      <i class="material-symbols-outlined" aria-hidden="true">add</i>Noter un symptôme
    </button>

    <SaisieSymptome v-model:open="saisieOuverte" :entree="entreeModifiee" :acne="false" :actions="actions"/>
  </main>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { TabsContent, TabsList, TabsRoot, TabsTrigger } from 'radix-vue';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { useToast } from '@/shared/components/ui/toast';
import EmptyStateAction from '@/shared/components/EmptyStateAction.vue';
import SelecteurMois from '@/shared/components/SelecteurMois.vue';
import Frise from '@/shared/components/Frise.vue';
import { useAuthStore } from '@/features/auth/store/auth';
import CalendrierRegles from '../components/CalendrierRegles.vue';
import CarteCycleEnCours from '../components/CarteCycleEnCours.vue';
import DetailJourRegles from '../components/DetailJourRegles.vue';
import ListeCycles from '../components/ListeCycles.vue';
import ListeSymptomes from '../components/ListeSymptomes.vue';
import SaisieSymptome, { type ActionsSaisieSymptome } from '../components/SaisieSymptome.vue';
import { useRegles } from '../composables/useRegles';
import { useSymptomes } from '../composables/useSymptomes';
import type { SymptomeCycle } from '../types/symptome-cycle';

type Onglet = 'cycles' | 'symptomes';
/** Les valeurs restent celles des liens existants (accueil, bilan) ; l'ancien `?onglet=acne` est redirigé vers `/acne` par le routeur. */
const ONGLETS: { valeur: Onglet; libelle: string }[] = [
  { valeur: 'cycles', libelle: 'Règles' },
  { valeur: 'symptomes', libelle: 'Symptômes' },
];

const auth = useAuthStore();
const route = useRoute();
const router = useRouter();
const { toast } = useToast();
const regles = useRegles();
const symptomes = useSymptomes({ carnetSanteId: () => auth.user?.carnetSanteId });

const demande = route.query.onglet as Onglet | undefined;
const onglet = ref<Onglet>(ONGLETS.some((o) => o.valeur === demande) ? demande! : 'cycles');
const saisieOuverte = ref(false);
const entreeModifiee = ref<SymptomeCycle | null>(null);
const charges = new Set<Onglet>();

const actions: ActionsSaisieSymptome = {
  enregistrer: async (saisie, id) => {
    await symptomes.enregistrer(saisie, id);
    toast({ title: id === null ? 'Symptôme noté' : 'Symptôme modifié', variant: 'custom' });
  },
  supprimer: async (id) => {
    await symptomes.supprimer(id);
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

function ouvrirAjout() {
  entreeModifiee.value = null;
  saisieOuverte.value = true;
}

function ouvrirModification(entree: SymptomeCycle) {
  entreeModifiee.value = entree;
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
}

onMounted(async () => {
  // Lien profond depuis l'accueil (tuile « Symptôme ») : ouvre directement la saisie.
  if (route.query.ajouter !== undefined) {
    ouvrirAjout();
    await router.replace({ query: { onglet: onglet.value } });
  }
  await changerOnglet(onglet.value);
});
</script>
