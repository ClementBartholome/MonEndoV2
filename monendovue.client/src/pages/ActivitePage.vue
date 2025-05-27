<template>
  <div class="flex-column-container">
    <BackButton/>
    <section 
             class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
      <div class="flex justify-between items-center mb-4">
        <h2 class="text-2xl flex gap-2 ml-2"><i class="material-symbols-outlined text-3xl ml-auto">directions_run</i>
          Suivi de l'activité</h2>
        <div class="form-modal">
          <Dialog>
            <DialogTrigger class="flex gap-2 items-center cursor-pointer hover:opacity-80 transition-opacity">
              <Button variant="custom">
                <span class="hide-xsm">Ajouter une session</span>
                <i class="material-symbols-outlined">add</i>
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle class="text-2xl">Ajouter une session</DialogTitle>
              </DialogHeader>
              <form class="flex flex-col gap-6" @submit="onSubmit">
                <FormField v-slot="{ componentField }" name="typeActivite">
                  <FormItem>
                    <FormLabel>Type d'activité</FormLabel>
                    <FormControl>
                      <Input type="text" placeholder="Course à pied" v-bind="componentField" :autofocus="false"/>
                    </FormControl>
                    <FormMessage/>
                  </FormItem>
                </FormField>
                <div class="flex items-center gap-8">
                  <FormField v-slot="{ componentField }" name="date">
                    <FormItem>
                      <FormLabel>Date</FormLabel>
                      <FormControl>
                        <Input type="date" v-bind="componentField" class="min-w-28"/>
                      </FormControl>
                      <FormMessage/>
                    </FormItem>
                  </FormField>
                  <FormField v-slot="{ componentField }" name="time" class="min-w-28">
                    <FormItem>
                      <FormLabel>Heure</FormLabel>
                      <FormControl>
                        <Input type="time" v-bind="componentField"/>
                      </FormControl>
                      <FormMessage/>
                    </FormItem>
                  </FormField>
                </div>
                <FormField v-slot="{ componentField }" name="duree">
                  <FormItem>
                    <FormLabel>Durée de l'activité</FormLabel>
                    <FormControl>
                      <Input type="number" placeholder="Durée en minutes" v-bind="componentField"/>
                    </FormControl>
                    <FormMessage/>
                  </FormItem>
                </FormField>
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
                    <FormLabel>Un commentaire ? (optionnel)</FormLabel>
                    <FormControl>
                      <Input type="text" placeholder="Écrivez ici" v-bind="componentField"/>
                    </FormControl>
                    <FormMessage/>
                  </FormItem>
                </FormField>
                <Button class="mt-4" variant="custom" type="submit" @click="onSubmit">
                  Enregistrer
                </Button>
              </form>
            </DialogContent>
          </Dialog>
        </div>
      </div>
      <SelectMonth v-model="selectedMonthYear" />
      <div v-if="isLoading" class="flex flex-col space-y-3">
        <Skeleton class="h-[300px] w-full mt-4 rounded-xl"/>
      </div>
      <Datatable v-else-if="entries.length > 0" :entries="entries" :columns="columns" :deleteFunction="deleteDonneesActivitePhysique">
        <thead>
        <tr>
          <th>Type</th>
          <th>Date</th>
          <th>Heure</th>
          <th>Durée</th>
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
    <div class="flex-row-container w-full gap-8">
      <section class="flex flex-wrap h-full w-8/12 container py-8 px-4 bg-clearer rounded-3xl shadow-xl ml-auto">
        <div class="w-full flex flex-col justify-center items-baseline">
          <div class="flex justify-center items-center gap-4">
            <h2 class="text-2xl self-start flex gap-4">
              <i class="material-symbols-outlined text-3xl ml-2">timeline</i>
              Historique
            </h2>
          </div>
          <div v-if="isLoading" class="flex flex-col space-y-3">
            <Skeleton class="h-[400px] w-full mt-4 rounded-xl"/>
          </div>  
          <p v-else-if="entries.length === 0" class="mt-8 text-2xl text-center">Aucune donnée enregistrée</p>
          <LineChart
              v-else
              :data="chartData"
              :categories="['intensite']"
              index="date"
          />
        </div>
      </section>
      <section
          class="flex flex-col h-auto items-center text-center gap-4 w-4/12 container py-8 bg-clearer rounded-3xl shadow-xl ml-auto">
        <div class="flex gap-4 items-baseline mr-auto ml-2">
          <h2 class="text-2xl self-start flex gap-4">
            <i class="material-symbols-outlined text-3xl">trending_up</i>
            Tendances
          </h2>
        </div>
        <p>Durée totale ({{ entries.length }}
          {{ entries.length > 1 ? 'entrées' : 'entrée' }})</p>
        <span class="text-5xl text-highlight">{{ totalSessionDuration }}</span>
        <!--        <i class="material-symbols-outlined text-7xl text-button">test</i>-->
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import {computed, onMounted, ref, watch} from 'vue'
import {useForm} from 'vee-validate'
import {toTypedSchema} from '@vee-validate/zod'
import * as z from 'zod'

import {Button} from '@/components/ui/button'
import {FormControl, FormField, FormItem, FormLabel, FormMessage,} from '@/components/ui/form'
import {Input} from '@/components/ui/input'
import {LineChart} from "@/components/ui/chart-line";
import Datatable from "@/components/Datatable.vue";

import apiService from "@/services/apiService";

import {useAuthStore} from '@/store/auth';
import {format} from 'date-fns';
import {Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger,} from '@/components/ui/dialog'
import {Slider} from "@/components/ui/slider";
import {useToast} from '@/components/ui/toast';
import {Skeleton} from "@/components/ui/skeleton";
import BackButton from "@/components/BackButton.vue";
import SelectMonth from "@/components/SelectMonth.vue";
import type {DonneesActivitePhysique} from "@/interfaces/donnees-activite-physique";

const {toast} = useToast();
const authStore = useAuthStore();

const columns: any = [
  {data: 'typeActivite'},
  {data: 'date'},
  {data: 'time'},
  {data: 'duree', render: (data) => `${data}min`},
  {data: 'intensite'},
  {data: 'commentaire'},
  {
    data: null,
    defaultContent: '<span class="material-symbols-outlined delete-btn">delete</span>'
  },
];

const selectedMonthYear = ref(format(new Date(), 'yyyy-MM'));

const chartData = computed(() => {
  return entries.value.map((entry: DonneesActivitePhysique) => {
    return {
      date: entry.date,
      type: entry.typeActivite,
      duree: entry.duree,
      intensite: entry.intensite,
      commentaire: entry.commentaire
    };
  });
});
const deleteDonneesActivitePhysique = async (id: number) => {
  try {
    await apiService.deleteDonneesActivitePhysique(id);
    toast({
      title: 'Succès',
      description: 'La session a été supprimée avec succès',
      variant: 'custom',
    });
  } catch (error) {
    toast({
      title: 'Erreur',
      description: 'Une erreur est survenue lors de la suppression de la session.',
    });
    console.error(error);
  }
};

const entries = ref<DonneesActivitePhysique[]>([]);

const isLoading = ref(true);

onMounted(() => {
  const [year, month] = selectedMonthYear.value.split('-').map(Number);
  fetchActiviteEntriesByMonth(month, year);
});

watch(selectedMonthYear, (newValue) => {
  const [year, month] = newValue.split('-').map(Number);
  fetchActiviteEntriesByMonth(month, year);
});

const fetchActiviteEntriesByMonth = async (month: number, year: number) => {
  isLoading.value = true;
  try {
    const response = await apiService.getDonneesActivitePhysiqueByMonth(authStore.user!.carnetSanteId, month, year);
    entries.value = response.map((entry: DonneesActivitePhysique) => {
      const date = new Date(entry.date);
      const hours = date.getHours().toString().padStart(2, '0');
      const minutes = date.getMinutes().toString().padStart(2, '0');
      const time = `${hours}h${minutes}`;
      return {
        id: entry.id,
        typeActivite: entry.typeActivite,
        date: format(date, 'dd/MM/yyyy').replace(/-/g, '/'),
        time: time,
        duree: entry.duree,
        intensite: entry.intensite,
        commentaire: entry.commentaire ? entry.commentaire : 'Pas de détails'
      };
    });
  } catch (error) {
    console.error(error);
  } finally {
    isLoading.value = false;
  }
};

const formSchema = toTypedSchema(z.object({
  typeActivite: z.string({
    required_error: 'Le type d\'activité est requis',
  }),
  date: z.string({
    required_error: 'La date est requise',
  }),
  time: z.string({
    required_error: 'L\'heure est requise',
  }),
  duree: z.number({
    required_error: 'La durée de l\'activité est requise',
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
  }
});

const onSubmit = form.handleSubmit((values) => {
  const dateTimeString = `${values.date}T${values.time}`;
  const dateTime = new Date(dateTimeString);
  const timezoneOffset = dateTime.getTimezoneOffset() * 60 * 1000;
  const utcDateTime = new Date(dateTime.getTime() - timezoneOffset);

  const valuesWithCarnetSanteId: DonneesActivitePhysique = {
    ...values,
    id: 0,
    typeActivite: values.typeActivite,
    intensite: values.intensite[0],
    date: utcDateTime,
    commentaire: values.commentaire ? values.commentaire : 'Pas de commentaire',
    carnetSanteId: authStore.user!.carnetSanteId,
  };
  apiService.postDonneesActivitePhysique(valuesWithCarnetSanteId).then((response) => {
    toast({
      title: 'Succès',
      description: 'La session a été ajoutée avec succès',
      variant: 'custom',
    });

    const valuesForView = {
      ...valuesWithCarnetSanteId, date: format(values.date, 'dd/MM/yyyy'),
      id: response.id,
      time: values.time.replace(":", "h"),
      intensite: values.intensite[0],
    };
    entries.value.push(valuesForView);
  }).catch((error) => {
    toast({
      title: 'Erreur',
      description: 'Un problème est survenu lors de l\'ajout de la douleur.',
    });
    console.error(error);
  });
});

const totalSessionDuration = computed(() => {
  const totalMinutes = entries.value.reduce((total, entry) => {
    const dureeString = String(entry.duree); 
    return total + Number(dureeString.replace('min', ''));
  }, 0);
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  return `${hours}h${minutes.toString().padStart(2, '0')}`;
});
</script>