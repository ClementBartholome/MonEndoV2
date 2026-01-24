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
          <!-- Period marking section - only show if not marked today -->
          <div v-if="isLoadingPeriod" class="flex flex-col space-y-3 p-6 mb-4">
            <Skeleton class="h-[80px] w-full rounded-xl"/>
          </div>
          <div v-else-if="!periodMarked" class="mb-6 p-4 bg-red-50 rounded-xl border-2 border-red-200">
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-3">
                <span class="text-lg font-medium">As-tu eu tes règles aujourd'hui ?</span>
              </div>
              <div class="ml-auto">
                <Button variant="custom" @click="markPeriodToday">Oui</Button>
              </div>
            </div>
          </div>

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
              :joursAcne="joursAcne"
              @day-click="handleDayClick"
          />

          <!-- Calendar Legend -->
          <div class="flex items-center justify-center gap-4 mt-4 text-sm">
            <div class="flex items-center gap-1.5">
              <div class="w-2 h-2 rounded-full bg-red-500"></div>
              <span>Règles</span>
            </div>
            <div class="flex items-center gap-1.5">
              <div class="w-2 h-2 rounded-full bg-red-300"></div>
              <span>Spotting</span>
            </div>
            <div class="flex items-center gap-1.5">
              <div class="w-2 h-2 rounded-full bg-purple-500"></div>
              <span>Acné</span>
            </div>
          </div>
        </section>

        <!-- Confirmation Dialog for Calendar Click -->
        <Dialog v-model:open="showConfirmJourRegleDialog">
          <DialogContent>
            <DialogHeader>
              <DialogTitle class="text-2xl">Confirmer le jour de règles</DialogTitle>
            </DialogHeader>
            <p class="text-base py-4">
              Voulez-vous marquer le <strong>{{ pendingJourRegleDate ? format(new Date(pendingJourRegleDate), 'dd/MM/yyyy') : '' }}</strong> comme jour de règles ?
            </p>
            <DialogFooter>
              <Button variant="outline" @click="showConfirmJourRegleDialog = false">
                Annuler
              </Button>
              <Button variant="custom" @click="confirmAddJourRegle">
                Confirmer
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </TabsContent>
      <TabsContent value="symptomes">
        <!-- Ongoing Acné Periods Section -->
        <section v-if="ongoingAcnePeriods.length > 0" class="container !mt-0 mx-auto py-8 mb-4 w-full bg-clearer rounded-3xl shadow-xl">
          <div class="mb-4">
            <h2 class="text-2xl flex gap-2 ml-2 mb-4">
              <i class="material-symbols-outlined text-3xl">schedule</i>
              Période d'acné en cours
            </h2>
            <div class="flex flex-col gap-3 px-2">
              <div v-for="period in ongoingAcnePeriods" :key="`${period.startDate}-${period.endDate}`"
                   class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 p-3 bg-white rounded-lg border border-gray-200 shadow-sm">
                <div class="flex flex-col">
                  <span class="font-medium">{{ period.startDate }} - {{ period.endDate }}</span>
                  <span class="text-sm text-gray-600">{{ period.duration }} jour{{ period.duration > 1 ? 's' : '' }} • Intensité moyenne: {{ period.avgIntensity }}</span>
                </div>
                <div class="flex gap-2 flex-wrap sm:flex-nowrap">
                  <Button variant="custom" size="sm" @click="extendAcnePeriod(period)">
                    <i class="material-symbols-outlined mr-1">add</i>
                    Ajouter aujourd'hui
                  </Button>
                  <Button variant="outline" size="sm" @click="openEndPeriodDialog(period)">
                    <i class="material-symbols-outlined mr-1">close</i>
                    Terminer
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- End Period Dialog -->
        <Dialog v-model:open="showEndPeriodDialog">
          <DialogContent>
            <DialogHeader>
              <DialogTitle class="text-2xl">Terminer la période d'acné</DialogTitle>
            </DialogHeader>
            <div class="flex flex-col gap-4 py-4">
              <p>Cette période a commencé le {{ selectedPeriodToEnd?.startDate }} et se termine actuellement le {{ selectedPeriodToEnd?.endDate }}.</p>
              <FormField name="endDate">
                <FormItem>
                  <FormLabel>Date de fin réelle</FormLabel>
                  <FormControl>
                    <Input type="date" v-model="endPeriodDate" :max="format(new Date(), 'yyyy-MM-dd')" />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              </FormField>
            </div>
            <DialogFooter>
              <Button variant="outline" @click="showEndPeriodDialog = false">
                Annuler
              </Button>
              <Button variant="custom" @click="confirmEndPeriod">
                Confirmer
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        <!-- Regular Symptoms Section -->
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

                    <!-- Period checkbox -->
                    <FormField name="isPeriod" v-slot="{ componentField, value }">
                      <FormItem>
                        <FormControl>
                          <div class="flex items-center gap-2">
                            <Checkbox
                                id="isPeriod"
                                v-bind="componentField"
                                :checked="value"
                                @update:checked="componentField.onChange"
                                class="shrink-0"
                            />
                            <FormLabel for="isPeriod" class="!mt-0 cursor-pointer">
                              Période de plusieurs jours
                            </FormLabel>
                          </div>
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    </FormField>

                    <!-- Single day fields (shown when not a period) -->
                    <div v-if="!entry.isPeriod" key="single-day-fields" class="flex items-center gap-8">
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

                    <!-- Period fields (shown when isPeriod is checked) -->
                    <div v-if="entry.isPeriod" key="period-fields" class="flex flex-col gap-4">
                      <FormField v-slot="{ componentField }" name="dateDebut">
                        <FormItem>
                          <FormLabel>Date de début</FormLabel>
                          <FormControl>
                            <Input type="date" v-model="entry.dateDebut" v-bind="componentField"/>
                          </FormControl>
                          <FormMessage/>
                        </FormItem>
                      </FormField>

                      <FormField name="enCours" v-slot="{ componentField, value }">
                        <FormItem>
                          <FormControl>
                            <div class="flex items-center gap-2">
                              <Checkbox
                                  id="enCoursEntry"
                                  v-bind="componentField"
                                  :checked="value"
                                  @update:checked="componentField.onChange"
                                  class="shrink-0"
                              />
                              <FormLabel for="enCoursEntry" class="!mt-0 cursor-pointer">
                                Toujours en cours
                              </FormLabel>
                            </div>
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      </FormField>

                      <FormField v-if="!entry.enCours" v-slot="{ componentField }" name="dateFin">
                        <FormItem>
                          <FormLabel>Date de fin</FormLabel>
                          <FormControl>
                            <Input type="date" v-model="entry.dateFin" v-bind="componentField"/>
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
                    <FormField name="photo">
                      <FormItem>
                        <FormLabel>Photo <span>(optionnel)</span></FormLabel>
                        <FormControl>
                          <Input
                              type="file"
                              accept="image/*"
                              capture="environment"
                              @change="handlePhotoUpload"
                          />
                        </FormControl>
                        <FormMessage />
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
          <Datatable v-else-if="processedEntries.length > 0" :entries="processedEntries" :columns="columns" :deleteFunction="handleDelete">
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
      <!-- Photo Modal -->
      <Dialog v-model:open="showPhotoModal">
        <DialogContent class="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Photo</DialogTitle>
          </DialogHeader>
          <div class="flex justify-center">
            <img v-if="selectedPhotoUrl" :src="selectedPhotoUrl" alt="Photo symptôme" class="max-w-full max-h-96 rounded-lg" />
          </div>
        </DialogContent>
      </Dialog>
    </Tabs>
  </div>
</template>

<script setup lang="ts">
import { Calendar } from '@/shared/components/ui/calendar'
import { type DateValue, getLocalTimeZone, today, parseDate } from '@internationalized/date'
import { type Ref, ref, onMounted, watch, computed, nextTick } from 'vue'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "@/shared/components/ui/dialog"
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
import { useSync } from '@/shared/composables/useSync'
import { useToast } from '@/shared/components/ui/toast'
import { format, parseISO } from 'date-fns'
import type { SymptomeCycle } from "@/features/cycle/types/symptome-cycle"
import offlineStorage from "@/shared/services/offlineStorage"
import {Checkbox} from "@/shared/components/ui/checkbox";

const { user } = useAuthStore()
const { formatDateDisplay, formatTimeDisplay, combineDateTime, getCurrentMonthYear } = useDateTimeFormat()
const { toast } = useToast()

// Dialog control
const showAddDialog = ref(false)
const { submitForm } = useDialogForm(showAddDialog)

// End period dialog
const showEndPeriodDialog = ref(false)
const selectedPeriodToEnd = ref<any>(null)
const endPeriodDate = ref('')

const showPhotoModal = ref(false)
const selectedPhotoUrl = ref('')

const openPhotoModal = (url: string) => {
  selectedPhotoUrl.value = url
  showPhotoModal.value = true
}


// Period marking
const periodMarked = ref(false)
const isLoadingPeriod = ref(true)
const { handleOfflineOperation } = useSync()

const selectedMonthYear = ref(getCurrentMonthYear())
const value = ref(today(getLocalTimeZone())) as Ref<DateValue>
const month = ref(value.value.month)
const year = ref(value.value.year)
const joursRegles = ref<Date[]>([])
const joursOvulation = ref<Date[]>([])
const joursFertiles = ref<Date[]>([])
const joursSpotting = ref<Date[]>([])
const joursAcne = ref<Date[]>([])
const cycleMoyen = ref(28)
const selectedPhoto = ref<File | null>(null)

const handlePhotoUpload = (event: Event) => {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0] ?? null
  selectedPhoto.value = file
}

const { entries, isLoading, refetch } = useMonthData<SymptomeCycle>({
  fetchFunction: async (month, year) => {
    return apiService.getSymptomesByMonth(user!.carnetSanteId, month, year)
  },
  transformData: (response) => {
    joursSpotting.value = response
      .filter((s: SymptomeCycle) => s.typeSymptome === 'Spotting')
      .map((s: SymptomeCycle) => new Date(s.date))

    joursAcne.value = response
      .filter((s: SymptomeCycle) => s.typeSymptome === 'Acné')
      .map((s: SymptomeCycle) => new Date(s.date))

    return response.map((symptomeCycle: SymptomeCycle) => ({
      id: symptomeCycle.id,
      typeSymptome: symptomeCycle.typeSymptome,
      date: formatDateDisplay(symptomeCycle.date),
      time: formatTimeDisplay(symptomeCycle.date),
      intensite: symptomeCycle.intensite,
      commentaire: symptomeCycle.commentaire || 'Pas de commentaire',
      photoUrl: symptomeCycle.photoUrl || ''
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
  commentaire: '',
  isPeriod: false,
  dateDebut: '',
  dateFin: '',
  enCours: false
})

// Confirmation dialog for calendar clicks
const showConfirmJourRegleDialog = ref(false)
const pendingJourRegleDate = ref<string>('')

const columns: any = [
  { data: 'typeSymptome' },
  { data: 'date' },
  { data: 'time' },
  { data: 'intensite' },
  { data: 'commentaire' },
  {
    data: 'photoUrl',
    render: (data: string) => data ? `<i class="material-symbols-outlined cursor-pointer photo-icon" data-url="${data}">photo_camera</i>` : ''
  },
  { data: null, defaultContent: '<span class="material-symbols-outlined delete-btn">delete</span>' }
]

// Group consecutive acné entries
const processedEntries = computed(() => {
  if (!entries.value || entries.value.length === 0) return []

  // Sort entries by date (parse DD/MM/YYYY format)
  const sortedEntries = [...entries.value].sort((a, b) => {
    const [dayA, monthA, yearA] = a.date.split('/').map(Number)
    const [dayB, monthB, yearB] = b.date.split('/').map(Number)
    const dateA = new Date(yearA, monthA - 1, dayA)
    const dateB = new Date(yearB, monthB - 1, dayB)
    return dateA.getTime() - dateB.getTime()
  })

  const result: any[] = []
  let i = 0

  while (i < sortedEntries.length) {
    const entry = sortedEntries[i]

    // If it's not an acné entry, just add it normally
    if (entry.typeSymptome !== 'Acné') {
      result.push(entry)
      i++
      continue
    }

    // It's an acné entry - check for consecutive days
    const acneGroup = [entry]
    let j = i + 1

    while (j < sortedEntries.length && sortedEntries[j].typeSymptome === 'Acné') {
      const currentEntry = sortedEntries[j]
      const previousEntry = sortedEntries[j - 1]

      // Parse dates
      const [dayPrev, monthPrev, yearPrev] = previousEntry.date.split('/').map(Number)
      const [dayCurr, monthCurr, yearCurr] = currentEntry.date.split('/').map(Number)
      const datePrev = new Date(yearPrev, monthPrev - 1, dayPrev)
      const dateCurr = new Date(yearCurr, monthCurr - 1, dayCurr)

      // Check if dates are consecutive (difference of 1 day)
      const diffInDays = Math.round((dateCurr.getTime() - datePrev.getTime()) / (1000 * 60 * 60 * 24))

      if (diffInDays === 1) {
        acneGroup.push(currentEntry)
        j++
      } else {
        break
      }
    }

    // If we have multiple consecutive acné days, group them
    if (acneGroup.length > 1) {
      const firstDate = acneGroup[0].date
      const lastDate = acneGroup[acneGroup.length - 1].date
      const avg = acneGroup.reduce((sum, e) => sum + e.intensite, 0) / acneGroup.length
      const avgIntensity = avg % 1 === 0 ? avg.toString() : avg.toFixed(1)

      result.push({
        id: `acne-group-${acneGroup[0].id}`,
        typeSymptome: 'Acné',
        date: `${firstDate} - ${lastDate}`,
        time: `${acneGroup.length} jour${acneGroup.length > 1 ? 's' : ''}`,
        intensite: avgIntensity,
        commentaire: acneGroup[0].commentaire,
        isGroup: true,
        groupedEntries: acneGroup,
        entryIds: acneGroup.map(e => e.id)
      })
    } else {
      // Single acné day, add normally
      result.push(entry)
    }

    i = j
  }

  return result
})

// Detect ongoing acné periods (periods ending today or yesterday)
const ongoingAcnePeriods = computed(() => {
  if (!entries.value || entries.value.length === 0) return []

  // Get all acné entries
  const acneEntries = entries.value
    .filter(e => e.typeSymptome === 'Acné')
    .sort((a, b) => {
      const [dayA, monthA, yearA] = a.date.split('/').map(Number)
      const [dayB, monthB, yearB] = b.date.split('/').map(Number)
      const dateA = new Date(yearA, monthA - 1, dayA)
      const dateB = new Date(yearB, monthB - 1, dayB)
      return dateA.getTime() - dateB.getTime()
    })

  if (acneEntries.length === 0) return []

  // Find groups of consecutive days
  const groups: any[] = []
  let currentGroup = [acneEntries[0]]

  for (let i = 1; i < acneEntries.length; i++) {
    const prevEntry = acneEntries[i - 1]
    const currEntry = acneEntries[i]

    const [dayPrev, monthPrev, yearPrev] = prevEntry.date.split('/').map(Number)
    const [dayCurr, monthCurr, yearCurr] = currEntry.date.split('/').map(Number)
    const datePrev = new Date(yearPrev, monthPrev - 1, dayPrev)
    const dateCurr = new Date(yearCurr, monthCurr - 1, dayCurr)

    const diffInDays = Math.round((dateCurr.getTime() - datePrev.getTime()) / (1000 * 60 * 60 * 24))

    if (diffInDays === 1) {
      currentGroup.push(currEntry)
    } else {
      if (currentGroup.length > 1) {
        groups.push([...currentGroup])
      }
      currentGroup = [currEntry]
    }
  }

  // Don't forget the last group
  if (currentGroup.length > 1) {
    groups.push(currentGroup)
  }

  // Filter for ongoing periods (ending today only)
  const today = new Date()
  today.setHours(0, 0, 0, 0)

  return groups.filter(group => {
    const lastEntry = group[group.length - 1]
    const [day, month, year] = lastEntry.date.split('/').map(Number)
    const lastDate = new Date(year, month - 1, day)
    lastDate.setHours(0, 0, 0, 0)

    const diffInDays = Math.round((today.getTime() - lastDate.getTime()) / (1000 * 60 * 60 * 24))

    // Period is ongoing if it ends today
    return diffInDays === 0
  }).map(group => ({
    startDate: group[0].date,
    endDate: group[group.length - 1].date,
    duration: group.length,
    entries: group,
    avgIntensity: (() => {
      const avg = group.reduce((sum: number, e: any) => sum + e.intensite, 0) / group.length
      return avg % 1 === 0 ? avg.toString() : avg.toFixed(1)
    })()
  }))
})

onMounted(() => {
  fetchJoursRegles()
  refetch()
})

onMounted(() => {
  nextTick(() => {
    document.addEventListener('click', (e) => {
      const target = e.target as HTMLElement
      if (target.classList.contains('photo-icon') && target.dataset.url) {
        openPhotoModal(target.dataset.url)
      }
    })
  })
})


onMounted(async () => {
  // Check if period was already marked today
  try {
    await offlineStorage.init()
    const cachedData = await offlineStorage.getCarnetData(user!.carnetSanteId)

    if (cachedData?.jourRegle?.date) {
      const today = format(new Date(), 'yyyy-MM-dd')
      const jourRegleDate = format(parseISO(cachedData.jourRegle.date), 'yyyy-MM-dd')
      if (jourRegleDate === today) {
        periodMarked.value = true
      }
    }
  } catch (error) {
    console.error('Error checking period status:', error)
  } finally {
    isLoadingPeriod.value = false
  }
})

const calendarValue = computed({
  get: () => {
    const [year, month] = selectedMonthYear.value.split('-').map(Number)
    const paddedMonth = month.toString().padStart(2, '0')
    return parseDate(`${year}-${paddedMonth}-01`)
  },
  set: (value) => {
    // Update selectedMonthYear when calendar changes
    if (value) {
      const newYear = value.year
      const newMonth = value.month
      selectedMonthYear.value = `${newYear}-${String(newMonth).padStart(2, '0')}`
    }
  }
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

    // Check if today is marked in the fetched jours de règles
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    const isTodayMarked = joursRegles.value.some(jour => {
      const jourDate = new Date(jour)
      jourDate.setHours(0, 0, 0, 0)
      return jourDate.getTime() === today.getTime()
    })
    if (isTodayMarked) {
      periodMarked.value = true
    }
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

const handleDelete = async (id: string | number) => {
  // Find the entry to determine if it's a group
  const entry = processedEntries.value.find(e => e.id === id);

  if (entry?.isGroup && entry.entryIds) {
    // Delete all entries in the group
    try {
      const deletePromises = entry.entryIds.map((entryId: number) =>
        apiService.deleteSymptomeCycle(entryId)
      );

      await Promise.all(deletePromises);

      // Remove all from local state
      entries.value = entries.value.filter(
        e => !entry.entryIds.includes(e.id)
      );

      toast({
        title: 'Succès',
        description: 'Période d\'acné supprimée',
        variant: 'custom',
      });
    } catch (error) {
      console.error('Error deleting group:', error);
      toast({
        title: 'Erreur',
        description: 'Erreur lors de la suppression',
        variant: 'destructive',
      });
    }
  } else {
    // Single entry deletion
    await deleteEntry(id as number, (id) => apiService.deleteSymptomeCycle(id as number), {
      successMessage: 'Symptôme supprimé avec succès',
      errorMessage: 'Une erreur est survenue lors de la suppression du symptôme',
      endpoint: 'SymptomesCycle'
    });
  }
}

// Unified schema that handles both period and single-day entries
const formSchema = toTypedSchema(z.object({
  typeSymptome: z.string({
    required_error: 'Le type de symptôme est requis'
  }),
  date: z.string().optional(),
  time: z.string().optional(),
  dateDebut: z.string().optional(),
  dateFin: z.string().optional(),
  enCours: z.boolean().optional(),
  isPeriod: z.boolean().optional(),
  intensite: z.array(z.number({
    required_error: 'L\'intensité est requise',
  })),
  commentaire: z.string().optional(),
}).superRefine((data, ctx) => {
  // Validate based on isPeriod
  if (entry.value.isPeriod) {
    // Period mode validation
    if (!data.dateDebut) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'La date de début est requise',
        path: ['dateDebut']
      });
    }
    if (!data.enCours && !data.dateFin) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'La date de fin est requise si non en cours',
        path: ['dateFin']
      });
    }
  } else {
    // Single day validation
    if (!data.date) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'La date est requise',
        path: ['date']
      });
    }
    if (!data.time) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'L\'heure est requise',
        path: ['time']
      });
    }
  }
}));

const form = useForm({
  validationSchema: formSchema,
  initialValues: {
    intensite: [5],
    typeSymptome: "Sélectionnez un symptôme"
  }
})

watch(() => form.values.isPeriod, v => { entry.value.isPeriod = !!v })
watch(() => form.values.enCours, v => { entry.value.enCours = !!v })

const onSubmit = form.handleSubmit(async (values) => {
  console.log(values)
  // Handle period submission (multiple days)
  if (entry.value.isPeriod) {
    const endDate = entry.value.enCours ? new Date() : new Date(entry.value.dateFin)
    const startDate = new Date(entry.value.dateDebut)

    // Validate dates
    if (startDate > endDate && !entry.value.enCours) {
      toast({
        title: 'Erreur',
        description: 'La date de début doit être avant la date de fin',
        variant: 'destructive',
      })
      return
    }

    // Generate all dates in the range
    const dates: Date[] = []
    const currentDate = new Date(startDate)
    while (currentDate <= endDate) {
      dates.push(new Date(currentDate))
      currentDate.setDate(currentDate.getDate() + 1)
    }

    // Create entries for each day
    const entriesToCreate = dates.map(date => ({
      typeSymptome: entry.value.typeSymptome,
      carnetSanteId: user?.carnetSanteId,
      date: combineDateTime(format(date, 'yyyy-MM-dd'), '12:00'),
      intensite: values.intensite[0],
      commentaire: values.commentaire || 'Pas de commentaire',
    }))

    try {
      const promises = entriesToCreate.map(entryData => apiService.postDonneesSymptomesCycle(entryData))
      await Promise.all(promises)
      await refetch()

      toast({
        title: 'Succès',
        description: `Période ajoutée (${entry.value.typeSymptome}, ${dates.length} jour${dates.length > 1 ? 's' : ''})`,
        variant: 'custom',
      })

      // Reset and close
      entry.value = {
        typeSymptome: '',
        date: '',
        time: '',
        intensite: 0,
        commentaire: '',
        isPeriod: false,
        dateDebut: '',
        dateFin: '',
        enCours: false
      }
      form.resetForm()
      showAddDialog.value = false
    } catch (error) {
      console.error('Error creating period:', error)
      toast({
        title: 'Erreur',
        description: 'Une erreur est survenue lors de l\'ajout de la période',
        variant: 'destructive',
      })
    }
  } else {
    // Handle single day submission
    const formData = new FormData()
    formData.append('typeSymptome', entry.value.typeSymptome)
    formData.append('carnetSanteId', user!.carnetSanteId.toString())
    formData.append('date', combineDateTime(values.date ?? '', values.time ?? '').toISOString())
    formData.append('intensite', values.intensite[0].toString())
    formData.append('commentaire', values.commentaire || 'Pas de commentaire')
    if (selectedPhoto.value) {
      formData.append('photo', selectedPhoto.value)
    }
    
    submitForm(formData, {
      submitFunction: (data) => apiService.postDonneesSymptomesCycle(data),
      successMessage: 'Symptôme ajouté avec succès',
      errorMessage: 'Une erreur est survenue lors de l\'ajout du symptôme',
      onSuccess: (response) => {
        entries.value.push({
          id: response.id,
          typeSymptome: entry.value.typeSymptome,
          date: formatDateDisplay(values.date || ''),
          time: formatTimeDisplay(values.time || ''),
          intensite: values.intensite[0],
          commentaire: values.commentaire || 'Pas de commentaire',
          photoUrl: response.photoUrl || ''
        })
      },
      resetFormData: () => {
        entry.value = {
          typeSymptome: '',
          date: '',
          time: '',
          intensite: 0,
          commentaire: '',
          isPeriod: false,
          dateDebut: '',
          dateFin: '',
          enCours: false
        }
        form.resetForm()
      }
    })
  }
})

const markPeriodToday = async () => {
  const today = format(new Date(), 'yyyy-MM-dd')
  const data = { date: today, carnetSanteId: user?.carnetSanteId }

  await handleOfflineOperation(
    () => apiService.postJourRegle(data),
    {
      endpoint: 'JourRegle',
      method: 'POST',
      data: data,
      onSuccess: () => {
        periodMarked.value = true
        fetchJoursRegles() // Refresh the calendar to show the new period day
      },
      onOfflineQueued: () => {
        periodMarked.value = true
      },
      successMessage: 'Règles marquées pour aujourd\'hui',
      errorMessage: 'Impossible de marquer les règles',
    }
  )
}

const extendAcnePeriod = async (period: any) => {
  const today = format(new Date(), 'yyyy-MM-dd')

  // Get the last entry from the period to use its intensity and comment
  const lastEntry = period.entries[period.entries.length - 1]

  const data = {
    typeSymptome: 'Acné',
    carnetSanteId: user?.carnetSanteId,
    date: combineDateTime(today, '12:00'),
    intensite: lastEntry.intensite,
    commentaire: lastEntry.commentaire
  }

  try {
    await apiService.postDonneesSymptomesCycle(data)
    await refetch()

    toast({
      title: 'Succès',
      description: 'Jour ajouté à la période d\'acné',
      variant: 'custom',
    })
  } catch (error) {
    console.error('Error extending acné period:', error)
    toast({
      title: 'Erreur',
      description: 'Une erreur est survenue lors de l\'extension de la période',
      variant: 'destructive',
    })
  }
}

const openEndPeriodDialog = (period: any) => {
  selectedPeriodToEnd.value = period
  // Pre-fill with last date of period
  const [day, month, year] = period.endDate.split('/').map(Number)
  endPeriodDate.value = `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`
  showEndPeriodDialog.value = true
}

const confirmEndPeriod = async () => {
  if (!selectedPeriodToEnd.value || !endPeriodDate.value) return

  const period = selectedPeriodToEnd.value
  const endDate = new Date(endPeriodDate.value)

  // Find entries to delete (those after the selected end date)
  const entriesToDelete = period.entries.filter((entry: any) => {
    const [day, month, year] = entry.date.split('/').map(Number)
    const entryDate = new Date(year, month - 1, day)
    return entryDate > endDate
  })

  if (entriesToDelete.length === 0) {
    // No entries to delete, just close
    showEndPeriodDialog.value = false
    toast({
      title: 'Période terminée',
      description: `Aucune modification nécessaire`,
      variant: 'custom',
    })
    return
  }

  try {
    // Delete entries after the end date
    const deletePromises = entriesToDelete.map((entry: any) =>
      apiService.deleteSymptomeCycle(entry.id)
    )
    await Promise.all(deletePromises)

    // Refresh data
    await refetch()

    showEndPeriodDialog.value = false
    toast({
      title: 'Succès',
      description: `Période terminée le ${format(endDate, 'dd/MM/yyyy')}`,
      variant: 'custom',
    })
  } catch (error) {
    console.error('Error ending period:', error)
    toast({
      title: 'Erreur',
      description: 'Impossible de terminer la période',
      variant: 'destructive',
    })
  }
}

const handleDayClick = async (date: any) => {
  // Convert DateValue to JS Date
  const jsDate = new Date(date.year, date.month - 1, date.day)

  // Convert the clicked date to string format
  const clickedDate = format(jsDate, 'yyyy-MM-dd')

  // Check if this day is already marked as a period day
  const isAlreadyMarked = joursRegles.value.some(jour =>
    format(jour, 'yyyy-MM-dd') === clickedDate
  )

  if (isAlreadyMarked) {
    return
  }

  // Store the date and show confirmation dialog
  pendingJourRegleDate.value = clickedDate
  showConfirmJourRegleDialog.value = true
}

const confirmAddJourRegle = async () => {
  const clickedDate = pendingJourRegleDate.value

  // Mark this day as a period day
  const data = { date: clickedDate, carnetSanteId: user?.carnetSanteId }

  await handleOfflineOperation(
    () => apiService.postJourRegle(data),
    {
      endpoint: 'JourRegle',
      method: 'POST',
      data: data,
      onSuccess: () => {
        fetchJoursRegles() // Refresh the calendar to show the new period day

        // If it's today, also mark periodMarked as true
        const today = format(new Date(), 'yyyy-MM-dd')
        if (clickedDate === today) {
          periodMarked.value = true
        }
      },
      onOfflineQueued: () => {
        // If it's today, also mark periodMarked as true
        const today = format(new Date(), 'yyyy-MM-dd')
        if (clickedDate === today) {
          periodMarked.value = true
        }
      },
      successMessage: 'Jour de règles marqué avec succès',
      errorMessage: 'Impossible de marquer ce jour',
    }
  )

  // Close the dialog
  showConfirmJourRegleDialog.value = false
}
</script>

<style scoped>
.checkmark-animation {
  display: flex;
  align-items: center;
  justify-content: center;
  animation: fadeIn 0.5s ease-in-out;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: scale(0.5);
  }
  to {
    opacity: 1;
    transform: scale(1);
  }
}
</style>
