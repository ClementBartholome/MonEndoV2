<template>
  <div class="flex-column-container">
    <BackButton/>
    <section class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
      <div class="flex justify-between items-center mb-4 douleurs-header">
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
              <form class="flex flex-col gap-6" @submit.prevent="onSubmit">
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
                <div class="flex items-center gap-8 douleurs-date-time-row">
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
      <SectionKpiHeader :items="douleurKpis" />

      <div v-if="isLoading" class="flex flex-col space-y-3">
        <Skeleton class="h-[120px] w-full mt-2 rounded-xl"/>
        <Skeleton class="h-[120px] w-full rounded-xl"/>
        <Skeleton class="h-[120px] w-full rounded-xl"/>
      </div>
      <template v-else-if="filteredEntries.length > 0">
        <!-- Mobile: cards -->
        <div class="md:hidden">
          <GenericCardList
            :entries="filteredEntries"
            titleField="type"
            dateField="date"
            timeField="time"
            intensityField="intensite"
            :extraFields="[{ key: 'commentaire', label: 'Note' }]"
            :iconConfig="douleurIconConfig"
            :onDelete="handleDelete"
            :onEdit="handleEditEntry"
            emptyMessage="Aucune douleur enregistrée ce mois"
          />
        </div>
        <!-- Desktop: table -->
        <div class="hidden md:block">
          <Datatable :entries="filteredEntries" :columns="columns" :deleteFunction="handleDelete" @edit-entry="handleEditEntry">
            <thead>
              <tr>
                <th>Type</th><th>Date</th><th>Heure</th><th>Intensité</th><th>Commentaire</th><th></th><th></th>
              </tr>
            </thead>
          </Datatable>
        </div>
      </template>
      <EmptyStateAction
        v-else
        title="Aucune douleur enregistrée"
        description="Ajoute une entrée pour suivre les épisodes et identifier les tendances."
        actionLabel="Ajouter une douleur"
        @action="isDialogOpen = true"
      />
    </section>
    <section class="container !mt-0 mx-auto py-8 px-4 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
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
              :data="chartData"
              :categories="['intensite']"
              index="date"
          />
        </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import * as z from 'zod'

import { Button } from '@/shared/components/ui/button'
import { Slider } from '@/shared/components/ui/slider'
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/shared/components/ui/form'
import { Input } from '@/shared/components/ui/input'
import { LineChart } from "@/shared/components/ui/chart-line"
import { Skeleton } from '@/shared/components/ui/skeleton'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/shared/components/ui/dialog'
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from '@/shared/components/ui/select'
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
import { eachDayOfInterval, startOfMonth, endOfMonth } from 'date-fns'

const authStore = useAuthStore()
const { formatDateDisplay, formatTimeDisplay, combineDateTime, getCurrentDateInput, getCurrentTimeInput } = useDateTimeFormat()

const { selectedMonthYear, entries, isLoading } = useMonthData({
  fetchFunction: async (month, year) => {
    return apiService.getDonneesDouleursByMonth(authStore.user!.carnetSanteId, month, year)
  },
  transformData: (response) => {
    const [year, month] = selectedMonthYear.value.split('-').map(Number)
    const startDate = startOfMonth(new Date(year, month - 1))
    const endDate = endOfMonth(startDate)
    const allDays = eachDayOfInterval({ start: startDate, end: endDate })

    return allDays.flatMap(day => {
      const formattedDate = formatDateDisplay(day)
      const dayEntries = response.filter(entry => {
        const entryDate = formatDateDisplay(entry.date)
        return entryDate === formattedDate
      })

      if (dayEntries.length > 0) {
        return dayEntries.map(entry => ({
          id: entry.id,
          type: entry.typeDouleur,
          date: formattedDate,
          time: formatTimeDisplay(entry.date),
          intensite: entry.intensite,
          commentaire: entry.commentaire || 'Pas de commentaire',
        }))
      } else {
        return [{
          type: 'Aucune douleur',
          date: formattedDate,
          time: '00h00',
          intensite: 0,
          commentaire: '',
        }]
      }
    })
  },
  dataType: 'douleurs'
})

const { deleteEntry } = useCrudOperations(entries)

const isEditMode = ref(false)
const isDialogOpen = ref(false)
const { submitForm } = useDialogForm(isDialogOpen)

const columns: any = [
  { data: 'type' },
  { data: 'date' },
  { data: 'time' },
  { data: 'intensite' },
  { data: 'commentaire' },
  { data: 'actions', defaultContent: '<span class="material-symbols-outlined edit-btn">edit</span>' },
  { data: null, defaultContent: '<span class="material-symbols-outlined delete-btn">delete</span>' },
]

const douleurIconConfig: Record<string, { color: string; bg: string; icon: string }> = {
  'Douleur pelvienne':      { color: 'text-red-600',    bg: 'bg-red-100',    icon: 'emergency_home' },
  'Douleur abdominale':     { color: 'text-orange-600', bg: 'bg-orange-100', icon: 'sick' },
  'Douleur lombaire':       { color: 'text-amber-600',  bg: 'bg-amber-100',  icon: 'spine' },
  'Douleur thoracique':     { color: 'text-blue-600',   bg: 'bg-blue-100',   icon: 'favorite' },
  'Douleur projetée':       { color: 'text-purple-600', bg: 'bg-purple-100', icon: 'neurology' },
  'Douleur neuropathique':  { color: 'text-indigo-600', bg: 'bg-indigo-100', icon: 'bolt' },
  'Dyspareunie':            { color: 'text-pink-600',   bg: 'bg-pink-100',   icon: 'health_and_beauty' },
  'Autre':                  { color: 'text-gray-500',   bg: 'bg-gray-100',   icon: 'help_outline' },
}

const filteredEntries = computed(() => {
  return entries.value.filter(entry => entry.intensite !== 0)
})

const douleurKpis = computed(() => {
  const uniquePainDays = new Set(filteredEntries.value.map(entry => entry.date)).size
  const maxIntensity = filteredEntries.value.reduce((max, entry) => Math.max(max, Number(entry.intensite || 0)), 0)
  return [
    { label: 'Jours douloureux', value: uniquePainDays },
    { label: 'Intensité moy.', value: averageIntensity.value },
    { label: 'Pic d\'intensité', value: maxIntensity }
  ]
})

const chartData = computed(() => {
  return entries.value.map((entry: any) => ({
    date: entry.date,
    intensite: entry.intensite,
    type: entry.type,
    commentaire: entry.commentaire,
  }))
})

const averageIntensity = computed(() => {
  if (filteredEntries.value.length === 0) {
    return 'N/A'
  }
  const totalIntensity = filteredEntries.value.reduce((total, entry) => total + Number(entry.intensite), 0)
  return (totalIntensity / filteredEntries.value.length).toFixed(2)
})


const handleDelete = async (id: string | number) => {
  await deleteEntry(id as number, (id) => apiService.deleteDonneesDouleurs(id as number), {
    successMessage: 'La douleur a été supprimée avec succès',
    errorMessage: 'Un problème est survenu lors de la suppression de la douleur',
    endpoint: 'DonneesDouleurs'
  })
}

const handleEditEntry = (id: string | number) => {
  const entry = entries.value.find(entry => entry.id === (id as number))
  if (entry) {
    const [day, month, year] = entry.date.split('/').map(Number)
    const parsedDate = new Date(year, month - 1, day)
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
    })

    isEditMode.value = true
    isDialogOpen.value = true
  }
}

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
}))

const form = useForm({
  validationSchema: formSchema,
  initialValues: {
    id: 0,
    intensite: [5],
    date: getCurrentDateInput(),
    time: getCurrentTimeInput(),
  }
})

const onSubmit = form.handleSubmit((values) => {
  const dataToSend = {
    typeDouleur: values.type.trim(),
    intensite: values.intensite[0],
    date: combineDateTime(values.date, values.time),
    commentaire: values.commentaire || 'Pas de commentaire',
    carnetSanteId: authStore.user?.carnetSanteId,
  }

  if (isEditMode.value && values.id !== null) {
    // Mode édition
    submitForm(dataToSend, {
      submitFunction: (data) => apiService.editDonneesDouleurs(values.id as number, data),
      successMessage: 'La douleur a été modifiée avec succès',
      errorMessage: 'Un problème est survenu lors de la modification de la douleur',
      onSuccess: () => {
        const entryIndex = entries.value.findIndex((entry) => entry.id === values.id)
        if (entryIndex !== -1) {
          entries.value[entryIndex] = {
            id: values.id,
            type: values.type,
            date: formatDateDisplay(values.date),
            time: formatTimeDisplay(values.time),
            intensite: values.intensite[0],
            commentaire: values.commentaire || 'Pas de commentaire',
          }
        }
        isEditMode.value = false
      },
      resetFormData: () => {
        form.resetForm()
      }
    })
  } else {
    // Mode création
    submitForm(dataToSend, {
      submitFunction: (data) => apiService.postDonneesDouleurs(data),
      successMessage: 'La douleur a été ajoutée avec succès',
      errorMessage: 'Un problème est survenu lors de l\'ajout de la douleur',
      onSuccess: (response) => {
        entries.value.push({
          id: response.id,
          type: values.type,
          date: formatDateDisplay(values.date),
          time: formatTimeDisplay(values.time),
          intensite: values.intensite[0],
          commentaire: values.commentaire || 'Pas de commentaire',
        })
      },
      resetFormData: () => {
        form.resetForm()
      }
    })
  }
})
</script>

<style scoped>
@media (max-width: 425px) {
  .douleurs-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.75rem;
  }

  .douleurs-date-time-row {
    flex-direction: column;
    align-items: stretch;
    gap: 0.75rem;
  }
}
</style>

