<template>
  <div class="flex-column-container">
    <BackButton/>
    <section
        class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
      <div class="flex justify-between items-center mb-4">
        <h2 class="text-2xl flex gap-2 ml-2"><i class="material-symbols-outlined text-3xl ml-auto">sick</i>
          Suivi des douleurs</h2>
        <div class="form-modal">
          <Dialog v-model:open="isDialogOpen">
            <DialogTrigger class="flex gap-2 items-center cursor-pointer hover:opacity-80 transition-opacity">
              <Button variant="custom" @click="isEditMode = false">
                <span class="hide-xsm">Ajouter une douleur</span>
                <i class="material-symbols-outlined">add</i>
              </Button>
            </DialogTrigger>
            <DialogContent >
              <DialogHeader>
                <DialogTitle class="text-2xl">{{ isEditMode ? 'Modifier la douleur' : 'Ajouter une douleur' }}</DialogTitle>
              </DialogHeader>
              <form class="flex flex-col gap-6" @submit="onSubmit">
                <FormField v-slot="{ componentField }" name="type">
                  <FormItem>
                    <FormLabel>Type</FormLabel>
                    <FormControl>
                      <Select v-model="form.values.type" v-bind="componentField">
                        <SelectTrigger>
                          <SelectValue v-bind="componentField">
                            {{ form.values.type || 'Sélectionner un type de douleur' }}
                          </SelectValue>
                        </SelectTrigger>
                        <SelectContent>
                          <SelectGroup label="Type de douleur">
                            <SelectItem value="Douleur pelvienne">Douleur pelvienne</SelectItem>
                            <SelectItem value="Douleur abdominale">Douleur abdominale</SelectItem>
                            <SelectItem value="Douleur lombaire">Douleur lombaire</SelectItem>
                            <SelectItem value="Douleur thoracique">Douleur thoracique</SelectItem>
                            <SelectItem value="Douleur projetée">Douleur projetée</SelectItem>
                            <SelectItem value="Douleur neuropathique">Douleur neuropathique</SelectItem>
                            <SelectItem value="Dyspareunie">Dyspareunie</SelectItem>
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
                        <Input type="date" v-bind="componentField" v-model="form.values.date"/>
                      </FormControl>
                      <FormMessage/>
                    </FormItem>
                  </FormField>
                  <FormField v-slot="{ componentField }" name="time">
                    <FormItem>
                      <FormLabel>Heure</FormLabel>
                      <FormControl>
                        <Input type="time" v-bind="componentField" v-model="form.values.time"/>
                      </FormControl>
                      <FormMessage/>
                    </FormItem>
                  </FormField>
                </div>
                <FormField v-slot="{ componentField }" name="intensite">
                  <FormItem>
                    <FormLabel>Intensité</FormLabel>
                    <FormControl>
                      <Slider v-bind="componentField" v-model="form.values.intensite" :default-value="[5]" :max="10" :min="1" :step="1"/>
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
                <DialogFooter>
                  <Button class="mt-4" variant="custom" type="submit" @click="onSubmit">
                    Enregistrer
                  </Button>
                </DialogFooter>
              </form>
            </DialogContent>
          </Dialog>
        </div>
      </div>
      <SelectMonth v-model="selectedMonthYear"/>

      <div v-if="isLoading" class="flex flex-col space-y-3">
        <Skeleton class="h-[300px] w-full mt-4 rounded-xl"/>
      </div>
      <Datatable v-else-if="filteredEntries.length > 0" :entries="filteredEntries" :columns="columns"
                 :deleteFunction="deleteDonneesDouleurs" @edit-entry="handleEditEntry">
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
            <Skeleton class="h-[400px] w-full rounded-xl"/>
          </div>
          <p v-else-if="entries.length === 0" class="mt-8 text-2xl text-center">Aucune donnée enregistrée</p>
          <LineChart
              v-else
              :data="entries"
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
        <p>Moyenne d'intensité des douleurs ({{ filteredEntries.length }}
          {{ filteredEntries.length > 1 ? 'entrées' : 'entrée' }})</p>
        <span class="text-5xl text-highlight">{{ averageIntensity }}</span>
        <i class="material-symbols-outlined text-7xl text-button">{{ intensityIcon }}</i>
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
import {Slider} from '@/components/ui/slider'
import {
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import {Input} from '@/components/ui/input'
import {LineChart} from "@/components/ui/chart-line";
import {Skeleton} from '@/components/ui/skeleton'

import apiService from "@/services/apiService";

import {useAuthStore} from '@/store/auth';
import {format, eachDayOfInterval, startOfMonth, endOfMonth} from 'date-fns';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import Datatable from "@/components/Datatable.vue";
import {useToast} from '@/components/ui/toast';
import {Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue,} from '@/components/ui/select'
import BackButton from "@/components/BackButton.vue";
import SelectMonth from "@/components/SelectMonth.vue";

const {toast} = useToast();
const authStore = useAuthStore();

type Entry = Record<string, any>;

const entries = ref<Entry[]>([]);
// const entry = ref<Entry>({
//   type: '',
//   date: format(new Date(), 'yyyy-MM-dd'),
//   time: format(new Date(), 'HH:mm'),
//   intensite: 0,
//   commentaire: '',
// });

const isLoading = ref(true);
const isEditMode = ref(false);
const isDialogOpen = ref(false);

const columns: any = [
  {data: 'type'},
  {data: 'date'},
  {data: 'time'},
  {data: 'intensite'},
  {data: 'commentaire'},
  {
    data: 'actions',
    defaultContent: '<span class="material-symbols-outlined edit-btn">edit</span>'
  },
  {
    data: null,
    defaultContent: '<span class="material-symbols-outlined delete-btn">delete</span>'
  },
];

const selectedMonthYear = ref(format(new Date(), 'yyyy-MM'));

const deleteDonneesDouleurs = async (id: number) => {
  try {
    await apiService.deleteDonneesDouleurs(id);
    toast({
      title: 'Succès',
      description: 'La douleur a été supprimée avec succès.',
      variant: 'custom',
    });
  } catch (error) {
    toast({
      title: 'Erreur',
      description: 'Un problème est survenu lors de la suppression de la douleur.',
    });
    console.error(error);
  }
};

const handleEditEntry = (id: number) => {
  const entry = entries.value.find(entry => entry.id === id);
  if (entry) {
    openEditForm(entry);
  } else {
    console.error(`Entry with id ${id} not found`);
  }
};

const openEditForm = (entry: Entry) => {
  const [day, month, year] = entry.date.split('/').map(Number);
  const parsedDate = new Date(year, month - 1, day);
  form.setValues({
    id: entry.id,
    type: entry.type,
    date: new Intl.DateTimeFormat('fr-CA', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    }).format(parsedDate),
    time: entry.time.replace('h', ':'),
    intensite: [entry.intensite],
    commentaire: entry.commentaire,
  });

  isEditMode.value = true;
  isDialogOpen.value = true;
};

const fetchPainEntriesByMonth = async (month: number, year: number) => {
  isLoading.value = true;
  try {
    const response = await apiService.getDonneesDouleursByMonth(authStore.user!.carnetSanteId, month, year);
    const entriesMap = new Map<string, Entry>();

    response.forEach((entry: Entry) => {
      const date = new Date(entry.date);
      const formattedDate = format(date, 'dd-MM-yyyy').replace(/-/g, '/');
      const hours = date.getHours().toString().padStart(2, '0');
      const minutes = date.getMinutes().toString().padStart(2, '0');
      const time = `${hours}h${minutes}`;
      entriesMap.set(formattedDate, {
        id: entry.id,
        type: entry.typeDouleur,
        date: formattedDate,
        time: time,
        intensite: entry.intensite,
        commentaire: entry.commentaire,
      });
    });

    const startDate = startOfMonth(new Date(year, month - 1));
    const endDate = endOfMonth(startDate);
    const allDays = eachDayOfInterval({start: startDate, end: endDate});

    entries.value = allDays.flatMap(day => {
      const formattedDate = format(day, 'dd-MM-yyyy').replace(/-/g, '/');
      const dayEntries = response.filter(entry => {
        const entryDate = format(new Date(entry.date), 'dd-MM-yyyy').replace(/-/g, '/');
        return entryDate === formattedDate;
      });
    
      if (dayEntries.length > 0) {
        return dayEntries.map(entry => ({
          id: entry.id,
          type: entry.typeDouleur,
          date: formattedDate,
          time: format(new Date(entry.date), 'HH:mm').replace(':', 'h'),
          intensite: entry.intensite,
          commentaire: entry.commentaire,
        }));
      } else {
        return [{
        type: 'Aucune douleur',
        date: formattedDate,
        time: '00h00',
        intensite: 0,
        commentaire: '',
        }];
      }
    });
    
  } catch (error) {
    console.error('Error fetching pain entries by month:', error);
  } finally {
    isLoading.value = false;
  }
};

onMounted(() => {
  const [year, month] = selectedMonthYear.value.split('-').map(Number);
  fetchPainEntriesByMonth(month, year);
});

watch(selectedMonthYear, (newValue) => {
  const [year, month] = newValue.split('-').map(Number);
  fetchPainEntriesByMonth(month, year);
});

const filteredEntries = computed(() => {
  return entries.value.filter(entry => entry.intensite !== 0);
});

const formSchema = toTypedSchema(z.object({
  id: z.number().nullable(),
  type: z.string({
    required_error: 'Le type de douleur est requis',
  }),
  date: z.string({
    required_error: 'La date est requise',
  }),
  time: z.string({
    required_error: 'L\'heure est requise',
  }),
  intensite: z.number({
    required_error: 'L\'intensité est requise',
  }).array(),
  commentaire: z.string().optional(),
}));

const form = useForm({
  validationSchema: formSchema,
  initialValues: {
    id: 0,
    intensite: [5],
    date: format(new Date(), 'yyyy-MM-dd'),
    time: format(new Date(), 'HH:mm'),
  }
});

const onSubmit = form.handleSubmit((values) => {
  const dateTimeString = `${values.date}T${values.time}`;
  const dateTime = new Date(dateTimeString);
  const timezoneOffset = dateTime.getTimezoneOffset() * 60 * 1000;
  const utcDateTime = new Date(dateTime.getTime() - timezoneOffset);

  const valuesWithCarnetSanteId = {
    ...values,
    typeDouleur: values.type.trim(),
    intensite: values.intensite[0],
    date: utcDateTime,
    commentaire: values.commentaire ? values.commentaire : 'Pas de commentaire',
    carnetSanteId: authStore.user?.carnetSanteId,
  };

  if (isEditMode.value && values.id !== null) {
    const entryIndex = entries.value.findIndex(entry => entry.id === values.id);
    if (entryIndex !== -1) {
      apiService.editDonneesDouleurs(values.id, valuesWithCarnetSanteId).then(() => {
        toast({
          title: 'Succès',
          description: 'La douleur a été modifiée avec succès.',
          variant: 'custom',
        });
        entries.value[entryIndex] = {
          ...valuesWithCarnetSanteId,
          date: format(values.date, 'dd/MM/yyyy'),
          time: values.time.replace(":", "h"),
        };
      }).catch((error) => {
        toast({
          title: 'Erreur',
          description: 'Un problème est survenu lors de la modification de la douleur.',
        });
        console.error(error);
      });
      return;
    }
  } else {
    apiService.postDonneesDouleurs(valuesWithCarnetSanteId).then((response) => {
      toast({
        title: 'Succès',
        description: 'La douleur a été ajoutée avec succès.',
        variant: 'custom',
      });
      const valuesForView = {
        ...valuesWithCarnetSanteId,
        id: response.id,
        date: format(values.date, 'dd/MM/yyyy'),
        time: values.time.replace(":", "h"),
      };
      delete valuesForView.carnetSanteId;
      entries.value.push(valuesForView);
    }).catch((error) => {
      toast({
        title: 'Erreur',
        description: 'Un problème est survenu lors de l\'ajout de la douleur.',
      });
      console.error(error);
    });
  }
});

const averageIntensity = computed(() => {
  if (filteredEntries.value.length === 0) {
    return 'N/A';
  }
  const totalIntensity = filteredEntries.value.reduce((total, entry) => total + Number(entry.intensite), 0);
  return (totalIntensity / filteredEntries.value.length).toFixed(2);
});

const intensityIcon = computed(() => {
  const avgIntensity = Number(averageIntensity.value);
  if (avgIntensity >= 0 && avgIntensity < 2) {
    return 'sentiment_very_satisfied';
  } else if (avgIntensity >= 2 && avgIntensity < 4) {
    return 'sentiment_satisfied';
  } else if (avgIntensity >= 4 && avgIntensity < 6) {
    return 'sentiment_neutral';
  } else if (avgIntensity >= 6 && avgIntensity < 8) {
    return 'sentiment_dissatisfied';
  } else if (avgIntensity >= 8 && avgIntensity <= 10) {
    return 'sentiment_very_dissatisfied';
  } else {
    return '';
  }
});
</script>