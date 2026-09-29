<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { format, isAfter, isToday, startOfDay, subDays } from 'date-fns';
import { fr } from 'date-fns/locale';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { TabsContent, TabsList, TabsRoot, TabsTrigger } from 'radix-vue';
import { useToast } from '@/shared/components/ui/toast';
import { useAuthStore } from '@/features/auth/store/auth';
import SaisieBilan from '@/features/bilan-quotidien/components/saisie/SaisieBilan.vue';
import BilanDuJourCard from '@/features/bilan-quotidien/components/BilanDuJourCard.vue';
import AnalyseTendances from '@/features/bilan-quotidien/components/AnalyseTendances.vue';
import EmotionSemaineCard from '@/features/bilan-quotidien/components/EmotionSemaineCard.vue';
import CalendrierBilans from '@/features/bilan-quotidien/components/historique/CalendrierBilans.vue';
import CourbesBilans from '@/features/bilan-quotidien/components/historique/CourbesBilans.vue';
import SelecteurPeriode from '@/features/bilan-quotidien/components/historique/SelecteurPeriode.vue';
import { useHistoriqueBilans } from '@/features/bilan-quotidien/composables/useHistoriqueBilans';
import { calculerTendances } from '@/features/bilan-quotidien/utils/tendances';
import { getWellbeingGoals } from '@/shared/services/wellbeingGoalsStorage';

/** Lien profond vers un onglet (`/bilan-quotidien?onglet=analyse`, depuis l'accueil). */
const ongletInitial = useRoute().query.onglet === 'analyse' ? 'analyse' : 'historique';
import type { BilanQuotidien } from '@/features/bilan-quotidien/types/bilan-quotidien';

const carnetSanteId = useAuthStore().user!.carnetSanteId;
const { toast } = useToast();
const { model, actions, bilanDu, chargerJours, enregistrerLocalement } = useHistoriqueBilans();

const isLoading = ref(true);

/** Saisie ouverte : création pour un jour, ou modification d'un bilan existant. */
interface SaisieOuverte {
  date: Date;
  bilan?: BilanQuotidien;
}
const saisie = ref<SaisieOuverte | null>(null);
// Chaque ouverture recrée le formulaire (clé) pour repartir de l'état du bilan choisi.
const cleSaisie = ref(0);

const ouvrirSaisie = async (date: Date) => {
  const jour = startOfDay(date);
  try {
    // La veille peut tomber hors de la période affichée : la charger pour « Comme hier ».
    await chargerJours(subDays(jour, 1), jour);
  } catch {
    toast({ title: 'Erreur', description: 'Impossible de charger ce bilan. Réessaie dans un instant.', variant: 'destructive' });
    return;
  }
  saisie.value = { date: jour, bilan: bilanDu(jour) };
  cleSaisie.value++;
  window.scrollTo({ top: 0 });
};

const bilanVeille = computed(() => (saisie.value ? bilanDu(subDays(saisie.value.date, 1)) : undefined));

const apresEnregistrement = (bilan: BilanQuotidien) => {
  enregistrerLocalement(bilan);
  if (saisie.value) actions.selectionnerJour(saisie.value.date);
  saisie.value = null;
};

onMounted(async () => {
  const aujourdhui = startOfDay(new Date());
  await actions.recharger();
  // Pas encore de bilan aujourd'hui : on ouvre directement la saisie (rappel du soir).
  if (!model.value.erreur && !bilanDu(aujourdhui)) await ouvrirSaisie(aujourdhui);
  isLoading.value = false;
});

// --- Consultation ---
const jourSelectionne = computed(() => model.value.jourSelectionne);
const bilanSelectionne = computed(() => bilanDu(jourSelectionne.value) ?? null);

const titreJour = computed(() =>
  isToday(jourSelectionne.value) ? "Aujourd'hui" : format(jourSelectionne.value, 'EEEE d MMMM', { locale: fr }));

const jourAVenir = computed(() => isAfter(jourSelectionne.value, startOfDay(new Date())));

const ONGLETS = [
  { valeur: 'historique', libelle: 'Historique' },
  { valeur: 'analyse', libelle: 'Tendances' },
];

// --- Analyse ---
const reperes = getWellbeingGoals();
const tendances = computed(() => calculerTendances(model.value.jours, model.value.bilansPrecedents, reperes));
</script>

<template>
  <main class="mx-auto flex w-full max-w-xl flex-col gap-3.5 px-5 pb-40 pt-24 lg:pb-12 lg:pt-10">
    <header v-if="!saisie" class="flex items-center gap-3">
      <i class="material-symbols-outlined rounded-controle bg-teinte-bilan-fond p-2 text-[26px] text-teinte-bilan" aria-hidden="true">event_note</i>
      <h1 class="m-0 grow text-[26px] font-semibold tracking-normal text-texte">Bilan</h1>
    </header>

    <div v-if="isLoading" class="flex flex-col gap-3.5" aria-busy="true" aria-label="Chargement des bilans">
      <Skeleton class="h-24 rounded-carte"/>
      <Skeleton class="h-72 rounded-carte"/>
    </div>

    <SaisieBilan
        v-else-if="saisie"
        :key="cleSaisie"
        :carnet-sante-id="carnetSanteId"
        :date="saisie.date"
        :bilan="saisie.bilan"
        :bilan-veille="bilanVeille"
        annulable
        @enregistre="apresEnregistrement"
        @annule="saisie = null"
    />

    <template v-else>
      <SelecteurPeriode
          :periode="model.periode"
          @changer-mode="actions.changerMode"
          @precedente="actions.precedente"
          @suivante="actions.suivante"
          @aujourdhui="actions.revenirAujourdhui"
      />

      <section v-if="model.erreur" role="alert" class="flex flex-col items-start gap-3 rounded-carte bg-surface p-5 shadow-elevation">
        <p class="m-0 text-texte">Les bilans de cette période n'ont pas pu être chargés.</p>
        <button type="button" class="min-h-11 rounded-controle border-[1.5px] border-contour px-4 text-sm font-medium text-texte"
                @click="actions.recharger">Réessayer</button>
      </section>

      <TabsRoot v-else :default-value="ongletInitial" class="flex flex-col gap-3.5" :class="{ 'opacity-60': model.chargement }"
                :aria-busy="model.chargement">
        <TabsList aria-label="Vues du bilan" class="grid grid-cols-2 gap-1 rounded-controle bg-surface-2 p-1">
          <TabsTrigger v-for="o in ONGLETS" :key="o.valeur" :value="o.valeur"
                       class="min-h-10 rounded-controle px-2 text-sm leading-tight text-texte-2 data-[state=active]:bg-surface data-[state=active]:font-semibold data-[state=active]:text-texte data-[state=active]:shadow-elevation">
            {{ o.libelle }}
          </TabsTrigger>
        </TabsList>

        <TabsContent value="historique" class="flex flex-col gap-3.5 focus:outline-none">
          <section aria-label="Calendrier des bilans" class="rounded-carte bg-surface p-3 shadow-elevation md:p-5">
            <CalendrierBilans
                :jours="model.jours"
                :mode="model.periode.mode"
                :jour-selectionne="jourSelectionne"
                @selectionner="actions.selectionnerJour"
            />
          </section>

          <BilanDuJourCard
              :titre="titreJour"
              :bilan="bilanSelectionne"
              :a-venir="jourAVenir"
              @modifier="ouvrirSaisie(jourSelectionne)"
              @remplir="ouvrirSaisie(jourSelectionne)"
          />

          <EmotionSemaineCard :bilans="model.bilans" :mode="model.periode.mode"/>

          <CourbesBilans
              :jours="model.jours"
              :mode="model.periode.mode"
              :jour-selectionne="jourSelectionne"
              @selectionner="actions.selectionnerJour"
          />
        </TabsContent>

        <TabsContent value="analyse" class="focus:outline-none">
          <AnalyseTendances :tendances="tendances" :mode="model.periode.mode"/>
        </TabsContent>
      </TabsRoot>
    </template>
  </main>
</template>
