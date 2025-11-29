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
      </TabsContent>
      <TabsContent value="symptomes">
        <section class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
          <div class="flex justify-between items-center mb-4 flex flex-wrap gap-2 h-full">
            <h2 class="text-2xl flex gap-2 ml-2">
              <i class="material-symbols-outlined text-3xl ml-auto">gynecology</i>Symptômes
            </h2>
            <div class="form-modal">
              <Dialog v-model:open="showAddDialog">
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
          <Datatable v-else-if="entries.length > 0" :entries="entries" :columns="columns" :deleteFunction="handleDelete">
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
import { Calendar } from '@/shared/components/ui/calendar'
import { type DateValue, getLocalTimeZone, today, parseDate } from '@internationalized/date'
import { type Ref, ref, onMounted, watch, computed } from 'vue'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/shared/components/ui/dialog"
import { Button } from "@/shared/components/ui/button"
import { Input } from "@/shared/components/ui/input"
import { FormControl, FormItem, FormLabel, FormField, FormMessage } from "@/shared/components/ui/form"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/shared/components/ui/tabs"
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from '@/shared/components/ui/select'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import * as z from 'zod'
import { Slider } from "@/shared/components/ui/slider"
import { Skeleton } from "@/shared/components/ui/skeleton"
import Datatable from "@/shared/components/Datatable.vue"
import BackButton from "@/shared/components/BackButton.vue"

import apiService from "@/shared/services/apiService"
import { useAuthStore } from "@/features/auth/store/auth"
import { useMonthData } from '@/shared/composables/useMonthData'
import { useDateTimeFormat } from '@/shared/composables/useDateTimeFormat'
import { useCrudOperations } from '@/shared/composables/useCrudOperations'
import { useDialogForm } from '@/shared/composables/useDialogForm'
import { format } from 'date-fns'
import type { SymptomeCycle } from "@/features/cycle/types/symptome-cycle"

const { user } = useAuthStore()
const { formatDateDisplay, formatTimeDisplay, combineDateTime, getCurrentMonthYear } = useDateTimeFormat()

// Dialog control
const showAddDialog = ref(false)
const { submitForm } = useDialogForm(showAddDialog)

const selectedMonthYear = ref(getCurrentMonthYear())
const value = ref(today(getLocalTimeZone())) as Ref<DateValue>
const month = ref(value.value.month)
const year = ref(value.value.year)
const joursRegles = ref<Date[]>([])
const joursOvulation = ref<Date[]>([])
const joursFertiles = ref<Date[]>([])
const joursSpotting = ref<Date[]>([])
const cycleMoyen = ref(28)

const { entries, isLoading, refetch } = useMonthData<SymptomeCycle>({
  fetchFunction: async (month, year) => {
    return apiService.getSymptomesByMonth(user!.carnetSanteId, month, year)
  },
  transformData: (response) => {
    joursSpotting.value = response
      .filter((s: SymptomeCycle) => s.typeSymptome === 'Spotting')
      .map((s: SymptomeCycle) => new Date(s.date))

    return response.map((symptomeCycle: SymptomeCycle) => ({
      id: symptomeCycle.id,
      typeSymptome: symptomeCycle.typeSymptome,
      date: formatDateDisplay(symptomeCycle.date),
      time: formatTimeDisplay(symptomeCycle.date),
      intensite: symptomeCycle.intensite,
      commentaire: symptomeCycle.commentaire || 'Pas de commentaire'
    }))
  },
  immediate: false
})

const { deleteEntry, createEntry } = useCrudOperations(entries)

const entry = ref({
  typeSymptome: '',
  date: '',
  time: '',
  intensite: 0,
  commentaire: ''
})

const columns: any = [
  { data: 'typeSymptome' },
  { data: 'date' },
  { data: 'time' },
  { data: 'intensite' },
  { data: 'commentaire' },
  {
    data: null,
    defaultContent: '<span class="material-symbols-outlined delete-btn">delete</span>'
  }
]

onMounted(() => {
  fetchJoursRegles()
  refetch()
})

const calendarValue = computed(() => {
  const [year, month] = selectedMonthYear.value.split('-').map(Number)
  const paddedMonth = month.toString().padStart(2, '0')
  return parseDate(`${year}-${paddedMonth}-01`)
})

const previousMonth = () => {
  const [year, month] = selectedMonthYear.value.split('-').map(Number)
  const newDate = new Date(year, month - 2, 1)
  selectedMonthYear.value = format(newDate, 'yyyy-MM')
}

const nextMonth = () => {
  const [year, month] = selectedMonthYear.value.split('-').map(Number)
  const newDate = new Date(year, month, 1)
  selectedMonthYear.value = format(newDate, 'yyyy-MM')
}

watch(calendarValue, (value) => {
  month.value = value.month
  year.value = value.year
  calculateFertilePeriodsForMonth(joursRegles.value)
  refetch()
})

watch(selectedMonthYear, () => {
  fetchJoursRegles()
})

const fetchJoursRegles = async () => {
  try {
    const response = await apiService.getJoursReglesByMonth(user!.carnetSanteId, month.value, year.value)
    joursRegles.value = response.$values.map(jour => new Date(jour.date))

    const { fertileDays, ovulationDates } = calculateFertilePeriodsForMonth(joursRegles.value)

    joursFertiles.value = fertileDays
    joursOvulation.value = ovulationDates

    updateAverageCycle(joursRegles.value)
  } catch (error) {
    console.error('Erreur lors de la récupération des jours de règles:', error)
  }
}

const isInSelectedMonth = (date: Date): boolean => {
  return date.getMonth() + 1 === month.value && date.getFullYear() === year.value
}

const getMonthBoundaries = () => {
  const firstDay = new Date(year.value, month.value - 1, 1)
  const lastDay = new Date(year.value, month.value, 0)
  return { firstDay, lastDay }
}

const calculateFertilePeriodsForMonth = (reglesDates: Date[]) => {
  const dates: Date[] = []
  const ovulationDates: Date[] = []

  const sortedRegles = reglesDates.sort((a, b) => a.getTime() - b.getTime())

  sortedRegles.forEach((regleDate) => {
    const ovulationDate = new Date(regleDate)
    ovulationDate.setDate(regleDate.getDate() + (cycleMoyen.value - 14))

    for (let i = -5; i <= 1; i++) {
      const fertileDate = new Date(ovulationDate)
      fertileDate.setDate(ovulationDate.getDate() + i)

      if (isInSelectedMonth(fertileDate)) {
        dates.push(fertileDate)
      }
    }

    if (isInSelectedMonth(ovulationDate)) {
      ovulationDates.push(ovulationDate)
    }
  })

  return {
    fertileDays: dates,
    ovulationDates: ovulationDates
  }
}

const updateAverageCycle = (reglesDates: Date[]) => {
  if (reglesDates.length < 2) return

  const sortedDates = reglesDates.sort((a, b) => a.getTime() - b.getTime())
  const cyclesDuration: number[] = []

  for (let i = 1; i < sortedDates.length; i++) {
    const cycleDuration = Math.floor(
      (sortedDates[i].getTime() - sortedDates[i - 1].getTime()) / (1000 * 60 * 60 * 24)
    )
    if (cycleDuration > 0 && cycleDuration <= 45) {
      cyclesDuration.push(cycleDuration)
    }
  }

  if (cyclesDuration.length > 0) {
    cycleMoyen.value = Math.round(
      cyclesDuration.reduce((acc, curr) => acc + curr, 0) / cyclesDuration.length
    )
  }
}

const handleDelete = async (id: number) => {
  await deleteEntry(id, (id) => apiService.deleteSymptomeCycle(id as number), {
    successMessage: 'Symptôme supprimé avec succès',
    errorMessage: 'Une erreur est survenue lors de la suppression du symptôme',
    endpoint: 'SymptomesCycle'
  })
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
})

const onSubmit = form.handleSubmit((values) => {
  const dataToSend = {
    typeSymptome: entry.value.typeSymptome,
    carnetSanteId: user?.carnetSanteId,
    date: combineDateTime(values.date, values.time),
    intensite: values.intensite[0],
    commentaire: values.commentaire || 'Pas de commentaire',
  }

  submitForm(dataToSend, {
    submitFunction: (data) => apiService.postDonneesSymptomesCycle(data),
    successMessage: 'Symptôme ajouté avec succès',
    errorMessage: 'Une erreur est survenue lors de l\'ajout du symptôme',
    onSuccess: (response) => {
      entries.value.push({
        id: response.id,
        typeSymptome: entry.value.typeSymptome,
        date: formatDateDisplay(values.date),
        time: formatTimeDisplay(values.time),
        intensite: values.intensite[0],
        commentaire: values.commentaire || 'Pas de commentaire',
      })
    },
    resetFormData: () => {
      entry.value = {
        typeSymptome: '',
        date: '',
        time: '',
        intensite: 0,
        commentaire: ''
      }
      form.resetForm()
    }
  })
})
</script>
