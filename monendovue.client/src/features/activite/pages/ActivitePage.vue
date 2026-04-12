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
            emptyMessage="Aucune session enregistrée ce mois"
          />
        </div>
        <!-- Desktop: table -->
        <div class="hidden md:block">
          <Datatable :entries="entries" :columns="columns" :deleteFunction="handleDelete">
            <thead>
              <tr>
                <th>Type</th><th>Date</th><th>Heure</th><th>Durée</th><th>Intensité</th><th>Commentaire</th><th></th>
              </tr>
            </thead>
          </Datatable>
        </div>
      </template>
      <div v-else class="flex justify-center items-center h-32">
        <p class="text-xl text-center text-muted-foreground italic">Aucune session enregistrée</p>
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
      </section>
    </div>
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

import apiService from "@/shared/services/apiService"
import { useAuthStore } from '@/features/auth/store/auth'
import { useMonthData } from '@/shared/composables/useMonthData'
import { useDateTimeFormat } from '@/shared/composables/useDateTimeFormat'
import { useCrudOperations } from '@/shared/composables/useCrudOperations'
import { useDialogForm } from '@/shared/composables/useDialogForm'
import type { DonneesActivitePhysique } from "@/features/activite/types/donnees-activite-physique"

const authStore = useAuthStore()
const { formatDateDisplay, formatTimeDisplay, combineDateTime } = useDateTimeFormat()

// Dialog control
const showAddDialog = ref(false)
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

const { deleteEntry, createEntry } = useCrudOperations(entries)

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

const totalSessionDuration = computed(() => {
  const totalMinutes = entries.value.reduce((total, entry) => {
    const dureeString = String(entry.duree)
    return total + Number(dureeString.replace('min', ''))
  }, 0)
  const hours = Math.floor(totalMinutes / 60)
  const minutes = totalMinutes % 60
  return `${hours}h${minutes.toString().padStart(2, '0')}`
})

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
})

const onSubmit = form.handleSubmit((values) => {
  const dataToSend = {
    id: 0,
    typeActivite: values.typeActivite,
    intensite: values.intensite[0],
    duree: values.duree,
    date: combineDateTime(values.date, values.time),
    commentaire: values.commentaire || 'Pas de commentaire',
    carnetSanteId: authStore.user!.carnetSanteId,
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
      form.resetForm()
    }
  })
})
</script>
