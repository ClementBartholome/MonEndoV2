<template>
  <div class="flex-column-container">
    <BackButton/>
    <div class="flex flex-col self-baseline w-full">
      <h2 class="text-2xl flex gap-2"><i class="material-symbols-outlined text-3xl">menstrual_health</i>Cycle menstruel
      </h2>
    </div>
    <Tabs v-model="activeTab" class="w-full">
      <TabsList class="cycle-tabs-list">
        <TabsTrigger value="cycles">Mes cycles</TabsTrigger>
        <TabsTrigger value="symptomes">Symptômes</TabsTrigger>
        <TabsTrigger value="acne">Acné</TabsTrigger>
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

        <!-- Regular Symptoms Section -->
        <section class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
          <div class="flex justify-between items-center mb-4 flex-wrap gap-2 h-full">
            <h2 class="text-2xl flex gap-2 ml-2">
              <i class="material-symbols-outlined text-3xl ml-auto">monitor_heart</i>Symptômes
            </h2>
            <div class="form-modal">
              <Button variant="custom" @click="openAddSymptomeDialog">
                <span class="hide-xsm">Ajouter une entrée</span>
                <i class="material-symbols-outlined">add</i>
              </Button>
            </div>
          </div>

          <SelectMonth v-model="symptomesMonthYear"/>

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
          <template v-else-if="filteredNonAcneEntries.length > 0">
            <!-- Cards sur mobile -->
            <div class="md:hidden">
              <GenericCardList
                :entries="filteredNonAcneEntries"
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
              <Datatable :entries="filteredNonAcneEntries" :columns="columns" :deleteFunction="handleDelete" @edit-entry="handleEditSymptome">
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

      <TabsContent value="acne">
        <AcneTabSection
          :carnetSanteId="user!.carnetSanteId"
          :refreshKey="acneRefreshKey"
          @open-add="openAddAcneDialog"
          @edit-entry="handleEditAcneEntry"
          @photo-click="openPhotoModal"
          @changed="handleAcneChanged"
        />
      </TabsContent>

      <!-- Photo Modal -->
      <Dialog v-model:open="showPhotoModal">
        <DialogContent class="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Photo</DialogTitle>
          </DialogHeader>
          <div class="flex justify-center">
            <img v-if="selectedPhotoUrl" :src="selectedPhotoUrl" alt="Photo symptôme" class="max-w-full max-h-[80vh] rounded-lg object-contain" />
          </div>
        </DialogContent>
      </Dialog>

      <!-- Add/Edit Symptom Dialog -->
      <Dialog v-model:open="showAddDialog">
        <DialogContent class="max-h-[85vh] overflow-y-auto">
          <DialogHeader class="text-2xl">
            <DialogTitle>{{ editingSymptomeId !== null ? 'Modifier le symptôme' : 'Ajouter un symptôme' }}</DialogTitle>
          </DialogHeader>
          <form class="flex flex-col gap-6" @submit.prevent="onSubmit">
            <FormField v-if="!isAcneOnlyDialog" v-slot="{ componentField }" name="typeSymptome">
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
                        <SelectItem value="Autre">Autre</SelectItem>
                      </SelectGroup>
                    </SelectContent>
                  </Select>
                </FormControl>
                <FormMessage/>
              </FormItem>
            </FormField>

            <div v-else class="rounded-md border border-purple-200 bg-purple-50 px-3 py-2 text-sm text-purple-900 inline-flex items-center gap-2">
              <i class="material-symbols-outlined text-base">face</i>
              <span>Type: Acné</span>
            </div>

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

            <div v-if="!form.values.isPeriod" key="single-day-fields" class="grid grid-cols-2 gap-3">
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
                  <div class="flex flex-col gap-3">
                    <div class="grid grid-cols-2 gap-2">
                      <label
                        for="cameraPhotoInput"
                        class="inline-flex items-center justify-center gap-2 rounded-md border border-input bg-background px-3 py-2 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground"
                        :class="isProcessingPhoto ? 'pointer-events-none opacity-50' : 'cursor-pointer'"
                      >
                        <i class="material-symbols-outlined text-base">photo_camera</i>
                        <span>Prendre une photo</span>
                      </label>
                      <label
                        for="galleryPhotoInput"
                        class="inline-flex items-center justify-center gap-2 rounded-md border border-input bg-background px-3 py-2 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground"
                        :class="isProcessingPhoto ? 'pointer-events-none opacity-50' : 'cursor-pointer'"
                      >
                        <i class="material-symbols-outlined text-base">photo_library</i>
                        <span>Galerie</span>
                      </label>
                    </div>
                    <input
                      id="cameraPhotoInput"
                      ref="cameraPhotoInputRef"
                      class="hidden"
                      type="file"
                      accept="image/*"
                      capture="environment"
                      @change="(event) => handlePhotoUpload(event, 'camera')"
                    />
                    <input
                      id="galleryPhotoInput"
                      ref="galleryPhotoInputRef"
                      class="hidden"
                      type="file"
                      accept="image/*,.heic,.heif"
                      @change="(event) => handlePhotoUpload(event, 'gallery')"
                    />
                  </div>
                </FormControl>
                <p v-if="isProcessingPhoto" class="text-xs text-gray-500">Traitement de la photo...</p>
                <p class="text-xs text-gray-500">JPG, PNG, WEBP, HEIC - max {{ MAX_PHOTO_SIZE_LABEL }}</p>
                <p v-if="selectedPhoto" class="text-xs text-gray-500">Fichier sélectionné : {{ selectedPhoto.name }} ({{ formatFileSize(selectedPhoto.size) }})</p>
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
    </Tabs>
  </div>
</template>

<script setup lang="ts">
import { Calendar } from '@/shared/components/ui/calendar'
import { type DateValue, getLocalTimeZone, today, parseDate } from '@internationalized/date'
import { type Ref, ref, onMounted, watch, computed, nextTick, onBeforeUnmount } from 'vue'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/shared/components/ui/dialog"
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
import { format } from 'date-fns'
import type { SymptomeCycle } from "@/features/cycle/types/symptome-cycle"
import {Checkbox} from "@/shared/components/ui/checkbox";
import PeriodQuickAdd from "@/features/cycle/components/PeriodQuickAdd.vue"
import AcneTabSection from "@/features/cycle/components/AcneTabSection.vue"
import GenericCardList from "@/shared/components/GenericCardList.vue"
import { symptomeIconConfig } from '@/shared/config/materialSymbols'
import { preparePhotoForUpload } from '@/shared/utils/safeImageUpload'

type SymptomFilter = 'Tous' | 'Acné' | 'Spotting' | 'Nausée' | 'Fatigue' | 'Autre'

const { user } = useAuthStore()
const { formatDateDisplay, formatTimeDisplay, combineDateTime, getCurrentMonthYear } = useDateTimeFormat()
const { toast } = useToast()
const activeTab = ref<'cycles' | 'symptomes' | 'acne'>('cycles')
const isAcneOnlyDialog = computed(() => activeTab.value === 'acne')

const getRequestErrorMessage = (error: unknown, fallback: string): string => {
  const anyError = error as any
  const statusCode = anyError?.response?.status
  const responseData = anyError?.response?.data

  if (statusCode === 413) {
    return 'Photo trop volumineuse pour le serveur. Réduisez la taille ou choisissez une autre photo.'
  }

  if (typeof responseData?.message === 'string' && responseData.message.length > 0) {
    return responseData.message
  }

  if (typeof responseData?.title === 'string' && responseData.title.length > 0) {
    return responseData.title
  }

  if (typeof anyError?.message === 'string' && anyError.message.length > 0) {
    return anyError.message
  }

  return fallback
}

const formatFileSize = (bytes: number): string => {
  if (!Number.isFinite(bytes) || bytes < 1024) {
    return `${Math.max(0, Math.round(bytes || 0))} B`
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`
  }

  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`
}

// Dialog control
const showAddDialog = ref(false)
const { submitForm } = useDialogForm(showAddDialog)

const showPhotoModal = ref(false)
const selectedPhotoUrl = ref('')
const acneRefreshKey = ref(0)

const notifyAcneRefresh = () => {
  acneRefreshKey.value += 1
}

const refreshAfterSymptomeMutation = async () => {
  await refetch()
  notifyAcneRefresh()
}

const handleAcneChanged = async () => {
  await refetch()
}

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
const cameraPhotoInputRef = ref<HTMLInputElement | null>(null)
const galleryPhotoInputRef = ref<HTMLInputElement | null>(null)
const selectedPhotoPreviewUrl = ref<string>('')
const isProcessingPhoto = ref(false)
const selectedPhotoSource = ref<'camera' | 'gallery' | null>(null)

// Edit symptôme — ouvre le dialog en mode édition
const editingSymptomeId = ref<number | null>(null)
const openEditDialogFromEntry = (found: any) => {
  if (!found) return

  if (found.typeSymptome === 'Acné') {
    activeTab.value = 'acne'
  }

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

const handleEditSymptome = (id: string | number) => {
  const found = processedEntries.value.find(e => e.id === id)
  openEditDialogFromEntry(found)
}

const handleEditAcneEntry = (entry: any) => {
  openEditDialogFromEntry(entry)
}

const MAX_PHOTO_SIZE_BYTES = 900 * 1024
const HARD_INPUT_MAX_BYTES = 20 * 1024 * 1024
const MAX_PHOTO_SIZE_LABEL = '900 KB'

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
  selectedPhotoSource.value = null

  if (selectedPhotoPreviewUrl.value) {
    URL.revokeObjectURL(selectedPhotoPreviewUrl.value)
    selectedPhotoPreviewUrl.value = ''
  }

  if (cameraPhotoInputRef.value) {
    cameraPhotoInputRef.value.value = ''
  }

  if (galleryPhotoInputRef.value) {
    galleryPhotoInputRef.value.value = ''
  }
}

const handlePhotoUpload = async (event: Event, source: 'camera' | 'gallery') => {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0] ?? null

  if (!file) {
    return
  }

  if (file.size === 0) {
    toast({
      title: 'Photo invalide',
      description: 'La photo sélectionnée est vide ou incomplète. Réessayez.',
      variant: 'destructive',
    })
    resetSelectedPhoto()
    return
  }

  if (file.size > HARD_INPUT_MAX_BYTES) {
    toast({
      title: 'Photo trop volumineuse',
      description: `Le fichier dépasse ${formatFileSize(HARD_INPUT_MAX_BYTES)}. Prenez une photo plus légère.`,
      variant: 'destructive',
    })
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
    const preparedPhoto = await preparePhotoForUpload(file, {
      targetMaxBytes: MAX_PHOTO_SIZE_BYTES,
    })

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
        description: `La photo doit faire moins de ${MAX_PHOTO_SIZE_LABEL}. Taille actuelle: ${formatFileSize(preparedPhoto.file.size)}.`,
        variant: 'destructive',
      })
      resetSelectedPhoto()
      return
    }

    if (selectedPhotoPreviewUrl.value) {
      URL.revokeObjectURL(selectedPhotoPreviewUrl.value)
    }

    selectedPhoto.value = preparedPhoto.file
    selectedPhotoSource.value = source
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
  photoSource?: 'camera' | 'gallery' | null
}) => {
  const formData = new FormData()

  if (payload.id !== undefined) {
    formData.append('id', payload.id.toString())
  }

  formData.append('typeSymptome', payload.typeSymptome)
  formData.append('carnetSanteId', payload.carnetSanteId.toString())
  formData.append('date', payload.dateIso)
  formData.append('intensite', payload.intensite.toString())
  formData.append('commentaire', payload.commentaire || '')

  if (payload.photo) {
    formData.append('photo', payload.photo, getUploadFileName(payload.photo))
  }

  if (payload.photoSource) {
    formData.append('photoSource', payload.photoSource)
  }

  return formData
}

const mapSymptomeToViewModel = (symptomeCycle: SymptomeCycle) => ({
  id: symptomeCycle.id,
  typeSymptome: symptomeCycle.typeSymptome,
  date: formatDateDisplay(symptomeCycle.date),
  time: formatTimeDisplay(symptomeCycle.date),
  intensite: symptomeCycle.intensite,
  commentaire: symptomeCycle.commentaire || '',
  photoUrl: symptomeCycle.photoUrl || ''
})

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

    return response.map((symptomeCycle: SymptomeCycle) => mapSymptomeToViewModel(symptomeCycle))
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
  activeTab.value = 'symptomes'
  resetSymptomeFormState()
  showAddDialog.value = true
}

const openAddAcneDialog = () => {
  resetSymptomeFormState()
  activeTab.value = 'acne'
  form.setValues({
    ...getDefaultSymptomeFormValues(),
    typeSymptome: 'Acné',
  })
  showAddDialog.value = true
}

// Confirmation dialog for calendar clicks
const showConfirmJourRegleDialog = ref(false)
const showConfirmDeleteJourRegleDialog = ref(false)
const pendingJourRegleDate = ref<string>('')
const pendingDeleteJourRegleId = ref<number | null>(null)
const selectedSymptomeFilter = ref<SymptomFilter>('Tous')
const symptomFilters: SymptomFilter[] = ['Tous', 'Spotting', 'Nausée', 'Fatigue', 'Autre']

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

const processedEntries = computed(() => entries.value ?? [])

const filteredProcessedEntries = computed(() => {
  if (selectedSymptomeFilter.value === 'Tous') {
    return processedEntries.value
  }

  return processedEntries.value.filter((entry: any) => entry.typeSymptome === selectedSymptomeFilter.value)
})

const filteredNonAcneEntries = computed(() => {
  return filteredProcessedEntries.value.filter((entry: any) => entry.typeSymptome !== 'Acné')
})


const calendarValue = computed({
  get: () => {
    const [selectedYear, selectedMonth] = selectedMonthYear.value.split('-').map(Number)
    return parseDate(`${selectedYear}-${String(selectedMonth).padStart(2, '0')}-01`)
  },
  set: (newValue) => {
    if (!newValue) return
    selectedMonthYear.value = `${newValue.year}-${String(newValue.month).padStart(2, '0')}`
  }
})

const previousMonth = () => {
  const [selectedYear, selectedMonth] = selectedMonthYear.value.split('-').map(Number)
  selectedMonthYear.value = format(new Date(selectedYear, selectedMonth - 2, 1), 'yyyy-MM')
}

const nextMonth = () => {
  const [selectedYear, selectedMonth] = selectedMonthYear.value.split('-').map(Number)
  selectedMonthYear.value = format(new Date(selectedYear, selectedMonth, 1), 'yyyy-MM')
}

watch(calendarValue, (value) => {
  month.value = value.month
  year.value = value.year
  calculateFertilePeriodsForMonth(joursRegles.value)
  refetch()
})

const handlePhotoIconClick = (event: Event) => {
  const target = event.target as HTMLElement
  if (target.classList.contains('photo-icon') && target.dataset.url) {
    openPhotoModal(target.dataset.url)
  }
}

onMounted(() => {
  fetchJoursRegles()
  refetch()
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
  } finally {
    // Always stop the loading skeleton to avoid blocking the period quick add UI.
    isLoadingPeriod.value = false
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
  if (typeof id !== 'number') {
    return
  }

  await deleteEntry(id, (entryId) => apiService.deleteSymptomeCycle(entryId as number), {
    successMessage: 'Symptôme supprimé avec succès',
    errorMessage: 'Une erreur est survenue lors de la suppression du symptôme',
    endpoint: 'SymptomesCycle'
  })

  await refreshAfterSymptomeMutation()
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
  const selectedType = isAcneOnlyDialog.value ? 'Acné' : values.typeSymptome

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
          typeSymptome: selectedType,
          carnetSanteId: user!.carnetSanteId,
          dateIso: combineDateTime(format(firstDate, 'yyyy-MM-dd'), '12:00').toISOString(),
          intensite: values.intensite[0],
          commentaire: values.commentaire,
          photo: selectedPhoto.value,
          photoSource: selectedPhotoSource.value
        })

        firstDayResponse = await apiService.postDonneesSymptomesCycle(firstDayFormData)

        if (dates.length === 1) {
          await refreshAfterSymptomeMutation()
          toast({
            title: 'Succès',
            description: `Période ajoutée (${selectedType}, 1 jour)`,
            variant: 'custom',
          })

          resetSymptomeFormState()
          showAddDialog.value = false
          return
        }

        const remainingDates = dates.slice(1)
        const remainingPromises = remainingDates.map(date => {
          const formData = buildSymptomeFormData({
            typeSymptome: selectedType,
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
            typeSymptome: selectedType,
            carnetSanteId: user!.carnetSanteId,
            dateIso: combineDateTime(format(date, 'yyyy-MM-dd'), '12:00').toISOString(),
            intensite: values.intensite[0],
            commentaire: values.commentaire
          })
          return apiService.postDonneesSymptomesCycle(formData)
        })

        await Promise.all(promises)
      }

      await refreshAfterSymptomeMutation()

      toast({
        title: 'Succès',
        description: `Période ajoutée (${selectedType}, ${dates.length} jour${dates.length > 1 ? 's' : ''})`,
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
        description: getRequestErrorMessage(error, errorMessage),
        variant: 'destructive',
      })

      if (firstDayResponse) {
        await refreshAfterSymptomeMutation()
      }
    }
  } else {
    // Mode édition ou ajout d'un seul jour
    if (editingSymptomeId.value !== null) {
      // Edit mode
      const dataToSend = buildSymptomeFormData({
        id: editingSymptomeId.value,
        typeSymptome: selectedType,
        carnetSanteId: user!.carnetSanteId,
        dateIso: combineDateTime(values.date ?? '', values.time ?? '').toISOString(),
        intensite: values.intensite[0],
        commentaire: values.commentaire || '',
        photo: selectedPhoto.value,
        photoSource: selectedPhotoSource.value
      })

      submitForm(dataToSend, {
        submitFunction: (data) => apiService.editSymptomeCycle(editingSymptomeId.value as number, data),
        successMessage: 'Symptôme modifié avec succès',
        errorMessage: 'Une erreur est survenue lors de la modification',
        onSuccess: async () => {
          await refreshAfterSymptomeMutation()
        },
        resetFormData: () => {
          resetSymptomeFormState()
        }
      })
    } else {
      // Create mode
      const formData = buildSymptomeFormData({
        typeSymptome: selectedType,
        carnetSanteId: user!.carnetSanteId,
        dateIso: combineDateTime(values.date ?? '', values.time ?? '').toISOString(),
        intensite: values.intensite[0],
        commentaire: values.commentaire,
        photo: selectedPhoto.value,
        photoSource: selectedPhotoSource.value
      })
      submitForm(formData, {
        submitFunction: (data) => apiService.postDonneesSymptomesCycle(data),
        successMessage: 'Symptôme ajouté avec succès',
        errorMessage: 'Une erreur est survenue lors de l\'ajout du symptôme',
        onSuccess: async () => {
          await refreshAfterSymptomeMutation()
        },
        resetFormData: () => {
          resetSymptomeFormState()
        }
      })
    }
  }
})

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

@media (max-width: 425px) {
  .cycle-tabs-list {
    width: 100%;
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
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

}
</style>
