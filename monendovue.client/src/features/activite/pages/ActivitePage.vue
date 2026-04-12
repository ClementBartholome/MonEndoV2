<template>
  <div class="flex-column-container">
    <BackButton/>
    <section class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
      <div class="flex justify-between items-center mb-4">
        <h2 class="text-2xl flex gap-2 ml-2"><i class="material-symbols-outlined text-3xl ml-auto">directions_run</i>
          Suivi de l'activité</h2>
        <div class="form-modal">
          <Dialog v-model:open="showAddDialog">
            <DialogTrigger class="flex gap-2 items-center cursor-pointer hover:opacity-80 transition-opacity">
              <Button variant="custom">
                <span class="hide-xsm">Ajouter une session</span>
                <i class="material-symbols-outlined">add</i>
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle class="text-2xl">{{ isEditMode ? 'Modifier la session' : 'Ajouter une session' }}</DialogTitle>
              </DialogHeader>
              <form class="flex flex-col gap-6" @submit.prevent="onSubmit">
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
                  {{ isEditMode ? 'Mettre à jour' : 'Enregistrer' }}
                </Button>
              </form>
            </DialogContent>
          </Dialog>
        </div>
      </div>
      <SelectMonth v-model="selectedMonthYear" />
      <SectionKpiHeader :items="activityKpis" />
      <div v-if="isLoading" class="flex flex-col space-y-3">
        <Skeleton class="h-[120px] w-full mt-2 rounded-xl"/>
        <Skeleton class="h-[120px] w-full rounded-xl"/>
        <Skeleton class="h-[120px] w-full rounded-xl"/>
      </div>
      <template v-else-if="entries.length > 0">
        <!-- Mobile: cards -->
        <div class="md:hidden">
          <GenericCardList
            :entries="entries"
            titleField="typeActivite"
            dateField="date"
            timeField="time"
            intensityField="intensite"
            :extraFields="[{ key: 'duree', label: 'Durée', suffix: ' min' }, { key: 'commentaire', label: 'Note' }]"
            :defaultIcon="{ color: 'text-teal-600', bg: 'bg-teal-100', icon: 'directions_run' }"
            :onDelete="handleDelete"
            :onEdit="handleEditEntry"
            emptyMessage="Aucune session enregistrée ce mois"
          />
        </div>
        <!-- Desktop: table -->
        <div class="hidden md:block">
          <Datatable :entries="entries" :columns="columns" :deleteFunction="handleDelete" @edit-entry="handleEditEntry">
            <thead>
              <tr>
                <th>Type</th><th>Date</th><th>Heure</th><th>Durée</th><th>Intensité</th><th>Commentaire</th><th></th>
              </tr>
            </thead>
          </Datatable>
        </div>
      </template>
      <EmptyStateAction
        v-else
        title="Aucune session enregistrée"
        description="Ajoute une activité pour commencer à suivre ton énergie et ton intensité."
        actionLabel="Ajouter une session"
        @action="showAddDialog = true"
      />
    </section>
    <section class="container !mt-0 mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
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
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import * as z from 'zod'

import { Button } from '@/shared/components/ui/button'
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/shared/components/ui/form'
import { Input } from '@/shared/components/ui/input'
import { LineChart } from "@/shared/components/ui/chart-line"
import { Slider } from "@/shared/components/ui/slider"
import { Skeleton } from "@/shared/components/ui/skeleton"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/shared/components/ui/dialog'
import Datatable from "@/shared/components/Datatable.vue"
import GenericCardList from "@/shared/components/GenericCardList.vue"
import BackButton from "@/shared/components/BackButton.vue"
import SelectMonth from "@/shared/components/SelectMonth.vue"
import SectionKpiHeader from "@/shared/components/SectionKpiHeader.vue"
import EmptyStateAction from "@/shared/components/EmptyStateAction.vue"

import apiService from "@/shared/services/apiService"
import { useAuthStore } from '@/features/auth/store/auth'
import { useMonthData } from '@/shared/composables/useMonthData'
import { useDateTimeFormat } from '@/shared/composables/useDateTimeFormat'
import { useCrudOperations } from '@/shared/composables/useCrudOperations'
import { useDialogForm } from '@/shared/composables/useDialogForm'
import type { DonneesActivitePhysique } from "@/features/activite/types/donnees-activite-physique"

const authStore = useAuthStore()
const { formatDateDisplay, formatTimeDisplay, combineDateTime, getCurrentDateInput, getCurrentTimeInput } = useDateTimeFormat()

// Dialog control
const showAddDialog = ref(false)
const isEditMode = ref(false)
const { submitForm } = useDialogForm(showAddDialog)

const { selectedMonthYear, entries, isLoading } = useMonthData<DonneesActivitePhysique>({
  fetchFunction: async (month, year) => {
    return apiService.getDonneesActivitePhysiqueByMonth(authStore.user!.carnetSanteId, month, year)
  },
  transformData: (data) => {
    return data.map((entry: DonneesActivitePhysique) => ({
      id: entry.id,
      typeActivite: entry.typeActivite,
      date: formatDateDisplay(entry.date),
      time: formatTimeDisplay(entry.date),
      duree: entry.duree,
      intensite: entry.intensite,
      commentaire: entry.commentaire || 'Pas de détails'
    }))
  },
  dataType: 'activite'
})

const { deleteEntry } = useCrudOperations(entries)

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
]

const editingEntryId = ref<number | null>(null)

const chartData = computed(() => {
  return entries.value.map((entry: any) => ({
    date: entry.date,
    type: entry.typeActivite,
    duree: entry.duree,
    intensite: entry.intensite,
    commentaire: entry.commentaire
  }))
})

const handleDelete = async (id: string | number) => {
  await deleteEntry(id as number, (id) => apiService.deleteDonneesActivitePhysique(id as number), {
    successMessage: 'La session a été supprimée avec succès',
    errorMessage: 'Une erreur est survenue lors de la suppression de la session',
    endpoint: 'DonneesActivitePhysique'
  })
}

const handleEditEntry = (id: string | number) => {
  const entry = entries.value.find(item => item.id === (id as number))
  if (!entry) return

  const [day, month, year] = String(entry.date).split('/').map(Number)
  const parsedDate = new Date(year, month - 1, day)

  editingEntryId.value = entry.id as number
  isEditMode.value = true
  form.setValues({
    id: entry.id,
    typeActivite: entry.typeActivite,
    date: new Intl.DateTimeFormat('fr-CA', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    }).format(parsedDate),
    time: String(entry.time).replace('h', ':'),
    duree: Number(String(entry.duree).replace('min', '')),
    intensite: [Number(entry.intensite)],
    commentaire: entry.commentaire === 'Pas de commentaire' ? '' : entry.commentaire,
  })
  showAddDialog.value = true
}

const totalSessionDuration = computed(() => {
  const totalMinutes = entries.value.reduce((total, entry) => {
    const dureeString = String(entry.duree)
    return total + Number(dureeString.replace('min', ''))
  }, 0)
  const hours = Math.floor(totalMinutes / 60)
  const minutes = totalMinutes % 60
  return `${hours}h${minutes.toString().padStart(2, '0')}`
})

const averageIntensity = computed(() => {
  if (entries.value.length === 0) return '0.0'
  const avg = entries.value.reduce((sum, entry: any) => sum + Number(entry.intensite || 0), 0) / entries.value.length
  return avg.toFixed(1)
})

const activityKpis = computed(() => ([
  { label: 'Sessions', value: entries.value.length },
  { label: 'Durée totale', value: totalSessionDuration.value },
  { label: 'Intensité moy.', value: averageIntensity.value }
]))

const formSchema = toTypedSchema(z.object({
  id: z.number().optional().nullable(),
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
    id: null,
    typeActivite: '',
    date: getCurrentDateInput(),
    time: getCurrentTimeInput(),
    duree: 30,
    intensite: [5],
    commentaire: '',
  }
})

const onSubmit = form.handleSubmit((values) => {
  const dataToSend = {
    id: editingEntryId.value ?? 0,
    typeActivite: values.typeActivite,
    intensite: values.intensite[0],
    duree: values.duree,
    date: combineDateTime(values.date, values.time),
    commentaire: values.commentaire || 'Pas de commentaire',
    carnetSanteId: authStore.user!.carnetSanteId,
  }

  if (isEditMode.value && editingEntryId.value !== null) {
    submitForm(dataToSend, {
      submitFunction: (data) => apiService.editDonneesActivitePhysique(editingEntryId.value as number, data),
      successMessage: 'La session a été modifiée avec succès',
      errorMessage: 'Un problème est survenu lors de la modification de la session',
      onSuccess: () => {
        const idx = entries.value.findIndex((entry) => entry.id === editingEntryId.value)
        if (idx !== -1) {
          entries.value[idx] = {
            id: editingEntryId.value,
            typeActivite: values.typeActivite,
            date: formatDateDisplay(values.date),
            time: formatTimeDisplay(values.time),
            duree: values.duree,
            intensite: values.intensite[0],
            commentaire: values.commentaire || 'Pas de commentaire',
          }
        }
      },
      resetFormData: () => {
        isEditMode.value = false
        editingEntryId.value = null
        form.resetForm({
          values: {
            id: null,
            typeActivite: '',
            date: getCurrentDateInput(),
            time: getCurrentTimeInput(),
            duree: 30,
            intensite: [5],
            commentaire: '',
          }
        })
      }
    })
    return
  }

  submitForm(dataToSend, {
    submitFunction: (data) => apiService.postDonneesActivitePhysique(data),
    successMessage: 'La session a été ajoutée avec succès',
    errorMessage: 'Un problème est survenu lors de l\'ajout de la session',
    onSuccess: (response) => {
      entries.value.push({
        id: response.id,
        typeActivite: values.typeActivite,
        date: formatDateDisplay(values.date),
        time: formatTimeDisplay(values.time),
        duree: values.duree,
        intensite: values.intensite[0],
        commentaire: values.commentaire || 'Pas de commentaire',
      })
    },
    resetFormData: () => {
      isEditMode.value = false
      editingEntryId.value = null
      form.resetForm({
        values: {
          id: null,
          typeActivite: '',
          date: getCurrentDateInput(),
          time: getCurrentTimeInput(),
          duree: 30,
          intensite: [5],
          commentaire: '',
        }
      })
    }
  })
})
</script>
