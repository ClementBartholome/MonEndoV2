<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { format, isToday, startOfDay, startOfWeek, subDays } from 'date-fns';
import { fr } from 'date-fns/locale';
import BackButton from '@/shared/components/BackButton.vue';
import SelectWeek from '@/shared/components/SelectWeek.vue';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { LineChart } from '@/shared/components/ui/chart-line';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';
import { useAuthStore } from '@/features/auth/store/auth';
import SaisieBilan from '@/features/bilan-quotidien/components/saisie/SaisieBilan.vue';
import BilanDuJourCard from '@/features/bilan-quotidien/components/BilanDuJourCard.vue';
import BilanWeekSelector from '@/features/bilan-quotidien/components/BilanWeekSelector.vue';
import DashboardBilanQuotidien from '@/features/bilan-quotidien/components/DashboardBilanQuotidien.vue';
import EmotionSemaineCard from '@/features/bilan-quotidien/components/EmotionSemaineCard.vue';
import { useBilansQuotidiens } from '@/features/bilan-quotidien/composables/useBilansQuotidiens';
import { scoreHumeur } from '@/features/bilan-quotidien/utils/humeur';
import { stressDuBilan } from '@/features/bilan-quotidien/utils/mesures';
import type { BilanQuotidien } from '@/features/bilan-quotidien/types/bilan-quotidien';

const carnetSanteId = useAuthStore().user!.carnetSanteId;
const { bilans, chargerSemaine, chargerSemaineDe, bilanDu, enregistrerLocalement } = useBilansQuotidiens(carnetSanteId);

const isLoading = ref(true);
const selectedDate = ref(startOfDay(new Date()));

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
  // La veille peut tomber la semaine précédente : la charger pour « Comme hier ».
  await Promise.all([chargerSemaineDe(jour), chargerSemaineDe(subDays(jour, 1))]);
  saisie.value = { date: jour, bilan: bilanDu(jour) };
  cleSaisie.value++;
  window.scrollTo({ top: 0 });
};

const bilanVeille = computed(() => (saisie.value ? bilanDu(subDays(saisie.value.date, 1)) : undefined));

const apresEnregistrement = (bilan: BilanQuotidien) => {
  enregistrerLocalement(bilan);
  selectedDate.value = saisie.value?.date ?? selectedDate.value;
  saisie.value = null;
};

onMounted(async () => {
  const aujourdhui = startOfDay(new Date());
  await Promise.all([chargerSemaineDe(aujourdhui), chargerSemaineDe(subDays(aujourdhui, 1))]);
  // Pas encore de bilan aujourd'hui : on ouvre directement la saisie (rappel du soir).
  if (!bilanDu(aujourdhui)) await ouvrirSaisie(aujourdhui);
  isLoading.value = false;
});

// --- Consultation ---
const selectedBilan = computed(() => bilanDu(selectedDate.value) ?? null);

const selectedDateTitle = computed(() =>
  isToday(selectedDate.value) ? "Aujourd'hui" : format(selectedDate.value, 'EEEE d MMMM', { locale: fr }));

const selectionnerJour = (date: Date) => {
  selectedDate.value = startOfDay(date);
  void chargerSemaineDe(date);
};

const modifierBilanSelectionne = () => ouvrirSaisie(selectedDate.value);

// --- Évolution hebdomadaire (graphique) ---
const selectedWeekYear = ref(format(new Date(), "RRRR-'W'II"));
const startYear = ref(new Date().getFullYear());
const endYear = ref(new Date().getFullYear());

const handleUpdateYears = ({ startYear: start, endYear: end }: { startYear: number; endYear: number }) => {
  startYear.value = start;
  endYear.value = end;
};

watch(selectedWeekYear, (valeur) => {
  const [, semaine] = valeur.split('-W');
  void chargerSemaine(semaine, endYear.value.toString());
});

const filteredBilans = computed<BilanQuotidien[]>(() => {
  if (!selectedWeekYear.value) return [];
  const [, week] = selectedWeekYear.value.split('-W');
  const startDate = startOfWeek(new Date(Number(endYear.value), 0, 1), { weekStartsOn: 1 });
  const adjustedStartDate = new Date(startDate.setDate(startDate.getDate() + (Number(week) - 1) * 7));
  const endDate = new Date(adjustedStartDate);
  endDate.setDate(adjustedStartDate.getDate() + 6);
  endDate.setHours(23, 59, 59, 999);

  return bilans.value.filter((bilan) => {
    const bilanDate = new Date(bilan.date);
    return bilanDate >= adjustedStartDate && bilanDate <= endDate &&
      bilanDate.getFullYear() >= startYear.value && bilanDate.getFullYear() <= endYear.value;
  });
});

// Mesures non renseignées laissées vides (undefined) : jamais tracées à 0.
const chartData = computed(() => filteredBilans.value.map((bilan) => {
  const humeur = scoreHumeur(bilan);
  return {
    date: format(new Date(bilan.date), 'dd/MM/yyyy'),
    stress: stressDuBilan(bilan) ?? undefined,
    fatigue: bilan.fatigue ?? undefined,
    // Même échelle que le stress et la fatigue (0 à 5) ; les anciens bilans comptent par leur humeur.
    humeur: humeur === null ? undefined : Math.round(humeur * 50) / 10,
    douleur: bilan.douleurMoyenne,
  };
}));
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
      <Tabs default-value="recap" class="w-full">
        <TabsList class="bilan-tabs-list">
          <TabsTrigger value="recap" class="bilan-tab-trigger">Récapitulatif & Historique</TabsTrigger>
          <TabsTrigger value="dashboard" class="bilan-tab-trigger">Analyse & Tendances</TabsTrigger>
        </TabsList>

        <TabsContent value="recap">
          <div class="w-full">
            <Card class="container mt-4 mx-auto w-full bg-clearer rounded-3xl shadow-xl flex flex-col">
              <CardContent class="p-4 md:p-6">
                <BilanWeekSelector
                    :bilans="bilans"
                    :selected-date="selectedDate"
                    @update:selected-date="selectionnerJour"
                    @semaine-affichee="chargerSemaineDe"
                />
              </CardContent>
            </Card>

            <BilanDuJourCard
                :titre="selectedDateTitle"
                :bilan="selectedBilan"
                :a-venir="selectedDate > startOfDay(new Date())"
                @modifier="modifierBilanSelectionne"
                @remplir="ouvrirSaisie(selectedDate)"
            />

            <EmotionSemaineCard :bilans="filteredBilans"/>

            <Card class="container !mx-0 mt-4 w-full bg-clearer rounded-3xl shadow-xl flex flex-col">
              <CardHeader class="p-4 md:p-6">
                <CardTitle class="m-0 text-lg leading-tight flex items-center gap-2">
                  <i class="material-symbols-outlined" aria-hidden="true">show_chart</i>Évolution de la semaine
                </CardTitle>
              </CardHeader>
              <CardContent class="px-4 pb-4 md:px-6 md:pb-6">
                <SelectWeek v-model="selectedWeekYear" class="text-left" @update:years="handleUpdateYears"/>
                <LineChart
                    :data="chartData"
                    :categories="['stress', 'fatigue', 'humeur', 'douleur']"
                    index="date"
                    :colors="['#ff6b6b', '#4ecdc4', '#ffa726', '#8e44ad']"
                    :y-formatter="(value) => `${value}`"
                    :y-domain="[0, 10]"
                />
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="dashboard">
          <DashboardBilanQuotidien :bilans="bilans"/>
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
