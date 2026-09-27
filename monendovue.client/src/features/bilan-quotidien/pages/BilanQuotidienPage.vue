<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { format, isAfter, isToday, startOfDay, subDays } from 'date-fns';
import { fr } from 'date-fns/locale';
import BackButton from '@/shared/components/BackButton.vue';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent } from '@/shared/components/ui/card';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';
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

// --- Analyse ---
const reperes = getWellbeingGoals();
const tendances = computed(() => calculerTendances(model.value.jours, model.value.bilansPrecedents, reperes));
</script>

<template>
  <div class="flex-column-container !gap-1">
    <div class="flex items-center justify-between w-full">
      <BackButton class="!w-1/4"/>
    </div>

    <div v-if="isLoading" class="flex flex-col space-y-3 p-6 pt-0">
      <Skeleton class="h-[300px] w-full mt-4 rounded-xl"/>
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

    <div v-else class="w-full">
      <SelecteurPeriode
          class="mt-2"
          :periode="model.periode"
          @changer-mode="actions.changerMode"
          @precedente="actions.precedente"
          @suivante="actions.suivante"
          @aujourdhui="actions.revenirAujourdhui"
      />

      <div v-if="model.erreur" class="flex flex-col items-center gap-3 text-center py-6" role="alert">
        <p class="text-paragraph">Les bilans de cette période n'ont pas pu être chargés.</p>
        <Button type="button" variant="outline" class="h-11" @click="actions.recharger">Réessayer</Button>
      </div>

      <Tabs v-else default-value="historique" class="w-full mt-4" :class="{ 'opacity-60': model.chargement }"
            :aria-busy="model.chargement">
        <TabsList class="bilan-tabs-list">
          <TabsTrigger value="historique" class="bilan-tab-trigger">Historique</TabsTrigger>
          <TabsTrigger value="analyse" class="bilan-tab-trigger">Analyse & Tendances</TabsTrigger>
        </TabsList>

        <TabsContent value="historique">
          <Card class="container mt-4 mx-auto w-full bg-clearer rounded-3xl shadow-xl flex flex-col">
            <CardContent class="p-3 md:p-6">
              <CalendrierBilans
                  :jours="model.jours"
                  :mode="model.periode.mode"
                  :jour-selectionne="jourSelectionne"
                  @selectionner="actions.selectionnerJour"
              />
            </CardContent>
          </Card>

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

        <TabsContent value="analyse">
          <AnalyseTendances :tendances="tendances" :mode="model.periode.mode"/>
        </TabsContent>
      </Tabs>
    </div>
  </div>
</template>

<style scoped>
.bilan-tabs-list {
  width: 100%;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.5rem;
}

.bilan-tab-trigger {
  white-space: normal;
  text-align: center;
  line-height: 1.2;
}

@media (max-width: 425px) {
  .bilan-tab-trigger {
    font-size: 0.8rem;
    padding-left: 0.5rem;
    padding-right: 0.5rem;
  }
}
</style>
