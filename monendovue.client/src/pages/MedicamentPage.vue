<template>
  <div class="flex-column-container">
    <BackButton/>
    <section class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
      <div class="flex justify-between items-center mb-4">
        <h2 class="text-2xl flex gap-2 ml-2"><i class="material-symbols-outlined text-3xl">medication</i>
          Prises de médicaments</h2>
        <div class="form-modal">
          <Dialog>
            <DialogTrigger class="flex gap-2 items-center cursor-pointer hover:opacity-80 transition-opacity">
              <Button variant="custom">
                <span class="hide-xsm">Ajouter une prise</span>
                <i class="material-symbols-outlined">add</i>
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle class="text-2xl">Ajouter une prise</DialogTitle>
              </DialogHeader>
              <form class="flex flex-col gap-6" @submit.prevent="onSubmitPriseForm">
                <FormField v-slot="{ componentField }" name="nom">
                  <FormItem>
                    <FormLabel>Médicament</FormLabel>
                    <FormControl>
                      <Select v-model="prise.medicamentId">
                        <SelectTrigger>
                          <SelectValue/>
                        </SelectTrigger>
                        <SelectContent>
                          <SelectGroup v-if="medicaments.length > 0" label="Médicaments">
                            <SelectItem v-for="medicament in medicaments" :key="medicament.id" :value="medicament.id">
                              {{ medicament.nom }}
                            </SelectItem>
                          </SelectGroup>
                          <p v-else class="px-4 py-2">Pas de médicaments enregistrés</p>
                        </SelectContent>
                      </Select>
                    </FormControl>
                    <FormMessage/>
                  </FormItem>
                </FormField>
                <FormField v-slot="{ componentField }" name="nombreComprimes">
                  <FormItem>
                    <FormLabel>Nombre de comprimés</FormLabel>
                    <FormControl>
                      <Input type="number" v-bind="componentField" v-model="prise.nombreComprimes" min="1"/>
                    </FormControl>
                    <FormMessage/>
                  </FormItem>
                </FormField>
                <div class="flex items-center gap-8">
                  <FormField v-slot="{ componentField }" name="date">
                    <FormItem>
                      <FormLabel>Date</FormLabel>
                      <FormControl>
                        <Input type="date" v-bind="componentField" v-model="prise.date" class="min-w-28"/>
                      </FormControl>
                      <FormMessage/>
                    </FormItem>
                  </FormField>
                  <FormField v-slot="{ componentField }" name="time">
                    <FormItem>
                      <FormLabel>Heure</FormLabel>
                      <FormControl>
                        <Input type="time" v-bind="componentField" v-model="prise.time" class="min-w-28"/>
                      </FormControl>
                      <FormMessage/>
                    </FormItem>
                  </FormField>
                </div>
                <FormField v-slot="{ componentField }" name="commentaire">
                  <FormItem>
                    <FormLabel>Un commentaire ? (optionnel)</FormLabel>
                    <FormControl>
                      <Input type="text" placeholder="Écrivez ici" v-bind="componentField" v-model="prise.commentaire"/>
                    </FormControl>
                    <FormMessage/>
                  </FormItem>
                </FormField>
                <Button class="mt-4" variant="custom" type="submit">
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
      <Datatable v-else-if="listePrises.length > 0" :entries="listePrises" :columns="columns"
                 :deleteFunction="deleteDonneesMedicament">
      </Datatable>
      <div v-else class="flex justify-center items-center h-32">
        <p class="text-2xl text-center">Aucune donnée enregistrée</p>
      </div>
    </section>
    <div class="flex-row-container w-full gap-8 mb-16">
      <section class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
        <div class="flex gap-4 items-center justify-between mb-4">
          <h2 class="text-2xl flex gap-2">
            <i class="material-symbols-outlined text-3xl">pill</i>
            Traitements en cours
          </h2>
          <Dialog>
            <DialogTrigger class="flex items-center ml-auto gap-2">
              <Button variant="custom">
                <span class="material-symbols-outlined">add</span>
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle class="text-2xl">Ajouter un traitement</DialogTitle>
              </DialogHeader>
              <form class="flex flex-col gap-4" @submit.prevent="onSubmitTraitementForm">
                <FormField v-slot="{ componentField }" name="nom">
                  <FormItem>
                    <FormLabel>Médicament</FormLabel>
                    <FormControl>
                      <Input v-bind="componentField" v-model="traitement.nom"/>
                    </FormControl>
                    <FormMessage/>
                  </FormItem>
                </FormField>
                <FormField v-slot="{ componentField }" name="posologie">
                  <FormItem>
                    <FormLabel>Posologie</FormLabel>
                    <FormControl>
                      <Input v-bind="componentField" v-model="traitement.posologie"/>
                    </FormControl>
                    <FormMessage/>
                  </FormItem>
                </FormField>
                <FormField v-slot="{ componentField }" name="dateDebutTraitement">
                  <FormItem>
                    <FormLabel>Date de début du traitement</FormLabel>
                    <FormControl>
                      <Input type="date" v-bind="componentField" v-model="traitement.dateDebutTraitement" class="min-w-28"/>
                    </FormControl>
                    <FormMessage/>
                  </FormItem>
                </FormField>
                <Button class="mt-4" variant="custom" type="submit">
                  Enregistrer
                </Button>
              </form>
            </DialogContent>
          </Dialog>
        </div>
        <ul class="w-full" v-if="traitementsEnCours.length > 0">
          <li v-for="traitement in traitementsEnCours" :key="traitement.id" class="relative">
            <Dialog>
              <DialogTrigger @click="editTraitement(traitement.id)" class="absolute top-0 right-0 cursor-pointer">
                <span class="material-symbols-outlined edit-btn">edit</span>
              </DialogTrigger>
              <DialogContent>
                <DialogHeader>
                  <DialogTitle class="text-2xl">Modifier le traitement</DialogTitle>
                </DialogHeader>
                <form @submit.prevent="onSubmitEditTraitement" class="flex flex-col gap-4">
                  <FormField v-slot="{ componentField }" name="nom">
                    <FormItem>
                      <FormLabel>Médicament</FormLabel>
                      <FormControl>
                        <Input v-bind="componentField" v-model="traitementForm.nom"/>
                      </FormControl>
                      <FormMessage/>
                    </FormItem>
                  </FormField>
                  <FormField v-slot="{ componentField }" name="posologie">
                    <FormItem>
                      <FormLabel>Posologie</FormLabel>
                      <FormControl>
                        <Input v-bind="componentField" v-model="traitementForm.posologie"/>
                      </FormControl>
                      <FormMessage/>
                    </FormItem>
                  </FormField>
                  <FormField v-slot="{ componentField }" name="dateDebutTraitement">
                    <FormItem>
                      <FormLabel>Date de début du traitement</FormLabel>
                      <FormControl>
                        <Input type="date" v-bind="componentField" v-model="traitementForm.dateDebutTraitement" class="min-w-28"/>
                      </FormControl>
                      <FormMessage/>
                    </FormItem>
                  </FormField>
                  <FormField v-slot="{ componentField }" name="traitementEnCours">
                    <FormItem>
                      <FormControl>
                        <FormLabel>Traitement en cours</FormLabel>
                        <div class="flex items-center">
                          <input type="radio" v-bind="componentField" v-model="traitementForm.traitementEnCours"
                                 value="true"/>
                          <label class="mx-2">Oui</label>
                          <input type="radio" v-bind="componentField" v-model="traitementForm.traitementEnCours"
                                 value="false"/>
                          <label class="ml-2">Non</label>
                        </div>
                      </FormControl>
                      <FormMessage/>
                    </FormItem>
                  </FormField>
                  <FormField v-if="traitementForm.traitementEnCours === 'false'" v-slot="{ componentField }"
                             name="dateFinTraitement">
                    <FormItem>
                      <FormControl>
                        <FormLabel>Date de fin du traitement</FormLabel>
                        <Input type="date" v-bind="componentField" v-model="traitementForm.dateFinTraitement" class="min-w-28"/>
                      </FormControl>
                      <FormMessage/>
                    </FormItem>
                  </FormField>
                  <Button class="mt-4" variant="custom" type="submit">
                    Enregistrer
                  </Button>
                </form>
              </DialogContent>
            </Dialog>
            <p class="text-headline font-bold text-xl">{{ traitement.nom }}</p>
            <p>{{ traitement.posologie }}</p>
            <p>Depuis le {{ format(traitement.dateDebutTraitement, 'dd-MM-yyyy').replace(/-/g, '/') }}</p>
          </li>
        </ul>
        <p v-else class="text-2xl text-center">Pas de traitements en cours</p>
      </section>
      <section class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
        <div class="flex gap-4 items-center justify-between mb-4">
          <h2 class="text-2xl flex gap-2">
            <i class="material-symbols-outlined text-3xl">pill_off</i>
            Traitements passés
          </h2>
        </div>
        <ul class="w-full">
          <li v-for="traitement in traitementsPasses" :key="traitement.id" class="relative">
            <div>
              <p class="text-headline font-bold text-xl">{{ traitement.nom }}</p>
              <p>{{ traitement.posologie }}</p>
                            <p>Traitement pris du {{ format(traitement.dateDebutTraitement, 'dd-MM-yyyy').replace(/-/g, '/') }} au
                              {{ format(traitement.dateFinTraitement, 'dd-MM-yyyy').replace(/-/g, '/') }}</p>
            </div>
          </li>
        </ul>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import {onMounted, ref, watch} from 'vue'


import {Button} from '@/components/ui/button'
import {FormControl, FormField, FormItem, FormLabel, FormMessage,} from '@/components/ui/form'
import {Input} from '@/components/ui/input'
import {Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue,} from '@/components/ui/select'

import Datatable from "@/components/Datatable.vue";

import apiService from "@/services/apiService";

import {useAuthStore} from '@/store/auth';
import {format, parseISO} from 'date-fns';
import {Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger,} from '@/components/ui/dialog'
import {useToast} from '@/components/ui/toast'
import {Skeleton} from "@/components/ui/skeleton";
import BackButton from "@/components/BackButton.vue";
import SelectMonth from "@/components/SelectMonth.vue";

const {toast} = useToast();
const authStore = useAuthStore();
const traitementsEnCours = ref<Medicament[]>([]);
const medicaments = ref<Medicament[]>([]);
const traitementsPasses = ref<Medicament[]>([]);
const listePrises = ref<Entry[]>([]);
const prise = ref<PriseMedicament>({nom: '', date: '', time: '', commentaire: '', medicamentId: '', nombreComprimes: 1});
const traitement = ref({nom: '', posologie: '', dateDebutTraitement: ''});
const isLoading = ref(true);

type Entry = Record<string, any>;

const traitementForm: any = ref({
  id: '',
  nom: '',
  posologie: '',
  dateDebutTraitement: '',
  dateFinTraitement: '',
  traitementEnCours: '',
});

const columns: any = [
  {data: 'nom', title: 'Médicament'},
  {data: 'nombreComprimes', title: 'Comprimés'},
  {data: 'date', title: 'Date'},
  {data: 'time', title: 'Heure'},
  {data: 'commentaire', title: 'Commentaire'},
  {
    data: null,
    defaultContent: '<span class="material-symbols-outlined delete-btn">delete</span>'
  },
];

const deleteDonneesMedicament = async (id: number) => {
  try {
    await apiService.deleteDonneesMedicament(id);
    toast({
      title: 'Succès',
      description: 'La prise de médicament a été supprimée avec succès.',
      variant: 'custom',
    });
  } catch (error) {
    toast({
      title: 'Erreur',
      description: 'Une erreur est survenue lors de la suppression de la prise de médicament.',
    });
    console.error(error);
  }
};
interface Medicament {
  id: string;
  nom: string;
  posologie: string;
  dateDebutTraitement: any;
  dateFinTraitement?: any;
  traitementEnCours: boolean;
}

interface PriseMedicament {
  nom: string;
  date: string;
  time: string;
  commentaire: string;
  medicamentId: string;
  nombreComprimes: number;
}

const selectedMonthYear = ref(format(new Date(), 'yyyy-MM'));

const fetchAllMedicaments = async () => {
  isLoading.value = true;
  try {
    const response = await apiService.getAllMedicaments(authStore.user!.carnetSanteId);
    medicaments.value = response.$values.map((med: Medicament) => ({
      id: med.id,
      nom: med.nom,
      posologie: med.posologie,
    }));
    traitementsEnCours.value = response.$values.filter(med => med.traitementEnCours);
    traitementsPasses.value = response.$values.filter(med => med.traitementEnCours == false);
  } catch (error) {
    console.error(error);
  } finally {
    isLoading.value = false;
  }
}
const fetchDonneesMedicamentsByMonth = async (month: number, year: number) => {
  isLoading.value = true;
  try {
    const response = await apiService.getDonneesMedicamentByMonth(authStore.user!.carnetSanteId, month, year);
    listePrises.value = response.map((d: Entry) => {
      const date = new Date(d.date);
      const hours = date.getHours().toString().padStart(2, '0');
      const minutes = date.getMinutes().toString().padStart(2, '0');
      const time = `${hours}h${minutes}`;
      return {
        id: d.id,
        nom: d.nomMedicament,
        nombreComprimes: d.nombreComprimes,
        date: format(date, 'dd-MM-yyyy').replace(/-/g, '/'),
        time: time,
        commentaire: d.commentaire ? d.commentaire : 'Pas de détails',
      };
    });
  } catch (error) {
    console.error(error);
  } finally {
    isLoading.value = false;
  }
}

onMounted(() => {
  fetchAllMedicaments();
  const [year, month] = selectedMonthYear.value.split('-').map(Number);
  fetchDonneesMedicamentsByMonth(month, year);
});

watch(selectedMonthYear, (newValue) => {
  const [year, month] = newValue.split('-').map(Number);
  fetchDonneesMedicamentsByMonth(month, year);
});

const onSubmitPriseForm = () => {
  const values = prise.value;
  const {time, ...valuesForApi} = prise.value;
  const dateTimeString = `${valuesForApi.date}T${time}`;
  const dateTime = new Date(dateTimeString);
  const timezoneOffset = dateTime.getTimezoneOffset() * 60 * 1000;
  const utcDateTime = new Date(dateTime.getTime() - timezoneOffset);

  const valuesWithCarnetSanteId = {
    ...valuesForApi,
    date: utcDateTime,
    commentaire: valuesForApi.commentaire ? valuesForApi.commentaire : 'Pas de commentaire',
    carnetSanteId: authStore.user?.carnetSanteId,
  };

  apiService.postDonneesPriseMedicament(valuesWithCarnetSanteId).then((response) => {
    toast({
      title: 'Succès',
      description: 'La prise de médicament a été enregistrée avec succès.',
      variant: 'custom',
    });
    
    const medicamentName = medicaments.value.find(med => med.id === values.medicamentId)?.nom;

    const valuesForView = {
      ...values,
      id: response.id,
      nom: medicamentName,
      nombreComprimes: valuesForApi.nombreComprimes,
      commentaire: valuesForApi.commentaire ? valuesForApi.commentaire : 'Pas de commentaire',
      date: format(values.date, 'dd/MM/yyyy'),
      time: values.time.replace(":", "h"),
    };
    listePrises.value.push(valuesForView);
  }).catch((error) => {
    toast({
      title: 'Erreur',
      description: 'Une erreur est survenue lors de l\'enregistrement de la prise de médicament.',
    });
    console.error(error);
  });

};

const onSubmitTraitementForm = () => {
  const values = traitement.value;
  const valuesWithCarnetSanteId = {
    ...values,
    dateDebutTraitement: new Date(values.dateDebutTraitement),
    carnetSanteId: authStore.user?.carnetSanteId,
    traitementEnCours: true
  };

  apiService.postMedicament(valuesWithCarnetSanteId).then((response) => {
    toast({
      title: 'Succès',
      description: 'Le traitement a été ajouté avec succès.',
      variant: 'custom',
    });
    const medicamentAdded: any = {
      ...valuesWithCarnetSanteId,
      id: response.id
    };
    traitementsEnCours.value.push(medicamentAdded);
    medicaments.value.push(medicamentAdded);
  }).catch((error) => {
    toast({
      title: 'Erreur',
      description: 'Une erreur est survenue lors de l\'ajout du traitement.',
    });
    console.error(error);
  });
};

function editTraitement(traitementId) {

  const traitementToEdit = traitementsEnCours.value.find(t => t.id === traitementId) || medicaments.value.find(m => m.id === traitementId);

  if (traitementToEdit) {
    let formattedStartDate, formattedEndDate;

    if (typeof traitementToEdit.dateDebutTraitement === 'string') {
      formattedStartDate = format(parseISO(traitementToEdit.dateDebutTraitement), 'yyyy-MM-dd');
    } else if (traitementToEdit.dateDebutTraitement instanceof Date) {
      formattedStartDate = format(traitementToEdit.dateDebutTraitement, 'yyyy-MM-dd');
    } else {
      console.error('dateDebutTraitement is not in a valid format:', traitementToEdit.dateDebutTraitement);
    }

    if (traitementToEdit.dateFinTraitement) {
      if (typeof traitementToEdit.dateFinTraitement === 'string') {
        formattedEndDate = format(parseISO(traitementToEdit.dateFinTraitement), 'yyyy-MM-dd');
      } else if (traitementToEdit.dateFinTraitement instanceof Date) {
        formattedEndDate = format(traitementToEdit.dateFinTraitement, 'yyyy-MM-dd');
      } else {
        console.error('dateFinTraitement is not in a valid format:', traitementToEdit.dateFinTraitement);
      }
    } else {
      formattedEndDate = null;
    }

    traitementForm.value = {
      ...traitementToEdit,
      dateDebutTraitement: formattedStartDate,
      dateFinTraitement: formattedEndDate
    };
    console.log(traitementForm.value);
  } else {
    console.log(`Aucun traitement trouvé avec l'id: ${traitementId}`);
  }
}

const onSubmitEditTraitement = () => {
  const values = traitementForm.value;
  console.log(values);
  const valuesWithCarnetSanteId = {
    ...values,
    dateDebutTraitement: new Date(values.dateDebutTraitement),
    carnetSanteId: authStore.user?.carnetSanteId,
    traitementEnCours: values.traitementEnCours === true
  };
  console.log(valuesWithCarnetSanteId);
  const traitementId: any = values.id;


  apiService.putDonneesMedicament(traitementId, valuesWithCarnetSanteId).then(() => {
    toast({
      title: 'Succès',
      description: 'Le traitement a été mis à jour avec succès.',
      variant: 'custom',
    });
    const indexEnCours = traitementsEnCours.value.findIndex(traitement => traitement.id === traitementId);
    const indexPasses = traitementsPasses.value.findIndex(traitement => traitement.id === traitementId);

    if (valuesWithCarnetSanteId.traitementEnCours) {
      if (indexEnCours !== -1) {
        traitementsEnCours.value[indexEnCours] = valuesWithCarnetSanteId;
      } else {
        traitementsEnCours.value.push(valuesWithCarnetSanteId);
      }
      if (indexPasses !== -1) {
        traitementsPasses.value.splice(indexPasses, 1);
      }
    } else {
      if (indexEnCours !== -1) {
        traitementsEnCours.value.splice(indexEnCours, 1);
      }
      if (indexPasses === -1) {
        traitementsPasses.value.push(valuesWithCarnetSanteId);
      }
    }
  }).catch((error) => {
    toast({
      title: 'Erreur',
      description: 'Une erreur est survenue lors de la mise à jour du traitement.',
    });
    console.error(error);
  });
};
</script>