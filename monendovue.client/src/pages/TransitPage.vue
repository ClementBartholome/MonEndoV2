<template>
  <div class="flex-column-container">
    <BackButton/>
    <section
        class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
      <div class="flex justify-between items-center mb-4">
        <h2 class="text-2xl flex gap-2 ml-2"><i class="material-symbols-outlined text-3xl ml-auto">gastroenterology</i>
          Suivi du transit</h2>
        <div class="form-modal">
          <Dialog>
            <DialogTrigger class="flex gap-2 items-center cursor-pointer hover:opacity-80 transition-opacity">
              <Button variant="custom">
                <span class="hide-xsm">Ajouter une entrée</span>
                <i class="material-symbols-outlined">add</i>
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle class="text-2xl">Ajouter une entrée</DialogTitle>
              </DialogHeader>
              <form class="flex flex-col gap-6" @submit="onSubmit">
                <FormField v-slot="{ componentField }" name="typeEvenement">
                  <FormItem>
                    <FormLabel>Type de trouble</FormLabel>
                    <FormControl>
                      <Select v-model="entry.typeEvenement" v-bind="componentField">
                        <SelectTrigger>
                          <SelectValue v-bind="componentField">
                            {{ entry.typeEvenement || 'Sélectionnez un type de trouble' }}
                          </SelectValue>
                        </SelectTrigger>
                        <SelectContent>
                          <SelectGroup label="Type de trouble">
                            <SelectItem value="Diarrhée">Diarrhée</SelectItem>
                            <SelectItem value="Constipation">Constipation</SelectItem>
                            <SelectItem value="Crampes">Crampes</SelectItem>
                            <SelectItem value="Ballonnements">Ballonnements</SelectItem>
                            <SelectItem value="Nausée">Nausée</SelectItem>
                            <SelectItem value="Autre">Autre</SelectItem>
                          </SelectGroup>
                        </SelectContent>
                      </Select>
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                </FormField>
                <div class="flex items-center justify-between">
                  <FormField name="date" v-slot="{ componentField }">
                    <FormItem>
                      <FormLabel>Date</FormLabel>
                      <FormControl>
                        <Input type="date" v-model="entry.date" v-bind="componentField" class="min-w-28"/>
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  </FormField>
                  <FormField v-slot="{ componentField }" name="time">
                    <FormItem>
                      <FormLabel>Heure</FormLabel>
                      <FormControl>
                        <Input type="time" v-model="entry.time" v-bind="componentField" class="min-w-28"/>
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  </FormField>
                </div>
                <FormField name="intensite" v-slot="{ componentField }">
                  <FormItem>
                    <FormLabel>Intensité</FormLabel>
                    <FormControl>
                      <Select v-model="entry.intensite" v-bind="componentField">
                        <SelectTrigger>
                          <SelectValue v-bind="componentField">
                            {{ entry.intensite || 'Sélectionnez un niveau d\'intensité' }}
                          </SelectValue>
                        </SelectTrigger>
                        <SelectContent>
                          <SelectGroup label="Intensité">
                            <SelectItem value="Légère">Légère</SelectItem>
                            <SelectItem value="Modérée">Modérée</SelectItem>
                            <SelectItem value="Sévère">Sévère</SelectItem>
                          </SelectGroup>
                        </SelectContent>
                      </Select>
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                </FormField>
                <div class="flex items-center justify-between">
                  <FormField v-slot="{ componentField }" name="saignement">
                    <FormItem>
                      <FormControl>
                        <FormLabel>Saignement</FormLabel>
                        <div class="flex items-center">
                          <input type="radio" v-bind="componentField" v-model="entry.saignement"
                                 :value="true"/>
                          <label class="mx-2">Oui</label>
                          <input type="radio" v-bind="componentField" v-model="entry.saignement"
                                 :value="false"/>
                          <label class="ml-2">Non</label>
                        </div>
                      </FormControl>
                      <FormMessage/>
                    </FormItem>
                  </FormField>
                  <FormField v-slot="{ componentField }" name="douleurs">
                    <FormItem>
                      <FormControl>
                        <FormLabel>Douleurs</FormLabel>
                        <div class="flex items-center">
                          <input type="radio" v-bind="componentField" v-model="entry.douleurs"
                                 :value="true"/>
                          <label class="mx-2">Oui</label>
                          <input type="radio" v-bind="componentField" v-model="entry.douleurs"
                                 :value="false"/>
                          <label class="ml-2">Non</label>
                        </div>
                      </FormControl>
                      <FormMessage/>
                    </FormItem>
                  </FormField>
                </div>
                <FormField v-slot="{ componentField }" name="commentaire">
                  <FormItem>
                    <FormLabel>Un commentaire ? (optionnel)</FormLabel>
                    <FormControl>
                      <Input type="text" placeholder="Écrivez ici" v-bind="componentField" v-model="entry.commentaire"/>
                    </FormControl>
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
      <Datatable v-else-if="entries.length > 0" :entries="entries" :columns="columns" :deleteFunction="deleteDonneesTransit">
        <thead>
        <tr>
          <th>Type</th>
          <th>Date</th>
          <th>Heure</th>
          <th>Intensité</th>
          <th>Saignement</th>
          <th>Douleurs</th>
          <th>Commentaire</th>
          <th></th>
        </tr>
        </thead>
      </Datatable>
      <div v-else class="flex justify-center items-center h-32">
        <p class="text-2xl text-center">Aucune donnée enregistrée</p>
      </div>
    </section>
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
import {Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue,} from '@/components/ui/select'
import Datatable from "@/components/Datatable.vue";

import apiService from "@/services/apiService";

import {useAuthStore} from '@/store/auth';
import {format} from 'date-fns';
import {Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger,} from '@/components/ui/dialog'
import {useToast} from '@/components/ui/toast'
import {Skeleton} from "@/components/ui/skeleton";
import BackButton from "@/components/BackButton.vue";
import SelectMonth from "@/components/SelectMonth.vue";

const {toast} = useToast();

const authStore = useAuthStore();

const columns: any = [
  {data: 'typeEvenement'},
  {data: 'date'},
  {data: 'time'},
  {data: 'intensite'},
  {data: 'saignement'},
  {data: 'douleurs'},
  {data: 'commentaire'},
  {
    data: null,
    defaultContent: '<span class="material-symbols-outlined delete-btn">delete</span>'
  },
];

const deleteDonneesTransit = async (id: number) => {
  try {
    await apiService.deleteDonneesTransit(id);
    toast({
      title: 'Succès',
      description: 'Donnée supprimée avec succès.',
      variant: 'custom',
    });
  } catch (error) {
    toast({
      title: 'Erreur',
      description: 'Une erreur est survenue lors de la suppression de la donnée.',
    });
    console.error(error);
  }
};

type Entry = Record<string, any>;
const entries = ref<Entry[]>([]);
const entry = ref<Entry>({
  typeEvenement: '',
  date: '',
  time: '',
  intensite: '',
  saignement: false,
  douleurs: false,
  commentaire: ''
});

const isLoading = ref(true);

const selectedMonthYear = ref(format(new Date(), 'yyyy-MM'));

const fetchTransitEntriesByMonth = async (month: number, year: number) => {
  isLoading.value = true;
  try {
    const response = await apiService.getDonneesTransitByMonth(authStore.user!.carnetSanteId, month, year);
    entries.value = response.map((entry: Entry) => {
      const date = new Date(entry.date);
      const hours = date.getHours().toString().padStart(2, '0');
      const minutes = date.getMinutes().toString().padStart(2, '0');
      const time = `${hours}h${minutes}`;
      return {
        ...entry,
        date: format(date, 'dd-MM-yyyy').replace(/-/g, '/'),
        time,
        douleurs: entry.douleur ? 'Oui' : 'Non',
        saignement: entry.saignement ? 'Oui' : 'Non',
        commentaire: entry.commentaires ? entry.commentaires : 'Pas de commentaire'
      };
    });
  } catch (error) {
    toast({
      title: 'Erreur',
      description: 'Une erreur est survenue lors de la récupération des données de transit.',
    });
    console.error(error);
  } finally {
    isLoading.value = false;
  }
}

onMounted(async () => {
  const [year, month] = selectedMonthYear.value.split('-').map(Number);
  await fetchTransitEntriesByMonth(month, year);
});

watch(selectedMonthYear, (newValue) => {
  const [year, month] = newValue.split('-').map(Number);
  fetchTransitEntriesByMonth(month, year);
});

const formSchema = toTypedSchema(z.object({
  typeEvenement: z.string({
    required_error: 'Le type de trouble est requis'
  }),
  date: z.string({
    required_error: 'La date est requise'
  }),
  time: z.string({
    required_error: 'L\'heure est requise'
  }),
  intensite: z.string({
    required_error: 'L\'intensité est requise'
  }),
  saignement: z.boolean().optional(),
  douleurs: z.boolean().optional(),
  commentaire: z.string().optional()
}));

const form = useForm({
  validationSchema: formSchema,
  initialValues: {
    intensite: "Sélectionnez un niveau d'intensité",
    typeEvenement: "Sélectionnez un type de trouble",
  }
});

const onSubmit = form.handleSubmit((values) => {
  const dateTimeString = `${values.date}T${values.time}`;
  const dateTime = new Date(dateTimeString);
  const timezoneOffset = dateTime.getTimezoneOffset() * 60 * 1000;
  const utcDateTime = new Date(dateTime.getTime() - timezoneOffset);
  
  const valuesWithCarnetSanteId = {
    ...values,
    typeEvenement: entry.value.typeEvenement,
    date: utcDateTime,
    carnetSanteId: authStore.user?.carnetSanteId,
    commentaire: entry.value.commentaire ? entry.value.commentaire : 'Pas de commentaire'
  };
  
  apiService.postDonneesTransit(valuesWithCarnetSanteId)
      .then((response) => {
        toast({
          title: 'Succès',
          description: 'Les données de transit ont été ajoutées avec succès.',
          variant: 'custom',
        });
        
        const valuesForView = {
          ...valuesWithCarnetSanteId,
          id: response.id, 
          date: format(values.date, 'dd/MM/yyyy'),
          time: values.time.replace(":", "h"),
          douleurs: values.douleurs ? 'Oui' : 'Non',
          saignement: values.saignement ? 'Oui' : 'Non',
          commentaire: values.commentaire ? values.commentaire : 'Pas de commentaire'
        };
        delete valuesForView.carnetSanteId;
        entries.value.push(valuesForView);
      })
      .catch((error) => {
        toast({
          title: 'Erreur',
          description: 'Une erreur est survenue lors de l\'ajout des données de transit.',
        });
        console.error(error);
      });
});
</script>
