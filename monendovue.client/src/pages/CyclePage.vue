<template>
  <div class="flex-column-container">
    <BackButton/>
    <div class="flex flex-col self-baseline w-full">
      <h2 class="text-2xl flex gap-2"><i class="material-symbols-outlined text-3xl">menstrual_health</i>Cycle menstruel
      </h2>
    </div>
    <Tabs default-value="cycles" class="w-full">
      <TabsList>
        <TabsTrigger value="cycles">Mes cycles</TabsTrigger>
        <TabsTrigger value="symptomes">Symptômes</TabsTrigger>
      </TabsList>
      <TabsContent value="cycles">
        <section class="container !mt-0 mx-auto py-4 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
          <div class="flex flex-col items-center m-auto mb-2" style="max-width: 46%">
            <div class="flex items-center">
              <button @click="previousMonth" class="mr-2">
                <i class="material-symbols-outlined">chevron_left</i>
              </button>
              <input class="max-w-40" type="month" id="month-year" v-model="selectedMonthYear"/>
              <button @click="nextMonth" class="ml-2">
                <i class="material-symbols-outlined">chevron_right</i>
              </button>
            </div>
          </div>
          <Calendar
              v-model="calendarValue"
              class="rounded-md border"
              :joursRegles="joursRegles"
              :joursOvulation="joursOvulation"
              :joursFertiles="joursFertiles"
              :joursSpotting="joursSpotting"
          />
        </section>
<!--        <section>-->
<!--          <div class="flex flex-wrap gap-4">-->
<!--            <Card class="w-full md:w-72 bg-clearer">-->
<!--              <CardHeader>-->
<!--                <CardTitle class="text-lg">Phase actuelle</CardTitle>-->
<!--              </CardHeader>-->
<!--              <CardContent>-->
<!--                <div class="text-sm space-y-2">-->
<!--                  <p class="text-headline">{{ phaseActuelle }}</p>-->
<!--                  <p>{{ phaseDescription }}</p>-->
<!--                </div>-->
<!--              </CardContent>-->
<!--            </Card>-->
<!--          </div>-->
<!--        </section>-->
<!--        <section class="flex flex-wrap gap-2 h-full">-->
<!--          <Card class="w-full flex-1 bg-clearer cursor-default">-->
<!--            <CardHeader>-->
<!--              <CardTitle class="text-base">Durée du précédent cycle</CardTitle>-->
<!--            </CardHeader>-->
<!--            <CardContent>-->
<!--              <p>28 jours</p>-->
<!--            </CardContent>-->
<!--          </Card>-->
<!--          <Card class="w-full flex-1 bg-clearer cursor-default">-->
<!--            <CardHeader>-->
<!--              <CardTitle class="text-base">Durée des précédentes règles</CardTitle>-->
<!--            </CardHeader>-->
<!--            <CardContent>-->
<!--              <p>5 jours</p>-->
<!--            </CardContent>-->
<!--          </Card>-->
<!--        </section>-->
      </TabsContent>
      <TabsContent value="symptomes">
        <section class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
          <div class="flex justify-between items-center mb-4 flex flex-wrap gap-2 h-full">
            <h2 class="text-2xl flex gap-2 ml-2">
              <i class="material-symbols-outlined text-3xl ml-auto">gynecology</i>Symptômes
            </h2>
            <div class="form-modal">
              <Dialog>
                <DialogTrigger class="flex gap-2 items-center cursor-pointer hover:opacity-80 transition-opacity">
                  <Button variant="custom">
                    <span class="hide-xsm">Ajouter une entrée</span>
                    <i class="material-symbols-outlined">add</i>
                  </Button>
                </DialogTrigger>
                <DialogContent>
                  <DialogHeader class="text-2xl">
                    <DialogTitle>Ajouter un symptôme</DialogTitle>
                  </DialogHeader>
                  <form class="flex flex-col gap-6" @submit="onSubmit">
                    <FormField v-slot="{ componentField }" name="typeSymptome">
                      <FormItem>
                        <FormLabel>Type</FormLabel>
                        <FormControl>
                          <Select v-model="entry.typeSymptome" v-bind="componentField">
                            <SelectTrigger>
                              <SelectValue v-bind="componentField">
                                {{ entry.typeSymptome || 'Sélectionner un type de symptôme' }}
                              </SelectValue>
                            </SelectTrigger>
                            <SelectContent>
                              <SelectGroup label="Type de symptôme">
                                <SelectItem value="Spotting">Spotting</SelectItem>
                                <SelectItem value="Nausée">Nausée</SelectItem>
                                <SelectItem value="Fatigue">Fatigue</SelectItem>
                                <SelectItem value="Acné">Acné</SelectItem>
                                <SelectItem value="Autre">Autre</SelectItem>
                              </SelectGroup>
                            </SelectContent>
                          </Select>
                        </FormControl>
                        <FormMessage/>
                      </FormItem>
                    </FormField>
                    <div class="flex items-center gap-8">
                      <FormField v-slot="{ componentField }" name="date">
                        <FormItem>
                          <FormLabel>Date</FormLabel>
                          <FormControl>
                            <Input type="date" v-model="entry.date" v-bind="componentField"/>
                          </FormControl>
                          <FormMessage/>
                        </FormItem>
                      </FormField>
                      <FormField v-slot="{ componentField }" name="time">
                        <FormItem>
                          <FormLabel>Heure</FormLabel>
                          <FormControl>
                            <Input type="time" v-model="entry.time" v-bind="componentField"/>
                          </FormControl>
                          <FormMessage/>
                        </FormItem>
                      </FormField>
                    </div>
                    <FormField v-slot="{ componentField }" name="intensite">
                      <FormItem>
                        <FormLabel>Intensité</FormLabel>
                        <FormControl>
                          <Slider v-bind="componentField" :default-value="[5]" :max="10" :min="1" :step="1"/>
                        </FormControl>
                        <FormMessage/>
                      </FormItem>
                    </FormField>
                    <FormField v-slot="{ componentField }" name="commentaire">
                      <FormItem>
                        <FormLabel>Un commentaire ? <span class="">(optionnel)</span></FormLabel>
                        <FormControl>
                          <Input type="text" placeholder="Écrivez ici" v-bind="componentField"/>
                        </FormControl>
                        <FormMessage/>
                      </FormItem>
                    </FormField>
                    <Button type="submit" variant="custom" class="mt-4" @click="onSubmit">
                      Enregistrer
                    </Button>
                  </form>
                </DialogContent>
              </Dialog>
            </div>
          </div>

          <div v-if="isLoading" class="flex flex-col space-y-3">
            <Skeleton class="h-[300px] w-full mt-4 rounded-xl"/>
          </div>
          <Datatable v-else-if="entries.length > 0" :entries="entries" :columns="columns" :deleteFunction="deleteSymptome">
            <thead>
            <tr>
              <th>Type</th>
              <th>Date</th>
              <th>Heure</th>
              <th>Intensité</th>
              <th>Commentaire</th>
              <th></th>
            </tr>
            </thead>
          </Datatable>
          <div v-else class="flex justify-center items-center h-32">
            <p class="text-2xl text-center">Aucune donnée enregistrée</p>
          </div>
          
        </section>
      </TabsContent>
    </Tabs>
  </div>
</template>

<script setup lang="ts">
import {Calendar} from '@/components/ui/calendar'
import {type DateValue, getLocalTimeZone, today, parseDate} from '@internationalized/date'
import {type Ref, ref, onMounted, watch, computed} from 'vue'
import BackButton from "@/components/BackButton.vue"
import apiService from "@/services/apiService";
import {useAuthStore} from "@/store/auth";
import {Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger} from "@/components/ui/dialog";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {FormControl, FormItem, FormLabel, FormField, FormMessage} from "@/components/ui/form";
import {Card, CardContent, CardHeader, CardTitle} from "@/components/ui/card";
import {Tabs, TabsContent, TabsList, TabsTrigger} from "@/components/ui/tabs";
import {format} from 'date-fns';
import {Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue,} from '@/components/ui/select'
import {useForm} from 'vee-validate'
import {toTypedSchema} from '@vee-validate/zod'
import * as z from 'zod'
import {toast} from "@/components/ui/toast";
import {Slider} from "@/components/ui/slider";
import Datatable from "@/components/Datatable.vue";
import {Skeleton} from "@/components/ui/skeleton";
import type {SymptomeCycle} from "@/interfaces/symptome-cycle";


interface CycleData {
  date: Date;
  type: 'regles' | 'ovulation' | 'fertile' | 'spotting';
}

const selectedMonthYear = ref(format(new Date(), 'yyyy-MM'));
const value = ref(today(getLocalTimeZone())) as Ref<DateValue>
const month = ref(value.value.month)
const year = ref(value.value.year)
const {user} = useAuthStore();
const joursRegles = ref<Date[]>([])
const joursOvulation = ref<Date[]>([])
const joursFertiles = ref<Date[]>([])
const joursSpotting = ref<Date[]>([])
const cycleMoyen = ref(28)

type Entry = Record<string, any>;

const entries = ref<Entry[]>([]);
const entry: Entry = {
  typeSymptome: '',
  date: '',
  time: '',
  intensite: 0,
  commentaire: ''
};

const columns: any = [
  {data: 'typeSymptome'},
  {data: 'date'},
  {data: 'time'},
  {data: 'intensite'},
  {data: 'commentaire'},
  {data: null,
    defaultContent: '<span class="material-symbols-outlined delete-btn">delete</span>'
  }
]

const isLoading = ref(true);


onMounted(() => {
  fetchJoursRegles();
  fetchSymptomesCycle(month.value, year.value);
});

const calendarValue = computed(() => {
  const [year, month] = selectedMonthYear.value.split('-').map(Number)
  const paddedMonth = month.toString().padStart(2, '0')
  return parseDate(`${year}-${paddedMonth}-01`)
})

const previousMonth = () => {
  const [year, month] = selectedMonthYear.value.split('-').map(Number);
  const newDate = new Date(year, month - 2, 1);
  selectedMonthYear.value = format(newDate, 'yyyy-MM');
};

const nextMonth = () => {
  const [year, month] = selectedMonthYear.value.split('-').map(Number);
  const newDate = new Date(year, month, 1);
  selectedMonthYear.value = format(newDate, 'yyyy-MM');
};


watch(calendarValue, (value) => {
  month.value = value.month;
  year.value = value.year;
  calculateFertilePeriodsForMonth(joursRegles.value);
});

watch(selectedMonthYear, () => {
  fetchJoursRegles();
});

const fetchJoursRegles = async () => {
  try {
    const response = await apiService.getJoursReglesByMonth(user!.carnetSanteId, month.value, year.value);
    joursRegles.value = response.$values.map(jour => new Date(jour.date));

    // Calcul des périodes fertiles et d'ovulation pour le mois sélectionné
    const {fertileDays, ovulationDates} = calculateFertilePeriodsForMonth(joursRegles.value);

    joursFertiles.value = fertileDays;
    joursOvulation.value = ovulationDates;

    // Mise à jour de la durée moyenne du cycle
    updateAverageCycle(joursRegles.value);
  } catch (error) {
    console.error('Erreur lors de la récupération des jours de règles:', error);
  }
}

// Fonction pour vérifier si une date est dans le mois sélectionné
const isInSelectedMonth = (date: Date): boolean => {
  return date.getMonth() + 1 === month.value && date.getFullYear() === year.value;
}

// Fonction pour obtenir le premier et dernier jour du mois sélectionné
const getMonthBoundaries = () => {
  const firstDay = new Date(year.value, month.value - 1, 1);
  const lastDay = new Date(year.value, month.value, 0);
  return {firstDay, lastDay};
}

// Fonction pour calculer la période fertile pour le mois sélectionné
const calculateFertilePeriodsForMonth = (reglesDates: Date[]) => {
  const {firstDay, lastDay} = getMonthBoundaries();
  const dates: Date[] = [];
  const ovulationDates: Date[] = [];

  // Trier les dates de règles
  const sortedRegles = reglesDates.sort((a, b) => a.getTime() - b.getTime());

  sortedRegles.forEach((regleDate, index) => {
    // Calculer l'ovulation pour ce cycle
    const ovulationDate = new Date(regleDate);
    ovulationDate.setDate(regleDate.getDate() + (cycleMoyen.value - 14));

    // Calculer la période fertile (5 jours avant, jour de l'ovulation, et 1 jour après)
    for (let i = -5; i <= 1; i++) {
      const fertileDate = new Date(ovulationDate);
      fertileDate.setDate(ovulationDate.getDate() + i);

      // Ne garder que les dates dans le mois sélectionné
      if (isInSelectedMonth(fertileDate)) {
        dates.push(fertileDate);
      }
    }

    // Ajouter la date d'ovulation si elle est dans le mois sélectionné
    if (isInSelectedMonth(ovulationDate)) {
      ovulationDates.push(ovulationDate);
    }
  });

  return {
    fertileDays: dates,
    ovulationDates: ovulationDates
  };
}

// Fonction pour calculer la phase actuelle
const calculateCurrentPhase = (reglesDates: Date[]): string => {
  if (reglesDates.length === 0) return "Données insuffisantes";

  const today = new Date();
  if (!isInSelectedMonth(today)) {
    // Si on n'est pas dans le mois sélectionné, chercher la phase pour une date du mois
    const midMonth = new Date(year.value, month.value - 1, 15);
    const closestRegleDate = findClosestRegleDate(reglesDates, midMonth);
    if (!closestRegleDate) return "Données insuffisantes";

    const daysSinceLastPeriod = Math.floor((midMonth.getTime() - closestRegleDate.getTime()) / (1000 * 60 * 60 * 24));
    return getPhaseFromDays(daysSinceLastPeriod);
  }

  // Pour le mois actuel, utiliser la date d'aujourd'hui
  const closestRegleDate = findClosestRegleDate(reglesDates, today);
  if (!closestRegleDate) return "Données insuffisantes";

  const daysSinceLastPeriod = Math.floor((today.getTime() - closestRegleDate.getTime()) / (1000 * 60 * 60 * 24));
  return getPhaseFromDays(daysSinceLastPeriod);
}

// Fonction utilitaire pour trouver la date de règles la plus proche
const findClosestRegleDate = (reglesDates: Date[], referenceDate: Date): Date | null => {
  if (reglesDates.length === 0) return null;

  return reglesDates.reduce((closest, current) => {
    const currentDiff = Math.abs(current.getTime() - referenceDate.getTime());
    const closestDiff = Math.abs(closest.getTime() - referenceDate.getTime());
    return currentDiff < closestDiff ? current : closest;
  });
}

// Fonction utilitaire pour déterminer la phase basée sur le nombre de jours
const getPhaseFromDays = (days: number): string => {
  if (days <= 5) return "Phase menstruelle";
  if (days <= 13) return "Phase folliculaire";
  if (days <= 16) return "Phase ovulatoire";
  return "Phase lutéale";
}

// Description des phases
const getPhaseDescription = (phase: string): string => {
  const descriptions = {
    "Phase menstruelle": "Début du cycle, règles. Niveau d'énergie plus bas.",
    "Phase folliculaire": "Augmentation de l'énergie et de la créativité. Les follicules se développent.",
    "Phase ovulatoire": "Période la plus fertile. Pic d'énergie et de libido.",
    "Phase lutéale": "Phase post-ovulatoire. Préparation de l'endomètre.",
    "Données insuffisantes": "Ajoutez vos dates de règles pour voir les phases de votre cycle."
  };
  return descriptions[phase as keyof typeof descriptions] || "";
}

// Computed properties
const phaseActuelle = computed(() => {
  return calculateCurrentPhase(joursRegles.value);
});

const phaseDescription = computed(() => {
  return getPhaseDescription(phaseActuelle.value);
});

const updateAverageCycle = (reglesDates: Date[]) => {
  if (reglesDates.length < 2) return;

  const sortedDates = reglesDates.sort((a, b) => a.getTime() - b.getTime());
  const cyclesDuration: number[] = [];

  for (let i = 1; i < sortedDates.length; i++) {
    const cycleDuration = Math.floor(
        (sortedDates[i].getTime() - sortedDates[i - 1].getTime()) / (1000 * 60 * 60 * 24)
    );
    if (cycleDuration > 0 && cycleDuration <= 45) { // Ignorer les cycles anormaux
      cyclesDuration.push(cycleDuration);
    }
  }

  if (cyclesDuration.length > 0) {
    cycleMoyen.value = Math.round(
        cyclesDuration.reduce((acc, curr) => acc + curr, 0) / cyclesDuration.length
    );
  }
}

const fetchSymptomesCycle = async (month: number, year: number) => {
  isLoading.value = true;
  try {
    const response = await apiService.getSymptomesByMonth(user!.carnetSanteId, month, year);
    entries.value = response.map((symptomeCycle: SymptomeCycle) => {
      const date = new Date(symptomeCycle.date);
      const hours = date.getHours().toString().padStart(2, '0');
      const minutes = date.getMinutes().toString().padStart(2, '0');
      const time = `${hours}h${minutes}`;
      return {
        id: symptomeCycle.id,
        typeSymptome: symptomeCycle.typeSymptome,
        date: format(date, 'dd/MM/yyyy').replace(/-/g, '/'),
        time: time,
        intensite: symptomeCycle.intensite,
        commentaire: symptomeCycle.commentaire ? symptomeCycle.commentaire : 'Pas de commentaire'
      }
    })
    joursSpotting.value = response.filter((symptomeCycle: SymptomeCycle) => symptomeCycle.typeSymptome === 'Spotting')
        .map((symptomeCycle: SymptomeCycle) => new Date(symptomeCycle.date));
  } catch (error) {
    console.error('Erreur lors de la récupération des symptômes:', error);
  } finally {
    isLoading.value = false;
  }
}

const formSchema = toTypedSchema(z.object({
  typeSymptome: z.string({
    required_error: 'Le type de symptôme est requis'
  }),
  date: z.string({
    required_error: 'La date est requise'
  }),
  time: z.string({
    required_error: 'L\'heure est requise'
  }),
  intensite: z.array(z.number({
    required_error: 'L\'intensité est requise',
  })),
  commentaire: z.string().optional(),
}))

const form = useForm({
  validationSchema: formSchema,
  initialValues: {
    intensite: [5],
    typeSymptome: "Sélectionnez un symptôme"
  }
});

const onSubmit = form.handleSubmit((values) => {
  const dateTimeString = `${values.date}T${values.time}`;
  const dateTime = new Date(dateTimeString);
  const timezoneOffset = dateTime.getTimezoneOffset() * 60 * 1000;
  const utcDateTime = new Date(dateTime.getTime() - timezoneOffset);

  const valuesWithCarnetSanteId = {
    ...values,
    carnetSanteId: user?.carnetSanteId,
    date: utcDateTime,
    intensite: values.intensite[0],
    commentaire: values.commentaire ? values.commentaire : 'Pas de commentaire',
  };
  apiService.postDonneesSymptomesCycle(valuesWithCarnetSanteId).then((response) => {
    toast({
      title: 'Succès',
      description: 'Symptôme ajouté avec succès',
      variant: 'custom',
    });

    const valuesForView = {
      ...valuesWithCarnetSanteId,
      id: response.id,
      date: format(values.date, 'dd/MM/yyyy'),
      time: values.time.replace(":", "h"),
      intensite: values.intensite[0],
    };
      delete valuesForView.carnetSanteId;
    entries.value.push(valuesForView);
  }).catch((error) => {
    toast({
      title: 'Erreur',
      description: 'Une erreur est survenue lors de l\'ajout du symptôme',
    });
    console.error('Erreur lors de l\'ajout du symptôme:', error);
  });
});

const deleteSymptome = async (id: number) => {
  try {
    await apiService.deleteSymptomeCycle(id);
    entries.value = entries.value.filter((entry) => entry.id !== id);
    toast({
      title: 'Succès',
      description: 'Symptôme supprimé avec succès',
      variant: 'custom',
    });
  } catch (error) {
    toast({
      title: 'Erreur',
      description: 'Une erreur est survenue lors de la suppression du symptôme',
    });
    console.error('Erreur lors de la suppression du symptôme:', error);
  }
}

</script>