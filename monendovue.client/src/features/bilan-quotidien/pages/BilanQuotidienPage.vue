<template>
  <div class="flex-column-container !gap-1">
    <div class="flex items-center justify-between w-full">
      <BackButton class="!w-1/4"/>
      <p v-if="justSubmitted" class="text-lg text-center flex items-center gap-2 justify-center ml-4 w-full">
        Bilan quotidien enregistré <i class="material-symbols-outlined check-icon mr-2">check_circle</i>
      </p>
    </div>

    <div v-if="isLoading" class="flex flex-col space-y-3 p-6 pt-0">
      <Skeleton class="h-[300px] w-full mt-4 rounded-xl"/>
    </div>

    <section v-else v-if="!isSubmitted"
             class="container !mt-0 mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto flex flex-col h-auto">

      <div class="mb-6">
        <div class="flex justify-between mb-2">
          <span class="text-sm font-medium">Étape {{ currentStep }}/7</span>
          <span class="text-sm text-gray-500">{{ Math.round((currentStep / 7) * 100) }}%</span>
        </div>
        <Progress :model-value="(currentStep / 7) * 100" class="h-3 mb-4"/>

        <div class="flex justify-between gap-2 bilan-stepper">
          <button
              v-for="step in 7"
              :key="step"
              @click="goToStep(step)"
              :disabled="!canAccessStep(step)"
              :class="[
                'w-10 h-10 rounded-full flex items-center justify-center text-sm font-medium transition-all duration-200',
                step < currentStep ? 'bg-green-500 text-white hover:bg-green-600 cursor-pointer' : '',
                step === currentStep ? 'bg-button text-white ring-button/30 scale-110' : '',
                step > currentStep ? 'bg-gray-200 text-gray-400 cursor-not-allowed' : '',
                canAccessStep(step) && step !== currentStep ? 'hover:scale-105' : ''
              ]"
              class="bilan-step-dot"
              :title="getStepTitle(step)"
          >
            <i v-if="step < currentStep" class="material-symbols-outlined text-base">check</i>
            <span v-else>{{ step }}</span>
          </button>
        </div>
      </div>

      <!-- Étape 1: Humeur -->
      <div v-if="currentStep === 1">
        <h2 class="text-2xl font-bold mb-14 flex items-center justify-center">
          <i class="material-symbols-outlined mr-2">mood</i>Humeur du jour
        </h2>
        <div class="grid grid-cols-3 gap-4 mb-4">
          <Button
              v-for="mood in moods"
              :key="mood.value"
              @click="selectMood(mood.value)"
              :variant="formData.mood === mood.value ? 'selected' : 'outline'"
              :class="[
                'h-32 flex flex-col items-center justify-center transition-all duration-300',
                formData.mood === mood.value 
                  ? 'scale-110 shadow-2xl ring-4 ring-button/50' 
                  : 'hover:scale-105 hover:shadow-lg'
              ]"
          >
            <span
                class="material-symbols-outlined text-6xl mb-2 transition-transform"
                :class="{ 'animate-bounce': formData.mood === mood.value }"
            >
              {{ mood.icon }}
            </span>
            <span class="font-semibold">{{ mood.label }}</span>
          </Button>
        </div>
      </div>

      <!-- Étape 2: Stress -->
      <div v-if="currentStep === 2">
        <h2 class="text-2xl font-bold mb-14 flex items-center justify-center">
          <i class="material-symbols-outlined mr-2">psychology</i>Niveau de stress
        </h2>
        <div class="mb-10">
          <div class="flex justify-between items-center mb-2">
            <h3 class="text-xl font-semibold flex items-center">
              <i class="material-symbols-outlined mr-2">work</i> Vie pro
            </h3>
            <span class="text-2xl font-bold text-button">{{ formData.stressPro[0] }}/5</span>
          </div>
          <Slider v-model="formData.stressPro" :min="0" :max="5" :step="1"/>
          <div class="flex justify-between text-sm text-gray-500 mt-1">
            <span>Aucun stress</span>
            <span>Stress maximum</span>
          </div>
        </div>
        <div class="mb-10">
          <div class="flex justify-between items-center mb-2">
            <h3 class="text-xl font-semibold flex items-center">
              <i class="material-symbols-outlined mr-2">home</i> Vie perso
            </h3>
            <span class="text-2xl font-bold text-button">{{ formData.stressPerso[0] }}/5</span>
          </div>
          <Slider v-model="formData.stressPerso" :min="0" :max="5" :step="1"/>
          <div class="flex justify-between text-sm text-gray-500 mt-1">
            <span>Aucun stress</span>
            <span>Stress maximum</span>
          </div>
        </div>
      </div>

      <!-- Étape 3: Fatigue -->
      <div v-if="currentStep === 3">
        <h2 class="text-2xl font-bold mb-8 flex items-center justify-center">
          <i class="material-symbols-outlined mr-2">bedtime</i>Niveau de fatigue
        </h2>
        <div class="mb-10">
          <div class="flex justify-between items-center mb-2">
            <span class="text-lg font-medium">Niveau actuel</span>
            <span class="text-2xl font-bold text-button">{{ formData.fatigue[0] }}/5</span>
          </div>
          <Slider v-model="formData.fatigue" :min="0" :max="5" :step="1"/>
          <div class="flex justify-between text-sm text-gray-500 mt-1">
            <span>En pleine forme</span>
            <span>Épuisée</span>
          </div>
        </div>
      </div>

      <!-- Étape 4: Pas -->
      <div v-if="currentStep === 4" class="flex flex-col items-center">
        <h2 class="text-2xl font-bold mb-6 flex items-center">
          <i class="material-symbols-outlined mr-2">directions_walk</i>Nombre de pas
        </h2>
        <FormField name="pas">
          <round-slider
              v-model="formData.pas"
              start-angle="315"
              end-angle="+270"
              line-cap="round"
              radius="120"
              @input="value => formData.pas = value"
              :tooltip-format="value => `${value.value} pas`"
          />
        </FormField>
      </div>

      <!-- Étape 5: Hydratation -->
      <div v-if="currentStep === 5" class="flex flex-col items-center">
        <h2 class="text-2xl font-bold mb-6 flex items-center">
          <i class="material-symbols-outlined mr-2">local_drink</i>Hydratation
        </h2>
        <FormField name="hydratation">
          <round-slider
              v-model="formData.hydratation"
              start-angle="315"
              end-angle="+270"
              line-cap="round"
              radius="120"
              max="2.5"
              step="0.1"
              @input="value => formData.hydratation = value"
              :tooltip-format="value => `${value.value} L`"
          />
        </FormField>
      </div>

      <!-- Étape 6: Alimentation -->
      <div v-if="currentStep === 6" class="flex flex-col">
        <h2 class="text-2xl font-bold mb-8 flex items-center justify-center">
          <i class="material-symbols-outlined mr-2">restaurant</i>Alimentation
        </h2>

        <FormField name="alimentation">
          <FormItem class="mb-6">
            <FormLabel class="text-lg font-semibold mb-4 block">
              Consommations du jour
            </FormLabel>
            <div class="space-y-2">
              <label
                  v-for="item in dietOptions"
                  :key="item.key"
                  :class="[
                    'flex items-center p-3 rounded-xl cursor-pointer transition-all duration-200 border-2',
                    formData[item.key] 
                      ? 'border-button bg-button/15 shadow-sm' 
                      : 'border-gray-200 bg-white hover:border-button/50 hover:bg-button/5'
                  ]"
              >
                <input
                    type="checkbox"
                    v-model="formData[item.key]"
                    class="peer sr-only"
                >
                <span :class="[
                  'w-5 h-5 rounded border-2 transition-all duration-200 flex items-center justify-center mr-3',
                  formData[item.key] 
                    ? 'bg-button border-button' 
                    : 'bg-white border-gray-300 peer-hover:border-button/50'
                ]">
                  <i v-if="formData[item.key]" class="material-symbols-outlined text-white text-sm">check</i>
                </span>

                <span :class="[
                  'w-10 h-10 rounded-full flex items-center justify-center mr-3 transition-all',
                  formData[item.key] ? 'bg-button/20' : 'bg-gray-100'
                ]">
                  <i :class="[
                    'material-symbols-outlined text-xl',
                    formData[item.key] ? 'text-button' : 'text-paragraph'
                  ]">
                    {{ item.icon }}
                  </i>
                </span>

                <span :class="[
                  'font-medium transition-colors',
                  formData[item.key] ? 'text-headline' : 'text-paragraph'
                ]">
                  {{ item.label }}
                </span>
              </label>
            </div>
          </FormItem>
        </FormField>

        <FormField name="commentaire">
          <FormItem>
            <FormLabel class="flex items-center gap-2 mb-3">
              <i class="material-symbols-outlined text-button">edit_note</i>
              <span class="text-lg font-semibold text-headline">Notes personnelles</span>
              <span class="text-sm text-paragraph italic font-normal">(optionnel)</span>
            </FormLabel>

            <div class="relative">
              <textarea
                  v-model="formData.commentaire"
                  placeholder="Ajoute des détails sur ton alimentation, ton ressenti, des événements particuliers..."
                  class="w-full p-4 rounded-xl border-2 border-gray-200 focus:border-button transition-all duration-200 resize-none min-h-[120px] bg-form-input text-paragraph placeholder:text-form-placeholder/60"
                  :class="{ 'border-button bg-button/5': formData.commentaire }"
                  maxlength="100"
              ></textarea>

              <div class="absolute bottom-2 right-3 text-xs text-paragraph/60">
                {{ formData.commentaire.length }}/100
              </div>
            </div>
          </FormItem>
        </FormField>
      </div>

      <!-- Étape 7: Douleur -->
      <div v-if="currentStep === 7">
        <h2 class="text-2xl font-bold mb-8 flex items-center justify-center">
          <i class="material-symbols-outlined mr-2">sick</i>Douleur
        </h2>
        <div class="mb-10">
          <div class="flex justify-between items-center mb-2">
            <span class="text-lg font-medium">Niveau de douleur</span>
            <span class="text-2xl font-bold text-button">{{ formData.douleurMoyenne[0] }}/10</span>
          </div>
          <Slider v-model="formData.douleurMoyenne" :min="0" :max="10" :step="1"/>
          <div class="flex justify-between text-sm text-gray-500 mt-1">
            <span>Aucune douleur</span>
            <span>Douleur maximale</span>
          </div>
        </div>
      </div>

      <!-- Boutons de navigation -->
      <div class="mt-auto flex justify-between pt-6">
        <Button
            @click="prevStep"
            v-if="currentStep > 1"
            variant="outline"
            class="flex items-center gap-2"
        >
          <i class="material-symbols-outlined">arrow_back</i>
          Précédent
        </Button>
        <div v-else></div>

        <Button
            @click="nextStep"
            v-if="currentStep < 7"
            :disabled="!isStepValid"
            variant="custom"
            class="flex items-center gap-2"
        >
          Suivant
          <i class="material-symbols-outlined">arrow_forward</i>
        </Button>
        <Button
            @click="submitForm"
            v-if="currentStep === 7"
            variant="custom"
            :disabled="!isStepValid"
            class="flex items-center gap-2"
        >
          <i class="material-symbols-outlined">check_circle</i>
          Soumettre
        </Button>
      </div>
    </section>

    <!-- Section après soumission avec onglets -->
    <div v-if="isSubmitted" class="w-full">
      <Tabs default-value="recap" class="w-full">
        <TabsList class="bilan-tabs-list">
          <TabsTrigger value="recap" class="bilan-tab-trigger">Récapitulatif & Historique</TabsTrigger>
          <TabsTrigger value="dashboard" class="bilan-tab-trigger">Analyse & Tendances</TabsTrigger>
        </TabsList>

        <TabsContent value="recap">
          <div class="confirmation-message w-full">

            <Card class="container mt-4 mx-auto w-full bg-clearer rounded-3xl shadow-xl ml-auto flex flex-col">
              <CardContent>
                <BilanWeekSelector
                    :bilans="bilans"
                    :selectedDate="selectedDate"
                    @update:selectedDate="handleDateSelection"
                />
              </CardContent>
            </Card>

            <!-- Section Bilan du jour sélectionné -->
            <Card class="container mt-4 mx-auto w-full bg-clearer rounded-3xl shadow-xl ml-auto flex flex-col">
              <CardHeader>
                <CardTitle class="flex items-center">
                  {{ selectedDateTitle }}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div v-if="selectedBilan">
                  <div class="grid grid-cols-2 md:grid-cols-3 gap-2 mb-4">
                    <div class="bg-white rounded-xl border border-gray-100 p-3">
                      <p class="text-xs text-muted-foreground flex items-center gap-1">
                        <span :class="`material-symbols-outlined text-base`">{{ moodIconMapping[selectedBilan.mood] }}</span>
                        Humeur
                      </p>
                      <p class="text-base font-semibold text-headline mt-1">{{ selectedBilan.mood }}</p>
                    </div>
                    <div class="bg-white rounded-xl border border-gray-100 p-3">
                      <p class="text-xs text-muted-foreground flex items-center gap-1">
                        <i class="material-symbols-outlined text-base">psychology</i>
                        Stress moyen
                      </p>
                      <p class="text-base font-semibold text-headline mt-1">{{ selectedBilanStress }}</p>
                    </div>
                    <div class="bg-white rounded-xl border border-gray-100 p-3">
                      <p class="text-xs text-muted-foreground flex items-center gap-1">
                        <i class="material-symbols-outlined text-base">bedtime</i>
                        Fatigue
                      </p>
                      <p class="text-base font-semibold text-headline mt-1">{{ selectedBilan.fatigue }}/5</p>
                    </div>
                    <div class="bg-white rounded-xl border border-gray-100 p-3">
                      <p class="text-xs text-muted-foreground flex items-center gap-1">
                        <i class="material-symbols-outlined text-base">sick</i>
                        Douleur
                      </p>
                      <p class="text-base font-semibold text-headline mt-1">{{ selectedBilan.douleurMoyenne }}/10</p>
                    </div>
                    <div class="bg-white rounded-xl border border-gray-100 p-3">
                      <p class="text-xs text-muted-foreground flex items-center gap-1">
                        <i class="material-symbols-outlined text-base">footprint</i>
                        Pas
                      </p>
                      <p class="text-base font-semibold text-headline mt-1">{{ selectedBilan.pas }}</p>
                    </div>
                    <div class="bg-white rounded-xl border border-gray-100 p-3">
                      <p class="text-xs text-muted-foreground flex items-center gap-1">
                        <i class="material-symbols-outlined text-base">water_drop</i>
                        Hydratation
                      </p>
                      <p class="text-base font-semibold text-headline mt-1">{{ selectedBilan.hydratation }} L</p>
                    </div>
                  </div>

                  <div class="bg-white rounded-xl border border-gray-100 p-3 mb-3">
                    <p class="text-xs text-muted-foreground flex items-center gap-1 mb-2">
                      <i class="material-symbols-outlined text-base">restaurant</i>
                      Alimentation
                    </p>
                    <div class="flex flex-wrap gap-2">
                      <span v-if="selectedBilan.gluten"
                            class="inline-flex items-center gap-1 px-2 py-1 rounded-full bg-orange-100 text-orange-700 text-xs"
                            title="Consommation de gluten">
                        <i class="material-symbols-outlined text-sm">bakery_dining</i>
                        Gluten
                      </span>
                      <span v-if="selectedBilan.lactose"
                            class="inline-flex items-center gap-1 px-2 py-1 rounded-full bg-blue-100 text-blue-700 text-xs"
                            title="Consommation de lactose">
                        <i class="material-symbols-outlined text-sm">icecream</i>
                        Lactose
                      </span>
                      <span v-if="selectedBilan.grignotage"
                            class="inline-flex items-center gap-1 px-2 py-1 rounded-full bg-purple-100 text-purple-700 text-xs"
                            title="Grignotage dans la journée">
                        <i class="material-symbols-outlined text-sm">cookie</i>
                        Grignotage
                      </span>
                      <span v-if="!selectedBilan.gluten && !selectedBilan.lactose && !selectedBilan.grignotage"
                            class="text-gray-500 italic text-sm">
                        Aucune consommation signalée
                      </span>
                    </div>
                  </div>

                  <div v-if="selectedBilan.commentaire" class="bg-white rounded-xl border border-gray-100 p-3">
                    <p class="text-xs text-muted-foreground flex items-center gap-1 mb-1">
                      <i class="material-symbols-outlined text-base">comment</i>
                      Commentaire
                    </p>
                    <p class="text-sm text-paragraph">{{ selectedBilan.commentaire }}</p>
                  </div>
                </div>
                <div v-else class="text-center py-8">
                  <div class="flex flex-col items-center gap-4">
                    <i class="material-symbols-outlined text-6xl text-gray-300">event_busy</i>
                    <div>
                      <p class="text-xl font-semibold text-headline mb-2">
                        Aucun bilan pour cette date
                      </p>
                    </div>

                    <Button
                        @click="fillBilanForDate(selectedDate)"
                        variant="custom"
                        size="lg"
                        class="flex items-center gap-2"
                    >
                      <i class="material-symbols-outlined">add_circle</i>
                      Remplir le bilan pour ce jour
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>

            <!-- Section Historique avec graphique -->
            <Card class="container !mx-0 mt-4 w-full bg-clearer rounded-3xl shadow-xl ml-auto flex flex-col">
              <CardHeader>
                <CardTitle class="flex items-center">
                  <i class="material-symbols-outlined mr-2">show_chart</i> Évolution hebdomadaire
                </CardTitle>
              </CardHeader>
              <CardContent>
                <SelectWeek v-model="selectedWeekYear" @update:years="handleUpdateYears" class="text-left"/>
                <LineChart
                    :data="chartData"
                    :categories="['stress', 'fatigue', 'humeur', 'douleur']"
                    index="date"
                    :colors="['#ff6b6b', '#4ecdc4', '#ffa726', '#8e44ad']"
                    :yFormatter="(value) => `${value}`"
                    :yDomain="[0, 10]"
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


<script setup lang="ts">
import {ref, computed, watch, onMounted} from 'vue';
import {Button} from '@/shared/components/ui/button';
import {Progress} from '@/shared/components/ui/progress';
import {Slider} from '@/shared/components/ui/slider';
import BackButton from "@/shared/components/BackButton.vue";
import {Card, CardContent, CardHeader, CardTitle} from "@/shared/components/ui/card";
import {format, startOfWeek, isToday} from 'date-fns';
import {fr} from 'date-fns/locale';
import {LineChart} from "@/shared/components/ui/chart-line";
import SelectWeek from "@/shared/components/SelectWeek.vue";
import {FormField, FormItem, FormLabel} from "@/shared/components/ui/form";
import RoundSlider from "@/shared/components/CircularSlider.vue";
import {Input} from "@/shared/components/ui/input";
import apiService from "@/shared/services/apiService";
import {useAuthStore} from "@/features/auth/store/auth";
import {Skeleton} from "@/shared/components/ui/skeleton";
import type {BilanQuotidien} from "@/features/bilan-quotidien/types/bilan-quotidien";
import DashboardBilanQuotidien from "@/features/bilan-quotidien/components/DashboardBilanQuotidien.vue";
import {Tabs, TabsContent, TabsList, TabsTrigger} from "@/shared/components/ui/tabs";
import BilanWeekSelector from "@/features/bilan-quotidien/components/BilanWeekSelector.vue";
import {toast} from "@/shared/components/ui/toast";
import {useSync} from "@/shared/composables/useSync";

const currentStep = ref(1);
const isSubmitted = ref(false);
const justSubmitted = ref(false);
const carnetSanteId = useAuthStore().user!.carnetSanteId;
const isLoading = ref(true);

const selectedDate = ref(new Date());
const selectedWeekYear = ref(format(new Date(), 'yyyy-\'W\'II'));
const startYear = ref(new Date().getFullYear());
const endYear = ref(new Date().getFullYear());

const bilans = ref<BilanQuotidien[]>([]);

const completedSteps = ref<Set<number>>(new Set());
const editingBilanId = ref<number | null>(null);


onMounted(() => {
  fetchBilans();
});

watch(selectedWeekYear, () => {
  const alreadyFetched = bilans.value.some(bilan => {
    const bilanDate = new Date(bilan.date);
    return (
        bilanDate.getFullYear() === endYear.value &&
        format(bilanDate, "II") === selectedWeekYear.value.split("-W")[1]
    );
  });
  if (!alreadyFetched) {
    fetchBilans();
  }
});

const handleUpdateYears = ({startYear: start, endYear: end}) => {
  startYear.value = start;
  endYear.value = end;
};

const formData = ref({
  date: new Date(),
  carnetSanteId: carnetSanteId,
  mood: '',
  stressPro: [5],
  stressPerso: [5],
  fatigue: [5],
  pas: 0,
  hydratation: 0,
  douleurMoyenne: [5],
  gluten: false,
  lactose: false,
  grignotage: false,
  commentaire: ''
});

const todayBilan = ref({
  mood: '',
  stressPro: 0,
  stressPerso: 0,
  fatigue: 0,
  pas: 0,
  hydratation: 0,
  douleurMoyenne: 0,
});

const moods = [
  {value: 'Heureuse', label: 'Positive', icon: 'sentiment_satisfied'},
  {value: 'Neutre', label: 'Neutre', icon: 'sentiment_neutral'},
  {value: 'Triste', label: 'Négative', icon: 'sentiment_dissatisfied'},
];

const moodMapping = {
  'Heureuse': 5,
  'Neutre': 3,
  'Triste': 0
};

const moodIconMapping = {
  'Heureuse': 'sentiment_satisfied',
  'Neutre': 'sentiment_neutral',
  'Triste': 'sentiment_dissatisfied'
};

const dietOptions = [
  {key: 'gluten', label: 'Consommation de gluten', icon: 'bakery_dining'},
  {key: 'lactose', label: 'Consommation de lactose', icon: 'icecream'},
  {key: 'grignotage', label: 'Grignotage dans la journée', icon: 'cookie'}
];

const stepTitles = {
  1: 'Humeur',
  2: 'Stress',
  3: 'Fatigue',
  4: 'Activité',
  5: 'Hydratation',
  6: 'Alimentation',
  7: 'Douleur'
};

const selectedBilan = computed(() => {
  if (!bilans.value || bilans.value.length === 0) return null;

  const selectedDateString = format(selectedDate.value, 'yyyy-MM-dd');
  return bilans.value.find(bilan =>
      format(new Date(bilan.date), 'yyyy-MM-dd') === selectedDateString
  ) || null;
});

const selectedDateTitle = computed(() => {
  if (isToday(selectedDate.value)) {
    return "Bilan d'aujourd'hui";
  } else {
    return `Bilan du ${format(selectedDate.value, 'dd MMMM yyyy', {locale: fr})}`;
  }
});

const selectedBilanStress = computed(() => {
  if (!selectedBilan.value) return '-';
  return (((selectedBilan.value.stressPro + selectedBilan.value.stressPerso) / 2).toFixed(1)) + '/5';
});

const handleDateSelection = (date: Date) => {
  selectedDate.value = date;
};

const filteredBilans = computed<BilanQuotidien[]>(() => {
  if (!selectedWeekYear.value) return [];
  const [year, week] = selectedWeekYear.value.split('-W');
  const startDate = startOfWeek(new Date(Number(endYear.value), 0, 1), {weekStartsOn: 1});
  const adjustedStartDate = new Date(startDate.setDate(startDate.getDate() + (Number(week) - 1) * 7));
  const endDate = new Date(adjustedStartDate);
  endDate.setDate(adjustedStartDate.getDate() + 6);
  endDate.setHours(23, 59, 59, 999);

  return bilans.value.filter((bilan: BilanQuotidien) => {
    const bilanDate = new Date(bilan.date);
    return bilanDate >= adjustedStartDate && bilanDate <= endDate &&
        bilanDate.getFullYear() >= startYear.value && bilanDate.getFullYear() <= endYear.value;
  });
});

const chartData = computed(() => {
  return filteredBilans.value.map(bilan => ({
    date: format(new Date(bilan.date), 'dd/MM/yyyy'),
    stress: (bilan.stressPro + bilan.stressPerso) / 2,
    fatigue: bilan.fatigue,
    humeur: moodMapping[bilan.mood],
    douleur: bilan.douleurMoyenne
  }));
});

// Fonctions de navigation
const goToStep = (step: number) => {
  if (canAccessStep(step)) {
    currentStep.value = step;
  }
};

const canAccessStep = (step: number) => {
  // On peut accéder à l'étape actuelle, aux étapes précédentes et à l'étape suivante si l'actuelle est valide
  return step <= currentStep.value || (step === currentStep.value + 1 && isStepValid.value);
};

const getStepTitle = (step: number) => {
  return stepTitles[step] || `Étape ${step}`;
};

const selectMood = (mood: string) => {
  formData.value.mood = mood;
  markStepCompleted(1);
};

const markStepCompleted = (step: number) => {
  completedSteps.value.add(step);
};

const nextStep = () => {
  if (currentStep.value < 7 && isStepValid.value) {
    markStepCompleted(currentStep.value);
    currentStep.value++;
  }
};

const prevStep = () => {
  if (currentStep.value > 1) {
    currentStep.value--;
  }
};

watch(() => formData.value.mood, (newVal) => {
  if (newVal) markStepCompleted(1);
});

watch(() => [formData.value.stressPro, formData.value.stressPerso], () => {
  if (formData.value.stressPro[0] !== undefined && formData.value.stressPerso[0] !== undefined) {
    markStepCompleted(2);
  }
}, {deep: true});

watch(() => formData.value.fatigue, () => {
  if (formData.value.fatigue[0] !== undefined) markStepCompleted(3);
}, {deep: true});

watch(() => formData.value.pas, (newVal) => {
  if (newVal > 0) markStepCompleted(4);
});

watch(() => formData.value.hydratation, (newVal) => {
  if (newVal > 0) markStepCompleted(5);
});

watch(() => formData.value.douleurMoyenne, () => {
  if (formData.value.douleurMoyenne[0] !== undefined) markStepCompleted(7);
}, {deep: true});

const { handleOfflineOperation } = useSync();

const submitForm = async () => {
  try {
    const newBilan: BilanQuotidien = {
      id: editingBilanId.value || 0,
      ...formData.value,
      stressPro: formData.value.stressPro[0],
      stressPerso: formData.value.stressPerso[0],
      fatigue: formData.value.fatigue[0],
      douleurMoyenne: formData.value.douleurMoyenne[0],
    };

    await handleOfflineOperation(
      () => apiService.postBilanQuotidien(newBilan),
      {
        endpoint: 'BilanQuotidien',
        method: 'POST',
        data: newBilan,
        onSuccess: (response) => {
          bilans.value = [...bilans.value, {...newBilan, id: response.id}];
          toast({
            title: 'Bilan enregistré',
            description: 'Le bilan a été créé avec succès.',
            variant: 'custom'
          });

          selectedDate.value = formData.value.date;
          isSubmitted.value = true;
          justSubmitted.value = true;
          editingBilanId.value = null;

          setTimeout(() => {
            justSubmitted.value = false;
          }, 3000);
        },
        onOfflineQueued: () => {
          selectedDate.value = formData.value.date;
          isSubmitted.value = true;
          justSubmitted.value = true;
          editingBilanId.value = null;
        },
        successMessage: 'Bilan enregistré (sera synchronisé)',
        errorMessage: 'Impossible d\'enregistrer le bilan',
      }
    );
  } catch (error) {
    console.error('Error submitting form:', error);
  }
};

const resetForm = () => {
  formData.value = {
    date: formData.value.date,
    carnetSanteId: carnetSanteId,
    mood: '',
    stressPro: [5],
    stressPerso: [5],
    fatigue: [5],
    pas: 0,
    hydratation: 0,
    douleurMoyenne: [5],
    gluten: false,
    lactose: false,
    grignotage: false,
    commentaire: ''
  };
};

const isStepValid = computed(() => {
  switch (currentStep.value) {
    case 1:
      return !!formData.value.mood;
    case 2:
      return formData.value.stressPro[0] !== undefined && formData.value.stressPerso[0] !== undefined;
    case 3:
      return formData.value.fatigue[0] !== undefined;
    case 4:
      return formData.value.pas > 0;
    case 5:
      return formData.value.hydratation > 0;
    case 6:
      return true; // Toujours valide (commentaire optionnel)
    case 7:
      return formData.value.douleurMoyenne[0] !== undefined;
    default:
      return false;
  }
});

const fetchBilans = async () => {
  isLoading.value = true;
  try {
    const [year, week] = selectedWeekYear.value.split('-W');
    const response = await apiService.getBilanQuotidienByWeek(carnetSanteId, week, endYear.value.toString());
    bilans.value = [...bilans.value, ...(response || [])];
    const today = format(new Date(), 'yyyy-MM-dd');
    const bilan = response.find((bilan: any) => format(new Date(bilan.date), 'yyyy-MM-dd') === today);
    if (bilan) {
      todayBilan.value = bilan;
      isSubmitted.value = true;
      selectedDate.value = new Date();
    }
  } catch (error) {
    console.error('Error fetching bilans:', error);
  } finally {
    isLoading.value = false;
  }
};

const fillBilanForDate = (date: Date) => {
  resetForm();
  formData.value.date = new Date(date);
  editingBilanId.value = null;
  currentStep.value = 1;
  completedSteps.value.clear();
  isSubmitted.value = false;

  toast({
    title: 'Nouveau bilan',
    description: `Remplissez le bilan pour le ${format(date, 'd MMMM yyyy', {locale: fr})}`,
    variant: 'custom'
  });
};

</script>

<style scoped>
/* Taille par défaut des icônes Material */
.material-symbols-outlined {
  font-size: 1.5rem;
}

/* Grandes icônes pour les titres d'étapes */
h2 .material-symbols-outlined {
  font-size: 3.2rem;
}

/* Icônes dans les listes du récapitulatif */
li .material-symbols-outlined {
  font-size: 2.5rem;
}

/* Petites icônes dans les boutons de navigation */
.mt-auto button .material-symbols-outlined,
Button .material-symbols-outlined {
  font-size: 1.25rem;
}

/* Icônes dans les pastilles de navigation */
.w-10.h-10 .material-symbols-outlined {
  font-size: 1rem;
}

/* Icônes moyennes pour les labels de formulaire */
.text-button.material-symbols-outlined {
  font-size: 1.5rem;
}

/* Icônes dans les mood buttons */
.text-6xl.material-symbols-outlined {
  font-size: 3.5rem !important;
}

/* Icônes dans les checkboxes */
.text-sm.material-symbols-outlined {
  font-size: 0.875rem !important;
}

/* Icônes dans les cercles des options d'alimentation */
.text-xl.material-symbols-outlined {
  font-size: 1.25rem !important;
}

.confirmation-message {
  text-align: center;
  font-size: 1.5rem;
}

.check-icon {
  font-size: 48px;
  color: var(--button);
}

.summary-list {
  list-style: none;
  padding: 0;
}

.summary-list li {
  display: flex;
  align-items: center;
  margin-bottom: 8px;
}

textarea {
  font-family: var(--font-text);
  line-height: 1.6;
}

textarea::placeholder {
  color: var(--form-placeholder);
  opacity: 0.6;
}

textarea:focus {
  outline: none;
}

textarea::-webkit-scrollbar {
  width: 8px;
}

textarea::-webkit-scrollbar-track {
  background: var(--background);
  border-radius: 10px;
}

textarea:focus {
  border-color: var(--button);
  --tw-ring-color: rgba(255, 122, 153, 0.5);
  --tw-ring-offset-color: var(--background);
}

textarea::-webkit-scrollbar-thumb {
  background: var(--button);
  border-radius: 10px;
}

textarea::-webkit-scrollbar-thumb:hover {
  background: #ff7a99;
}

/* Animation pour les boutons */
button:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

label:active {
  transform: scale(0.98);
}

/* Transitions */
.material-symbols-outlined {
  transition: all 0.2s ease;
}

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
  .bilan-stepper {
    flex-wrap: wrap;
    justify-content: center;
  }

  .bilan-step-dot {
    width: 2rem !important;
    height: 2rem !important;
    font-size: 0.75rem;
  }

  .bilan-tab-trigger {
    font-size: 0.8rem;
    padding-left: 0.5rem;
    padding-right: 0.5rem;
  }
}
</style>