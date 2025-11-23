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
      <SelectMonth v-model="selectedMonthYear"/>
      <div v-if="isLoading" class="flex flex-col space-y-3">
        <Skeleton class="h-[300px] w-full mt-4 rounded-xl"/>
      </div>
      <Datatable v-else-if="listePrises.length > 0" :entries="listePrises" :columns="columns"
                 :deleteFunction="handleDeletePrise">
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
                    <FormLabel>Nom du traitement</FormLabel>
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
                      <Input type="date" v-bind="componentField" v-model="traitement.dateDebutTraitement"
                             class="min-w-28"/>
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
          <li v-for="traitement in traitementsEnCours" :key="traitement.id" class="relative pr-20">
            <div class="absolute top-0 right-0 flex gap-2">
              <span @click="confirmDeleteMedicament(traitement.id)" class="material-symbols-outlined delete-btn cursor-pointer">delete</span>
              <Dialog>
                <DialogTrigger @click="editTraitement(traitement.id)" class="cursor-pointer">
                  <span class="material-symbols-outlined edit-btn">edit</span>
                </DialogTrigger>
              <DialogContent>
                <DialogHeader>
                  <DialogTitle class="text-2xl">Modifier le traitement</DialogTitle>
                </DialogHeader>
                <form @submit.prevent="onSubmitEditTraitement" class="flex flex-col gap-4">
                  <FormField v-slot="{ componentField }" name="nom">
                    <FormItem>
                      <FormLabel>Nom du traitement</FormLabel>
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
                        <Input type="date" v-bind="componentField" v-model="traitementForm.dateDebutTraitement"
                               class="min-w-28"/>
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
                        <Input type="date" v-bind="componentField" v-model="traitementForm.dateFinTraitement"
                               class="min-w-28"/>
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
            <p class="text-headline font-bold text-xl">{{ traitement.nom }}</p>
            <p>{{ traitement.posologie }}</p>
            <p>Depuis le {{ formatDateDisplay(traitement.dateDebutTraitement) }}</p>
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
          <li v-for="traitement in traitementsPasses" :key="traitement.id" class="relative pr-12">
            <div class="absolute top-0 right-0">
              <span @click="confirmDeleteMedicament(traitement.id)" class="material-symbols-outlined delete-btn cursor-pointer">delete</span>
            </div>
            <div>
              <p class="text-headline font-bold text-xl">{{ traitement.nom }}</p>
              <p>{{ traitement.posologie }}</p>
              <p>Traitement pris du {{ formatDateDisplay(traitement.dateDebutTraitement) }} au
                {{ formatDateDisplay(traitement.dateFinTraitement) }}</p>
            </div>
          </li>
        </ul>
      </section>
    </div>

    <Dialog v-model:open="showDeleteDialog">
      <DialogContent>
        <DialogHeader>
          <DialogTitle class="text-2xl">Confirmer la suppression</DialogTitle>
        </DialogHeader>
        <div class="py-4">
          <p class="text-lg">Es-tu sûr de vouloir supprimer ce traitement ?</p>
          <p class="text-sm text-gray-600 mt-2">Cette action est irréversible.</p>
        </div>
        <div class="flex justify-end gap-4">
          <Button variant="outline" @click="showDeleteDialog = false">
            Annuler
          </Button>
          <Button variant="custom" @click="handleDeleteMedicament">
            Supprimer
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { Button } from '@/shared/components/ui/button'
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/shared/components/ui/form'
import { Input } from '@/shared/components/ui/input'
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from '@/shared/components/ui/select'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/shared/components/ui/dialog'
import { Skeleton } from "@/shared/components/ui/skeleton"
import Datatable from "@/shared/components/Datatable.vue"
import BackButton from "@/shared/components/BackButton.vue"
import SelectMonth from "@/shared/components/SelectMonth.vue"

import apiService from "@/shared/services/apiService"
import { useAuthStore } from '@/features/auth/store/auth'
import { useMonthData } from '@/shared/composables/useMonthData'
import { useDateTimeFormat } from '@/shared/composables/useDateTimeFormat'
import { useCrudOperations } from '@/shared/composables/useCrudOperations'
import { useToast } from '@/shared/components/ui/toast'
import { useSync } from '@/shared/composables/useSync'
import { format, parseISO } from 'date-fns'

const authStore = useAuthStore()
const { formatDateDisplay, formatTimeDisplay, combineDateTime, getCurrentDateInput, getCurrentTimeInput } = useDateTimeFormat()
const { toast } = useToast()
const { handleOfflineOperation } = useSync()

const traitementsEnCours = ref<Medicament[]>([])
const medicaments = ref<Medicament[]>([])
const traitementsPasses = ref<Medicament[]>([])
const medicamentToDelete = ref<string | null>(null)
const showDeleteDialog = ref(false)

const { selectedMonthYear, entries: listePrises, isLoading } = useMonthData({
  fetchFunction: async (month, year) => {
    return apiService.getDonneesMedicamentByMonth(authStore.user!.carnetSanteId, month, year)
  },
  transformData: (response) => {
    return response.map((d: any) => ({
      id: d.id,
      nom: d.nomMedicament,
      nombreComprimes: d.nombreComprimes,
      date: formatDateDisplay(d.date),
      time: formatTimeDisplay(d.date),
      commentaire: d.commentaire || 'Pas de détails',
    }))
  },
  immediate: false,
  dataType: 'medicament'
})

const { deleteEntry, createEntry } = useCrudOperations(listePrises)

const prise = ref({
  nom: '',
  date: getCurrentDateInput(),
  time: getCurrentTimeInput(),
  commentaire: '',
  medicamentId: '',
  nombreComprimes: 1
})

const traitement = ref({ nom: '', posologie: '', dateDebutTraitement: '' })

const traitementForm: any = ref({
  id: '',
  nom: '',
  posologie: '',
  dateDebutTraitement: '',
  dateFinTraitement: '',
  traitementEnCours: '',
})

const columns: any = [
  { data: 'nom', title: 'Médicament' },
  { data: 'nombreComprimes', title: 'Comprimés' },
  { data: 'date', title: 'Date' },
  { data: 'time', title: 'Heure' },
  { data: 'commentaire', title: 'Commentaire' },
  {
    data: null,
    defaultContent: '<span class="material-symbols-outlined delete-btn">delete</span>'
  },
]

interface Medicament {
  id: string
  nom: string
  posologie: string
  dateDebutTraitement: any
  dateFinTraitement?: any
  traitementEnCours: boolean
}

const fetchAllMedicaments = async () => {
  try {
    const response = await apiService.getAllMedicaments(authStore.user!.carnetSanteId)
    medicaments.value = response.$values.map((med: Medicament) => ({
      id: med.id,
      nom: med.nom,
      posologie: med.posologie,
    }))
    traitementsEnCours.value = response.$values.filter(med => med.traitementEnCours)
    traitementsPasses.value = response.$values.filter(med => med.traitementEnCours == false)
  } catch (error) {
    console.error(error)
  }
}

onMounted(async () => {
  isLoading.value = true

  try {
    await fetchAllMedicaments()
    const [year, month] = selectedMonthYear.value.split('-').map(Number)
    const response = await apiService.getDonneesMedicamentByMonth(authStore.user!.carnetSanteId, month, year)
    listePrises.value = response.map((d: any) => ({
      id: d.id,
      nom: d.nomMedicament,
      nombreComprimes: d.nombreComprimes,
      date: formatDateDisplay(d.date),
      time: formatTimeDisplay(d.date),
      commentaire: d.commentaire || 'Pas de détails',
    }))
  } catch (error) {
    console.error('Error loading medicament data:', error)
  } finally {
    isLoading.value = false
  }
})

const handleDeletePrise = async (id: number) => {
  await deleteEntry(id, (id) => apiService.deleteDonneesMedicament(id as number), {
    successMessage: 'La prise de médicament a été supprimée avec succès',
    errorMessage: 'Une erreur est survenue lors de la suppression de la prise de médicament',
    endpoint: 'DonneesMedicament'
  })
}

const onSubmitPriseForm = () => {
  const values = prise.value
  const medicamentName = medicaments.value.find(med => med.id === values.medicamentId)?.nom

  createEntry(values, {
    createFunction: (data) => apiService.postDonneesPriseMedicament(data),
    formatForApi: (data) => {
      const { time, ...valuesForApi } = data
      return {
        ...valuesForApi,
        date: combineDateTime(data.date, time),
        commentaire: data.commentaire || 'Pas de commentaire',
        carnetSanteId: authStore.user?.carnetSanteId,
      }
    },
    formatForDisplay: (data, response) => ({
      id: response.id,
      nom: medicamentName,
      nombreComprimes: data.nombreComprimes,
      commentaire: data.commentaire || 'Pas de commentaire',
      date: formatDateDisplay(data.date),
      time: formatTimeDisplay(data.time),
    }),
    successMessage: 'La prise de médicament a été enregistrée avec succès',
    errorMessage: 'Une erreur est survenue lors de l\'enregistrement de la prise de médicament',
    endpoint: 'DonneesMedicament'
  })
}

const onSubmitTraitementForm = () => {
  const values = traitement.value
  const valuesWithCarnetSanteId = {
    ...values,
    dateDebutTraitement: new Date(values.dateDebutTraitement),
    carnetSanteId: authStore.user?.carnetSanteId,
    traitementEnCours: true
  }

  apiService.postMedicament(valuesWithCarnetSanteId).then((response) => {
    const medicamentAdded: any = {
      ...valuesWithCarnetSanteId,
      id: response.id
    }
    traitementsEnCours.value.push(medicamentAdded)
    medicaments.value.push(medicamentAdded)
  }).catch((error) => {
    console.error(error)
  })
}

function editTraitement(traitementId) {
  const traitementToEdit = traitementsEnCours.value.find(t => t.id === traitementId) || medicaments.value.find(m => m.id === traitementId)

  if (traitementToEdit) {
    let formattedStartDate, formattedEndDate

    if (typeof traitementToEdit.dateDebutTraitement === 'string') {
      formattedStartDate = format(parseISO(traitementToEdit.dateDebutTraitement), 'yyyy-MM-dd')
    } else if (traitementToEdit.dateDebutTraitement instanceof Date) {
      formattedStartDate = format(traitementToEdit.dateDebutTraitement, 'yyyy-MM-dd')
    } else {
      console.error('dateDebutTraitement is not in a valid format:', traitementToEdit.dateDebutTraitement)
    }

    if (traitementToEdit.dateFinTraitement) {
      if (typeof traitementToEdit.dateFinTraitement === 'string') {
        formattedEndDate = format(parseISO(traitementToEdit.dateFinTraitement), 'yyyy-MM-dd')
      } else if (traitementToEdit.dateFinTraitement instanceof Date) {
        formattedEndDate = format(traitementToEdit.dateFinTraitement, 'yyyy-MM-dd')
      } else {
        console.error('dateFinTraitement is not in a valid format:', traitementToEdit.dateFinTraitement)
      }
    } else {
      formattedEndDate = null
    }

    traitementForm.value = {
      ...traitementToEdit,
      dateDebutTraitement: formattedStartDate,
      dateFinTraitement: formattedEndDate
    }
  }
}

const onSubmitEditTraitement = () => {
  const values = traitementForm.value
  const valuesWithCarnetSanteId = {
    ...values,
    dateDebutTraitement: new Date(values.dateDebutTraitement),
    carnetSanteId: authStore.user?.carnetSanteId,
    traitementEnCours: values.traitementEnCours === true
  }
  const traitementId: any = values.id

  apiService.putDonneesMedicament(traitementId, valuesWithCarnetSanteId).then(() => {
    const indexEnCours = traitementsEnCours.value.findIndex(traitement => traitement.id === traitementId)
    const indexPasses = traitementsPasses.value.findIndex(traitement => traitement.id === traitementId)

    if (valuesWithCarnetSanteId.traitementEnCours) {
      if (indexEnCours !== -1) {
        traitementsEnCours.value[indexEnCours] = valuesWithCarnetSanteId
      } else {
        traitementsEnCours.value.push(valuesWithCarnetSanteId)
      }
      if (indexPasses !== -1) {
        traitementsPasses.value.splice(indexPasses, 1)
      }
    } else {
      if (indexEnCours !== -1) {
        traitementsEnCours.value.splice(indexEnCours, 1)
      }
      if (indexPasses === -1) {
        traitementsPasses.value.push(valuesWithCarnetSanteId)
      }
    }
  }).catch((error) => {
    console.error(error)
  })
}

const confirmDeleteMedicament = (medicamentId: string) => {
  medicamentToDelete.value = medicamentId
  showDeleteDialog.value = true
}

const handleDeleteMedicament = async () => {
  if (!medicamentToDelete.value) return

  const medicamentId = medicamentToDelete.value

  const removeMedicamentFromLists = () => {
    // Remove from traitementsEnCours
    const indexEnCours = traitementsEnCours.value.findIndex(t => t.id === medicamentId)
    if (indexEnCours !== -1) {
      traitementsEnCours.value.splice(indexEnCours, 1)
    }

    // Remove from traitementsPasses
    const indexPasses = traitementsPasses.value.findIndex(t => t.id === medicamentId)
    if (indexPasses !== -1) {
      traitementsPasses.value.splice(indexPasses, 1)
    }

    // Remove from medicaments list
    const indexMedicaments = medicaments.value.findIndex(m => m.id === medicamentId)
    if (indexMedicaments !== -1) {
      medicaments.value.splice(indexMedicaments, 1)
    }
  }

  await handleOfflineOperation(
    () => apiService.deleteMedicament(Number(medicamentId)),
    {
      endpoint: 'Medicament',
      method: 'DELETE',
      resourceId: medicamentId,
      onSuccess: () => {
        removeMedicamentFromLists()
      },
      onOfflineQueued: () => {
        removeMedicamentFromLists()
      },
      successMessage: 'Le traitement a été supprimé avec succès',
      errorMessage: 'Une erreur est survenue lors de la suppression du traitement',
    }
  )

  showDeleteDialog.value = false
  medicamentToDelete.value = null
}
</script>
