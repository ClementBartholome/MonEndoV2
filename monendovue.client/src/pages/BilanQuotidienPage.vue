<template>
  <div class="flex-column-container">
    <BackButton/>
    <div v-if="isLoading" class="flex flex-col space-y-3 p-6 pt-0">
      <Skeleton class="h-[300px] w-full mt-4 rounded-xl"/>
    </div>
    <section v-else v-if="!isSubmitted"
             class="container !mt-0 mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto flex flex-col h-auto">
      <Progress :model-value="(currentStep / 7) * 100" class="mb-6"/>
      <div v-if="currentStep === 1">
        <h2 class="text-2xl font-bold mb-14 flex items-center">
          <i class="material-symbols-outlined mr-2">mood</i> Étape 1: Humeur du jour
        </h2>
        <div class="grid grid-cols-3 gap-4 mb-4">
          <Button
              v-for="mood in moods"
              :key="mood.value"
              @click="selectMood(mood.value)"
              :variant="formData.mood === mood.value ? 'selected' : 'outline'"
              class="h-24 flex flex-col items-center justify-center"
          >
            <component :is="'span'" class="material-symbols-outlined mb-2">{{ mood.icon }}</component>
            {{ mood.label }}
          </Button>
        </div>
      </div>

      <div v-if="currentStep === 2">
        <h2 class="text-2xl font-bold mb-14 flex items-center">
          <i class="material-symbols-outlined mr-2">psychology</i> Étape 2: Niveau de stress
        </h2>
        <div class="mb-14">
          <h3 class="text-xl font-semibold mb-4 flex items-center">
            <i class="material-symbols-outlined mr-2">work</i> Vie pro
          </h3>
          <Slider v-model="formData.stressPro" :default-value="[5]" :min="0" :max="5" :step="1"/>
        </div>
        <div class="mb-14">
          <h3 class="text-xl font-semibold mb-4 flex items-center">
            <i class="material-symbols-outlined mr-2">home</i> Vie perso
          </h3>
          <Slider v-model="formData.stressPerso" :default-value="[5]" :min="0" :max="5" :step="1"/>
        </div>
        <input type="hidden" :value="averageStress">
      </div>

      <div v-if="currentStep === 3">
        <h2 class="text-2xl font-bold mb-14 flex items-center">
          <i class="material-symbols-outlined mr-2">bedtime</i> Étape 3: Niveau de fatigue
        </h2>
        <Slider v-model="formData.fatigue" :default-value="[5]" :min="0" :max="5" :step="1" class="mb-14"/>
      </div>

      <div v-if="currentStep === 4" class="flex flex-col items-center">
        <h2 class="text-2xl font-bold mb-6 flex items-center">Étape 4: Nombre de pas</h2>
        <FormField name="pas">
          <span class="material-symbols-outlined absolute top-[20rem]">footprint</span>
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

      <div v-if="currentStep === 5" class="flex flex-col items-center">
        <h2 class="text-2xl font-bold mb-6 flex items-center">Étape 5: Hydratation</h2>
        <FormField name="hydratation">
          <span class="material-symbols-outlined absolute top-[20rem]">local_drink</span>
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

      <div v-if="currentStep === 6" class="flex flex-col items-center">
        <h2 class="text-2xl font-bold mb-6 flex items-center">
          <i class="material-symbols-outlined mr-2">restaurant</i> Étape 6: Alimentation
        </h2>
        <FormField name="alimentation">
          <div class="flex flex-col items-start">
            <label class="flex items-center mb-2">
              <input type="checkbox" v-model="formData.gluten" class="mr-2"> Consommation de gluten
            </label>
            <label class="flex items-center mb-2">
              <input type="checkbox" v-model="formData.lactose" class="mr-2"> Consommation de lactose
            </label>
            <label class="flex items-center mb-2">
              <input type="checkbox" v-model="formData.grignotage" class="mr-2"> Grignotage dans la journée
            </label>
          </div>
        </FormField>
        <FormField name="commentaire">
          <FormItem class="mb-4">
            <FormLabel>Commentaire</FormLabel>
            <Input v-model="formData.commentaire" placeholder="Commentaire" type="text" class="w-full"/>
          </FormItem>
        </FormField>
      </div>
            
      <div v-if="currentStep === 7">
        <h2 class="text-2xl font-bold mb-14 flex items-center">
          <i class="material-symbols-outlined mr-2">sick</i> Étape 7: Douleur
        </h2>
        <Slider v-model="formData.douleurMoyenne" :default-value="[5]" :min="0" :max="10" :step="1"  class="mb-14"/>
      </div>

      <div class="mt-auto flex justify-end">
        <Button @click="prevStep" v-if="currentStep > 1" variant="outline" class="mr-2">Précédent</Button>
        <Button @click="nextStep" v-if="currentStep < 7" :disabled="isStepValid()" variant="custom">Suivant</Button>
        <Button @click="submitForm" v-if="currentStep === 7" variant="custom" :disabled="isStepValid()">Soumettre
        </Button>
      </div>
    </section>
    <div v-if="isSubmitted" class="confirmation-message w-full">
      <p class="text-xl text-center flex items-center gap-2 justify-center">
        Bilan quotidien enregistré <i class="material-symbols-outlined check-icon mr-2">check_circle</i></p>
      <div class="checkmark-animation">
      </div>
      <Card class="container mt-4 mx-auto w-full bg-clearer rounded-3xl shadow-xl ml-auto flex flex-col">
        <CardHeader>
          <CardTitle class="flex items-center">
            Récapitulatif du bilan
          </CardTitle>
        </CardHeader>
        <CardContent>
          <ul class="summary-list text-xl">
            <li class="flex items-center">
              <span :class="`material-symbols-outlined mood-icon mr-2`">{{ moodIconMapping[todayBilan.mood] }}</span>
              Humeur : {{ todayBilan.mood }}
            </li>
            <li class="flex items-center">
              <i class="material-symbols-outlined stress-icon mr-2">psychology</i> Niveau de stress :
              {{ (todayBilan.stressPro + todayBilan.stressPerso) / 2 }}
            </li>
            <li class="flex items-center">
              <i class="material-symbols-outlined fatigue-icon mr-2">bedtime</i> Niveau de fatigue :
              {{ todayBilan.fatigue.toString() }}
            </li>
            <li class="flex items-center">
              <i class="material-symbols-outlined fatigue-icon mr-2">sick</i> Niveau de douleur :
              {{ todayBilan.douleurMoyenne.toString() }}
            </li>
            <li class="flex items-center">
              <i class="material-symbols-outlined neat-icon mr-2">footprint</i> Nombre de pas :
              {{ todayBilan.pas.toString() }}
            </li>
            <li class="flex items-center">
              <i class="material-symbols-outlined hydratation-icon mr-2">local_drink</i> Hydratation :
              {{ todayBilan.hydratation.toString() + "L" }}
            </li>
          </ul>
        </CardContent>
      </Card>

      <Card class="container !mx-0 mt-4 w-full bg-clearer rounded-3xl shadow-xl ml-auto flex flex-col">
        <CardHeader>
          <CardTitle class="flex items-center">
            <i class="material-symbols-outlined mr-2">history</i> Historique des bilans
          </CardTitle>
        </CardHeader>
        <CardContent>
          <SelectWeek v-model="selectedWeekYear" @update:years="handleUpdateYears" class="text-left"/>
          <LineChart
              :data="chartData"
              :categories="['stress', 'fatigue', 'mood']"
              index="date"
              :colors="['#ff6b6b', '#4ecdc4', '#ffa726']"
              :yFormatter="(value) => `${value}`"
              :yDomain="[0, 5]"
          />
        </CardContent>
      </Card>
    </div>
  </div>
</template>

<script setup lang="ts">
import {ref, computed, watch, onMounted} from 'vue';
import {Button} from '@/components/ui/button';
import {Progress} from '@/components/ui/progress';
import {Slider} from '@/components/ui/slider';
import BackButton from "@/components/BackButton.vue";
import {Card, CardContent, CardHeader, CardTitle} from "@/components/ui/card";
import {format, startOfWeek, endOfWeek, startOfISOWeek, endOfISOWeek} from 'date-fns';
import {LineChart} from "@/components/ui/chart-line";
import SelectWeek from "@/components/SelectWeek.vue";
import {FormField, FormItem, FormLabel} from "@/components/ui/form";
import RoundSlider from "@/components/CircularSlider.vue";
import {Input} from "@/components/ui/input";
import apiService from "@/services/apiService";
import {useAuthStore} from "@/store/auth";
import {Skeleton} from "@/components/ui/skeleton";
import type {BilanQuotidien} from "@/interfaces/bilan-quotidien";

const currentStep = ref(7);
const isSubmitted = ref(false);
const carnetSanteId = useAuthStore().user!.carnetSanteId;
const isLoading = ref(true);

const selectedWeekYear = ref(format(new Date(), 'yyyy-\'W\'II'));
const startYear = ref(new Date().getFullYear());
const endYear = ref(new Date().getFullYear());

const handleUpdateYears = ({ startYear: start, endYear: end }) => {
  startYear.value = start;
  endYear.value = end;
  fetchBilans();
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

const filteredBilans = computed<BilanQuotidien[]>(() => {
  if (!selectedWeekYear.value) return [];
  const [year, week] = selectedWeekYear.value.split('-W');
  const startDate = startOfWeek(new Date(Number(endYear.value), 0, 1), { weekStartsOn: 1 });
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
    // douleurMoyenne: bilan.douleurMoyenne,
    fatigue: bilan.fatigue,
    mood: moodMapping[bilan.mood]
  }));
});

const selectMood = (mood: string) => {
  formData.value.mood = mood;
};

const nextStep = () => {
  if (currentStep.value < 7) {
    currentStep.value++;
  }
};

const prevStep = () => {
  if (currentStep.value > 1) {
    currentStep.value--;
  }
};

const bilans = ref<BilanQuotidien[]>([]);

const submitForm = async () => {
  try {
    console.log('Form Data:', formData.value);
    const newBilan: BilanQuotidien = {
      id: 0,
      ...formData.value,
      stressPro: formData.value.stressPro[0],
      stressPerso: formData.value.stressPerso[0],
      fatigue: formData.value.fatigue[0],
      douleurMoyenne: formData.value.douleurMoyenne[0],
    }
    await apiService.postBilanQuotidien(newBilan);
    bilans.value = [...bilans.value, newBilan];
    todayBilan.value = newBilan;
    isSubmitted.value = true;
  } catch (error) {
    console.error('Error submitting form:', error);
  }
};

const isStepValid = () => {
  if (currentStep.value === 1) {
    return !formData.value.mood;
  } else if (currentStep.value === 2) {
    return !formData.value.stressPro && !formData.value.stressPerso;
  } else if (currentStep.value === 3) {
    return !formData.value.fatigue;
  } else if (currentStep.value === 4) {
    return !formData.value.pas;
  } else if (currentStep.value === 5) {
    return !formData.value.hydratation;
  } else if (currentStep.value === 6) {
    return !formData.value.gluten && !formData.value.lactose && !formData.value.grignotage; 
  } else if (currentStep.value === 7) {
    return !formData.value.douleurMoyenne;
  }
};

const averageStress = computed(() => {
  return (formData.value.stressPro[0] + formData.value.stressPerso[0]) / 2;
});

const fetchBilans = async () => {
  isLoading.value = true;
  try {
    const [year, week] = selectedWeekYear.value.split('-W');
    const response = await apiService.getBilanQuotidienByWeek(carnetSanteId, week, endYear.value.toString());
    bilans.value = response || [];
    const today = format(new Date(), 'yyyy-MM-dd');
    const bilan = response.find((bilan: any) => format(new Date(bilan.date), 'yyyy-MM-dd') === today);
    if (bilan) {
      todayBilan.value = bilan;
      isSubmitted.value = true;
    }
  } catch (error) {
    console.error('Error fetching bilans:', error);
  } finally {
    isLoading.value = false;
  }
};

onMounted(() => {
  fetchBilans();
});

watch(selectedWeekYear, () => {
  fetchBilans();
});
</script>

<style scoped>
.icon-selection button {
  margin: 5px;
  font-size: 24px;
}

.material-symbols-outlined {
  font-size: 3.2rem;
}

li .material-symbols-outlined {
  font-size: 2.5rem;
}

.confirmation-message {
  text-align: center;
  font-size: 1.5rem;
}

.checkmark-animation {
  display: flex;
  align-items: center;
  justify-content: center;
  animation: fadeIn 0.5s ease-in-out;
}

.check-icon {
  font-size: 48px;
  color: var(--button);
}

.summary-card {
  background-color: #f9f9f9;
  border-radius: 8px;
  padding: 16px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  margin-top: 16px;
}

.summary-title {
  font-size: 1.25rem;
  font-weight: bold;
  margin-bottom: 8px;
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

.summary-list i {
  margin-right: 8px;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: scale(0.5);
  }
  to {
    opacity: 1;
    transform: scale(1);
  }
}
</style>