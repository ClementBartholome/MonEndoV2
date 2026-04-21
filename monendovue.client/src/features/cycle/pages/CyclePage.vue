<template>
  <div class="flex-column-container">
    <BackButton/>
    <div class="flex flex-col self-baseline w-full">
      <h2 class="text-2xl flex gap-2"><i class="material-symbols-outlined text-3xl">menstrual_health</i>Cycle menstruel
      </h2>
    </div>
    <Tabs default-value="cycles" class="w-full">
      <TabsList class="cycle-tabs-list">
        <TabsTrigger value="cycles">Mes cycles</TabsTrigger>
        <TabsTrigger value="symptomes">Symptômes</TabsTrigger>
      </TabsList>
      <TabsContent value="cycles">
        <section class="container !mt-0 mx-auto py-4 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
          <!-- Period marking section -->
          <div v-if="isLoadingPeriod" class="flex flex-col space-y-3 mb-4">
            <Skeleton class="h-[80px] w-full rounded-xl"/>
          </div>
          <PeriodQuickAdd
            v-else
            :isPeriodMarkedToday="periodMarked"
            :onMarkPeriod="handleMarkPeriod"
            @marked="onPeriodMarked"
            class="mb-4"
          />

          <div class="flex flex-col items-center m-auto mb-2 cycle-month-wrapper" style="max-width: 46%">
            <div class="flex items-center cycle-month-controls">
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
          <div class="flex items-center justify-center gap-4 mt-4 text-sm cycle-legend">
            <div class="flex items-center gap-1.5 cycle-legend-item">
              <div class="w-2 h-2 rounded-full bg-red-500"></div>
              <span>Règles</span>
            </div>
            <div class="flex items-center gap-1.5 cycle-legend-item">
              <div class="w-2 h-2 rounded-full bg-red-300"></div>
              <span>Spotting</span>
            </div>
            <div class="flex items-center gap-1.5 cycle-legend-item">
              <div class="w-2 h-2 rounded-full bg-purple-500"></div>
              <span>Acné</span>
            </div>
          </div>
        </section>

        <!-- Confirmation Dialog for Calendar Click (Add) -->
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

        <!-- Confirmation Dialog for Calendar Click (Delete) -->
        <Dialog v-model:open="showConfirmDeleteJourRegleDialog">
          <DialogContent>
            <DialogHeader>
              <DialogTitle class="text-2xl">Démarquer ce jour de règles</DialogTitle>
            </DialogHeader>
            <p class="text-base py-4">
              Voulez-vous retirer le <strong>{{ pendingJourRegleDate ? format(new Date(pendingJourRegleDate), 'dd/MM/yyyy') : '' }}</strong> des jours de règles ?
            </p>
            <DialogFooter>
              <Button variant="outline" @click="showConfirmDeleteJourRegleDialog = false">
                Annuler
              </Button>
              <Button variant="destructive" @click="confirmDeleteJourRegle">
                Supprimer
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </TabsContent>
      <TabsContent value="symptomes">

        <!-- Quick-add acné aujourd'hui (visible si pas encore marquée aujourd'hui) -->
        <div v-if="!acneMarkedToday" class="container !mt-0 mx-auto w-full">
          <button
            @click="quickAddAcneToday"
            :disabled="isQuickAddingAcne"
            class="w-full flex items-center gap-3 bg-purple-50 border-2 border-purple-200 rounded-xl p-3 hover:bg-purple-100 transition-colors"
          >
            <span class="material-symbols-outlined text-purple-500 text-2xl shrink-0">face_retouching_natural</span>
            <span class="font-medium text-headline text-sm text-left flex-1">
              {{ isQuickAddingAcne ? 'Enregistrement...' : 'Acné aujourd\'hui ?' }}
            </span>
            <span class="material-symbols-outlined text-purple-400 text-base shrink-0">add_circle</span>
          </button>
        </div>

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
          <div class="flex justify-between items-center mb-4 flex-wrap gap-2 h-full">
            <h2 class="text-2xl flex gap-2 ml-2">
              <i class="material-symbols-outlined text-3xl ml-auto">monitor_heart</i>Symptômes
            </h2>
            <div class="form-modal">
              <Dialog v-model:open="showAddDialog">
                <DialogTrigger class="flex gap-2 items-center cursor-pointer hover:opacity-80 transition-opacity">
                  <Button variant="custom" @click="openAddSymptomeDialog">
                    <span class="hide-xsm">Ajouter une entrée</span>
                    <i class="material-symbols-outlined">add</i>
                  </Button>
                </DialogTrigger>
                <DialogContent>
                  <DialogHeader class="text-2xl">
                    <DialogTitle>{{ editingSymptomeId !== null ? 'Modifier le symptôme' : 'Ajouter un symptôme' }}</DialogTitle>
                  </DialogHeader>
                  <form class="flex flex-col gap-6" @submit.prevent="onSubmit">
                    <FormField v-slot="{ componentField }" name="typeSymptome">
                      <FormItem>
                        <FormLabel>Type</FormLabel>
                        <FormControl>
                          <Select v-model="form.values.typeSymptome" v-bind="componentField">
                            <SelectTrigger>
                              <SelectValue v-bind="componentField">
                                {{ form.values.typeSymptome || 'Sélectionner un type de symptôme' }}
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
                    <div v-if="!form.values.isPeriod" key="single-day-fields" class="flex items-center gap-8 cycle-single-day-fields">
                      <FormField v-slot="{ componentField }" name="date">
                        <FormItem>
                          <FormLabel>Date</FormLabel>
                          <FormControl>
                            <Input type="date" v-model="form.values.date" v-bind="componentField"/>
                          </FormControl>
                          <FormMessage/>
                        </FormItem>
                      </FormField>
                      <FormField v-slot="{ componentField }" name="time">
                        <FormItem>
                          <FormLabel>Heure</FormLabel>
                          <FormControl>
                            <Input type="time" v-model="form.values.time" v-bind="componentField"/>
                          </FormControl>
                          <FormMessage/>
                        </FormItem>
                      </FormField>
                    </div>

                    <!-- Period fields (shown when isPeriod is checked) -->
                    <div v-if="form.values.isPeriod" key="period-fields" class="flex flex-col gap-4">
                      <FormField v-slot="{ componentField }" name="dateDebut">
                        <FormItem>
                          <FormLabel>Date de début</FormLabel>
                          <FormControl>
                            <Input type="date" v-model="form.values.dateDebut" v-bind="componentField"/>
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

                      <FormField v-if="!form.values.enCours" v-slot="{ componentField }" name="dateFin">
                        <FormItem>
                          <FormLabel>Date de fin</FormLabel>
                          <FormControl>
                            <Input type="date" v-model="form.values.dateFin" v-bind="componentField"/>
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
                          <Input type="text" placeholder="Écrivez ici" v-model="form.values.commentaire" v-bind="componentField"/>
                        </FormControl>
                        <FormMessage/>
                      </FormItem>
                    </FormField>
                    <FormField name="photo">
                      <FormItem>
                        <FormLabel>Photo <span>(optionnel)</span></FormLabel>
                        <FormControl>
                          <Input
                              ref="photoInputRef"
                              type="file"
                              accept="image/*,.heic,.heif"
                              :disabled="isProcessingPhoto"
                              @change="handlePhotoUpload"
                          />
                        </FormControl>
                        <p v-if="isProcessingPhoto" class="text-xs text-gray-500">Traitement de la photo...</p>
                        <p class="text-xs text-gray-500">JPG, PNG, WEBP, HEIC - max {{ MAX_PHOTO_SIZE_MB }} MB</p>
                        <div v-if="selectedPhotoPreviewUrl" class="mt-2 flex items-center gap-3">
                          <img :src="selectedPhotoPreviewUrl" alt="Aperçu photo" class="w-16 h-16 rounded-md object-cover border" />
                          <Button type="button" variant="outline" size="sm" @click="resetSelectedPhoto">Retirer</Button>
                        </div>
                        <FormMessage />
                      </FormItem>
                    </FormField>


                    <Button type="submit" variant="custom" class="mt-4">
                      Enregistrer
                    </Button>
                  </form>
                </DialogContent>
              </Dialog>
            </div>
          </div>

          <SelectMonth v-model="symptomesMonthYear"/>

          <!-- KPI acné - visibles seulement quand filtre Acné ou Tous avec des entrées d'acné -->
          <div v-if="acneMonthlyStats.totalDays > 0" class="grid grid-cols-3 gap-2 mt-4 mb-4">
            <div class="bg-white border border-purple-100 rounded-xl p-3 shadow-sm text-center">
              <p class="text-xs text-muted-foreground mb-1">{{ acneMonthlyStats.totalDays > 1 ? 'Jours d\'acné' : 'Jour d\'acné' }}</p>
              <p class="text-xl font-bold text-headline">{{acneMonthlyStats.totalDays}}</p>
            </div>
            <div class="bg-white border border-purple-100 rounded-xl p-3 shadow-sm text-center">
              <p class="text-xs text-muted-foreground mb-1">Intensité moy.</p>
              <p class="text-xl font-bold text-headline">{{ acneMonthlyStats.avgIntensity }}</p>
            </div>
          </div>

          <!-- Filtres rapides -->
          <div class="flex gap-2 overflow-x-auto pb-1 mb-3">
            <Button
              v-for="filter in symptomFilters"
              :key="filter"
              size="sm"
              :variant="selectedSymptomeFilter === filter ? 'selected' : 'outline'"
              @click="selectedSymptomeFilter = filter"
            >
              {{ filter }}
            </Button>
          </div>

          <div v-if="isLoading" class="flex flex-col space-y-3">
            <Skeleton class="h-[120px] w-full mt-2 rounded-xl"/>
            <Skeleton class="h-[120px] w-full rounded-xl"/>
            <Skeleton class="h-[120px] w-full rounded-xl"/>
          </div>
          <template v-else-if="filteredProcessedEntries.length > 0">
            <!-- Cards sur mobile -->
            <div class="md:hidden">
              <GenericCardList
                :entries="filteredProcessedEntries"
                titleField="typeSymptome"
                dateField="date"
                timeField="time"
                intensityField="intensite"
                photoField="photoUrl"
                :extraFields="[{ key: 'commentaire', label: 'Note' }]"
                :iconConfig="symptomeIconConfig"
                :onDelete="handleDelete"
                :onEdit="handleEditSymptome"
                :onPhotoClick="openPhotoModal"
                emptyMessage="Aucun symptôme enregistré ce mois"
              />
            </div>
            <!-- Table sur desktop -->
            <div class="hidden md:block">
              <Datatable :entries="filteredProcessedEntries" :columns="columns" :deleteFunction="handleDelete" @edit-entry="handleEditSymptome">
                <thead>
                  <tr>
                    <th>Type</th><th>Date</th><th>Heure</th><th>Intensité</th><th>Commentaire</th><th></th><th></th>
                  </tr>
                </thead>
              </Datatable>
            </div>
          </template>
          <div v-else class="flex justify-center items-center h-32">
            <p class="text-xl text-center text-muted-foreground italic">Aucune donnée enregistrée</p>
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
import { type Ref, ref, onMounted, watch, computed, nextTick, onBeforeUnmount } from 'vue'
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
import SelectMonth from "@/shared/components/SelectMonth.vue"

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
import PeriodQuickAdd from "@/features/cycle/components/PeriodQuickAdd.vue"
import GenericCardList from "@/shared/components/GenericCardList.vue"
import { symptomeIconConfig } from '@/shared/config/materialSymbols'
import { preparePhotoForUpload } from '@/shared/utils/safeImageUpload'

type SymptomFilter = 'Tous' | 'Acné' | 'Spotting' | 'Nausée' | 'Fatigue' | 'Autre'

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
const joursReglesMap = ref<Map<string, number>>(new Map()) // date string 'yyyy-MM-dd' -> id
const joursOvulation = ref<Date[]>([])
const joursFertiles = ref<Date[]>([])
const joursSpotting = ref<Date[]>([])
const joursAcne = ref<Date[]>([])
const cycleMoyen = ref(28)
const selectedPhoto = ref<File | null>(null)
const photoInputRef = ref<HTMLInputElement | null>(null)
const selectedPhotoPreviewUrl = ref<string>('')
const isProcessingPhoto = ref(false)

// Quick-add acné
const isQuickAddingAcne = ref(false)
const acneMarkedToday = computed(() => {
  if (!entries.value) return false
  const today = format(new Date(), 'dd/MM/yyyy')
  return entries.value.some(e => e.typeSymptome === 'Acné' && e.date === today)
})

const quickAddAcneToday = async () => {
  if (!user?.carnetSanteId) return
  isQuickAddingAcne.value = true
  try {
    const formData = buildSymptomeFormData({
      typeSymptome: 'Acné',
      carnetSanteId: user.carnetSanteId,
      dateIso: combineDateTime(format(new Date(), 'yyyy-MM-dd'), format(new Date(), 'HH:mm')).toISOString(),
      intensite: 5,
      commentaire: 'Pas de commentaire',
    })
    await apiService.postDonneesSymptomesCycle(formData)
    await refetch()
    toast({ title: 'Acné enregistrée', description: 'Marquée pour aujourd\'hui', variant: 'custom' })
  } catch {
    toast({ title: 'Erreur', description: 'Impossible d\'enregistrer', variant: 'destructive' })
  } finally {
    isQuickAddingAcne.value = false
  }
}

// Edit symptôme — ouvre le dialog en mode édition
const editingSymptomeId = ref<number | null>(null)
const handleEditSymptome = (id: string | number) => {
  const found = processedEntries.value.find(e => e.id === id)
  if (!found) return

  if ((found as any).isGroup) {
    toast({
      title: 'Modification indisponible',
      description: 'Modifiez individuellement les symptômes simples. Pour une période d\'acné, utilisez les actions dédiées.',
      variant: 'destructive',
    })
    return
  }

  if (typeof found.id !== 'number') return

  editingSymptomeId.value = found.id
  const [day, mo, yr] = found.date.split('/').map(Number)
  const formDate = `${yr}-${String(mo).padStart(2, '0')}-${String(day).padStart(2, '0')}`
  const formTime = found.time?.replace('h', ':') ?? format(new Date(), 'HH:mm')

  form.setValues({
    typeSymptome: found.typeSymptome,
    date: formDate,
    time: formTime,
    intensite: [typeof found.intensite === 'number' ? found.intensite : Number(found.intensite)],
    commentaire: found.commentaire === 'Pas de commentaire' ? '' : found.commentaire,
    isPeriod: false,
    enCours: false,
    dateDebut: '',
    dateFin: '',
  })

  resetSelectedPhoto()
  showAddDialog.value = true
}

const MAX_PHOTO_SIZE_MB = 10
const MAX_PHOTO_SIZE_BYTES = MAX_PHOTO_SIZE_MB * 1024 * 1024

const ALLOWED_PHOTO_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp', '.heic', '.heif']

const getFileExtension = (fileName: string): string => {
  const index = fileName.lastIndexOf('.')
  if (index < 0) return ''
  return fileName.substring(index).toLowerCase()
}

const inferExtensionFromType = (mimeType: string): string => {
  const normalizedType = mimeType.trim().toLowerCase()
  switch (normalizedType) {
    case 'image/jpeg':
    case 'image/jpg':
    case 'image/pjpeg':
      return '.jpg'
    case 'image/png':
      return '.png'
    case 'image/webp':
      return '.webp'
    case 'image/heic':
    case 'image/heic-sequence':
      return '.heic'
    case 'image/heif':
    case 'image/heif-sequence':
      return '.heif'
    default:
      return ''
  }
}

const isAcceptedImageFile = (file: File): boolean => {
  const extension = getFileExtension(file.name)
  if (extension && ALLOWED_PHOTO_EXTENSIONS.includes(extension)) {
    return true
  }

  return !!inferExtensionFromType(file.type)
}

const getUploadFileName = (file: File): string => {
  const baseName = file.name.replace(/\.[^.]+$/, '').trim() || `photo-${Date.now()}`
  const extension = getFileExtension(file.name) || inferExtensionFromType(file.type) || '.jpg'
  return `${baseName}${extension}`
}

const resetSelectedPhoto = () => {
  selectedPhoto.value = null

  if (selectedPhotoPreviewUrl.value) {
    URL.revokeObjectURL(selectedPhotoPreviewUrl.value)
    selectedPhotoPreviewUrl.value = ''
  }

  if (photoInputRef.value) {
    photoInputRef.value.value = ''
  }
}

const handlePhotoUpload = async (event: Event) => {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0] ?? null

  if (!file) {
    resetSelectedPhoto()
    return
  }

  if (!isAcceptedImageFile(file)) {
    toast({
      title: 'Format non supporté',
      description: 'Veuillez choisir une image (jpg, png, webp, heic).',
      variant: 'destructive',
    })
    resetSelectedPhoto()
    return
  }

  isProcessingPhoto.value = true

  try {
    const preparedPhoto = await preparePhotoForUpload(file)

    if (preparedPhoto.convertedFromHeic) {
      toast({
        title: 'Photo optimisée',
        description: 'Votre photo HEIC a été convertie en JPG pour garantir la compatibilité.',
        variant: 'custom',
      })
    }

    if (preparedPhoto.file.size > MAX_PHOTO_SIZE_BYTES) {
      toast({
        title: 'Photo trop volumineuse',
        description: `La photo doit faire moins de ${MAX_PHOTO_SIZE_MB} MB.`,
        variant: 'destructive',
      })
      resetSelectedPhoto()
      return
    }

    if (selectedPhotoPreviewUrl.value) {
      URL.revokeObjectURL(selectedPhotoPreviewUrl.value)
    }

    selectedPhoto.value = preparedPhoto.file
    selectedPhotoPreviewUrl.value = URL.createObjectURL(preparedPhoto.file)
  } catch {
    toast({
      title: 'Erreur de traitement',
      description: 'Impossible de préparer la photo pour l\'upload. Réessayez avec une autre image.',
      variant: 'destructive',
    })
    resetSelectedPhoto()
  } finally {
    isProcessingPhoto.value = false
  }
}

const buildSymptomeFormData = (payload: {
  id?: number
  typeSymptome: string
  carnetSanteId: number
  dateIso: string
  intensite: number
  commentaire?: string
  photo?: File | null
}) => {
  const formData = new FormData()

  if (payload.id !== undefined) {
    formData.append('id', payload.id.toString())
  }

  formData.append('typeSymptome', payload.typeSymptome)
  formData.append('carnetSanteId', payload.carnetSanteId.toString())
  formData.append('date', payload.dateIso)
  formData.append('intensite', payload.intensite.toString())
  formData.append('commentaire', payload.commentaire || 'Pas de commentaire')

  if (payload.photo) {
    formData.append('photo', payload.photo, getUploadFileName(payload.photo))
  }

  return formData
}

const { selectedMonthYear: symptomesMonthYear, entries, isLoading, refetch } = useMonthData<SymptomeCycle>({
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

const { deleteEntry } = useCrudOperations(entries)

const getDefaultSymptomeFormValues = () => ({
  typeSymptome: '',
  date: format(new Date(), 'yyyy-MM-dd'),
  time: format(new Date(), 'HH:mm'),
  intensite: [5],
  commentaire: '',
  isPeriod: false,
  dateDebut: '',
  dateFin: '',
  enCours: false
})

const resetSymptomeFormState = () => {
  editingSymptomeId.value = null
  resetSelectedPhoto()
  form.resetForm({ values: getDefaultSymptomeFormValues() })
}

const openAddSymptomeDialog = () => {
  resetSymptomeFormState()
}

// Confirmation dialog for calendar clicks
const showConfirmJourRegleDialog = ref(false)
const showConfirmDeleteJourRegleDialog = ref(false)
const pendingJourRegleDate = ref<string>('')
const pendingDeleteJourRegleId = ref<number | null>(null)
const selectedSymptomeFilter = ref<SymptomFilter>('Tous')
const symptomFilters: SymptomFilter[] = ['Tous', 'Acné', 'Spotting', 'Nausée', 'Fatigue', 'Autre']

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
        photoUrl: acneGroup[0].photoUrl || '',
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

const filteredProcessedEntries = computed(() => {
  if (selectedSymptomeFilter.value === 'Tous') {
    return processedEntries.value
  }

  return processedEntries.value.filter((entry: any) => entry.typeSymptome === selectedSymptomeFilter.value)
})

const acneMonthlyStats = computed(() => {
  const acneEntries = entries.value.filter((entry: any) => entry.typeSymptome === 'Acné')
  const totalDays = acneEntries.length
  const withPhotos = acneEntries.filter((entry: any) => !!entry.photoUrl).length

  if (totalDays === 0) {
    return {
      totalDays: 0,
      avgIntensity: '-',
      withPhotos: 0
    }
  }

  const avg = acneEntries.reduce((sum: number, entry: any) => sum + Number(entry.intensite || 0), 0) / totalDays

  return {
    totalDays,
    avgIntensity: avg % 1 === 0 ? avg.toString() : avg.toFixed(1),
    withPhotos
  }
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

const handlePhotoIconClick = (e: Event) => {
  const target = e.target as HTMLElement
  if (target.classList.contains('photo-icon') && target.dataset.url) {
    openPhotoModal(target.dataset.url)
  }
}

onMounted(() => {
  nextTick(() => {
    document.addEventListener('click', handlePhotoIconClick)
  })
})

onBeforeUnmount(() => {
  document.removeEventListener('click', handlePhotoIconClick)
  if (selectedPhotoPreviewUrl.value) {
    URL.revokeObjectURL(selectedPhotoPreviewUrl.value)
  }
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
    const joursList: { id: number; date: string }[] = response.$values
    joursRegles.value = joursList.map(jour => new Date(jour.date))

    // Build map date -> id for deletion
    const newMap = new Map<string, number>()
    joursList.forEach(jour => {
      newMap.set(format(new Date(jour.date), 'yyyy-MM-dd'), jour.id)
    })
    joursReglesMap.value = newMap

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
  }).min(1, 'Le type de symptôme est requis'),
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
  if (data.isPeriod) {
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
  initialValues: getDefaultSymptomeFormValues()
})

watch(showAddDialog, (isOpen) => {
  if (!isOpen) {
    resetSymptomeFormState()
  }
})

const onSubmit = form.handleSubmit(async (values) => {
  // Handle period submission (multiple days)
  if (values.isPeriod) {
    const endDate = values.enCours ? new Date() : new Date(values.dateFin ?? '')
    const startDate = new Date(values.dateDebut ?? '')

    // Validate dates
    if (startDate > endDate && !values.enCours) {
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

    let firstDayResponse = null

    try {
      if (selectedPhoto.value) {
        const firstDate = dates[0]
        const firstDayFormData = buildSymptomeFormData({
          typeSymptome: values.typeSymptome,
          carnetSanteId: user!.carnetSanteId,
          dateIso: combineDateTime(format(firstDate, 'yyyy-MM-dd'), '12:00').toISOString(),
          intensite: values.intensite[0],
          commentaire: values.commentaire,
          photo: selectedPhoto.value
        })

        firstDayResponse = await apiService.postDonneesSymptomesCycle(firstDayFormData)

        if (dates.length === 1) {
          await refetch()
          toast({
            title: 'Succès',
            description: `Période ajoutée (${values.typeSymptome}, 1 jour)`,
            variant: 'custom',
          })

          resetSymptomeFormState()
          showAddDialog.value = false
          return
        }

        const remainingDates = dates.slice(1)
        const remainingPromises = remainingDates.map(date => {
          const formData = buildSymptomeFormData({
            typeSymptome: values.typeSymptome,
            carnetSanteId: user!.carnetSanteId,
            dateIso: combineDateTime(format(date, 'yyyy-MM-dd'), '12:00').toISOString(),
            intensite: values.intensite[0],
            commentaire: values.commentaire
          })
          return apiService.postDonneesSymptomesCycle(formData)
        })

        await Promise.all(remainingPromises)
      } else {
        const promises = dates.map(date => {
          const formData = buildSymptomeFormData({
            typeSymptome: values.typeSymptome,
            carnetSanteId: user!.carnetSanteId,
            dateIso: combineDateTime(format(date, 'yyyy-MM-dd'), '12:00').toISOString(),
            intensite: values.intensite[0],
            commentaire: values.commentaire
          })
          return apiService.postDonneesSymptomesCycle(formData)
        })

        await Promise.all(promises)
      }

      await refetch()

      toast({
        title: 'Succès',
        description: `Période ajoutée (${values.typeSymptome}, ${dates.length} jour${dates.length > 1 ? 's' : ''})`,
        variant: 'custom',
      })

      // Reset and close
      resetSymptomeFormState()
      showAddDialog.value = false
    } catch (error) {
      console.error('Error creating period:', error)

      const errorMessage = selectedPhoto.value && firstDayResponse
        ? 'La première journée a été ajoutée mais les jours suivants ont échoué. Veuillez réessayer pour les jours manquants.'
        : 'Une erreur est survenue lors de l\'ajout de la période'

      toast({
        title: 'Erreur',
        description: errorMessage,
        variant: 'destructive',
      })

      if (firstDayResponse) {
        await refetch()
      }
    }
  } else {
    // Mode édition ou ajout d'un seul jour
    if (editingSymptomeId.value !== null) {
      // Edit mode
      const dataToSend = buildSymptomeFormData({
        id: editingSymptomeId.value,
        typeSymptome: values.typeSymptome,
        carnetSanteId: user!.carnetSanteId,
        dateIso: combineDateTime(values.date ?? '', values.time ?? '').toISOString(),
        intensite: values.intensite[0],
        commentaire: values.commentaire || 'Pas de commentaire',
        photo: selectedPhoto.value
      })

      submitForm(dataToSend, {
        submitFunction: (data) => apiService.editSymptomeCycle(editingSymptomeId.value as number, data),
        successMessage: 'Symptôme modifié avec succès',
        errorMessage: 'Une erreur est survenue lors de la modification',
        onSuccess: async () => {
          await refetch()
        },
        resetFormData: () => {
          resetSymptomeFormState()
        }
      })
    } else {
      // Create mode
      const formData = buildSymptomeFormData({
        typeSymptome: values.typeSymptome,
        carnetSanteId: user!.carnetSanteId,
        dateIso: combineDateTime(values.date ?? '', values.time ?? '').toISOString(),
        intensite: values.intensite[0],
        commentaire: values.commentaire,
        photo: selectedPhoto.value
      })
      submitForm(formData, {
        submitFunction: (data) => apiService.postDonneesSymptomesCycle(data),
        successMessage: 'Symptôme ajouté avec succès',
        errorMessage: 'Une erreur est survenue lors de l\'ajout du symptôme',
        onSuccess: async () => {
          await refetch()
        },
        resetFormData: () => {
          resetSymptomeFormState()
        }
      })
    }
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

const handleMarkPeriod = async (dateString: string) => {
  const data = { date: dateString, carnetSanteId: user?.carnetSanteId }

  await handleOfflineOperation(
    () => apiService.postJourRegle(data),
    {
      endpoint: 'JourRegle',
      method: 'POST',
      data: data,
      onSuccess: () => {
        periodMarked.value = true
        fetchJoursRegles()
      },
      onOfflineQueued: () => {
        periodMarked.value = true
      },
      successMessage: 'Règles enregistrées',
      errorMessage: 'Impossible de marquer les règles',
    }
  )
}

const onPeriodMarked = () => {
  fetchJoursRegles()
}

const extendAcnePeriod = async (period: any) => {
  const today = format(new Date(), 'yyyy-MM-dd')

  // Get the last entry from the period to use its intensity and comment
  const lastEntry = period.entries[period.entries.length - 1]

  if (!user?.carnetSanteId) {
    return
  }

  const data = buildSymptomeFormData({
    typeSymptome: 'Acné',
    carnetSanteId: user.carnetSanteId,
    dateIso: combineDateTime(today, '12:00').toISOString(),
    intensite: Number(lastEntry.intensite),
    commentaire: lastEntry.commentaire
  })

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
    // Propose to unmark it
    const jourId = joursReglesMap.value.get(clickedDate)
    if (jourId !== undefined) {
      pendingJourRegleDate.value = clickedDate
      pendingDeleteJourRegleId.value = jourId
      showConfirmDeleteJourRegleDialog.value = true
    }
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

const confirmDeleteJourRegle = async () => {
  if (pendingDeleteJourRegleId.value === null) return

  try {
    await apiService.deleteJourRegle(pendingDeleteJourRegleId.value)
    await fetchJoursRegles()

    // If today was unmarked, update periodMarked
    const todayStr = format(new Date(), 'yyyy-MM-dd')
    if (pendingJourRegleDate.value === todayStr) {
      periodMarked.value = false
    }

    toast({
      title: 'Jour retiré',
      description: 'Le jour de règles a été supprimé',
      variant: 'custom',
    })
  } catch (error) {
    console.error('Erreur lors de la suppression du jour de règles:', error)
    toast({
      title: 'Erreur',
      description: 'Impossible de supprimer ce jour',
      variant: 'destructive',
    })
  } finally {
    showConfirmDeleteJourRegleDialog.value = false
    pendingDeleteJourRegleId.value = null
  }
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

@media (max-width: 425px) {
  .cycle-tabs-list {
    width: 100%;
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.5rem;
  }

  .cycle-month-wrapper {
    max-width: 100% !important;
    width: 100%;
  }

  .cycle-month-controls {
    width: 100%;
    justify-content: space-between;
    gap: 0.5rem;
  }

  .cycle-month-controls input[type="month"] {
    width: 100%;
    max-width: none;
    min-width: 0;
  }

  .cycle-legend {
    flex-wrap: wrap;
    justify-content: flex-start;
    gap: 0.75rem;
  }

  .cycle-legend-item {
    width: calc(50% - 0.5rem);
  }

  .cycle-single-day-fields {
    flex-direction: column;
    align-items: stretch;
    gap: 0.75rem;
  }
}
</style>
