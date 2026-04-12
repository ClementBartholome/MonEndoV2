<template>
  <div class="flex-column-container">
    <BackButton/>
    <section class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
      <div class="flex justify-between items-center mb-4 medicament-section-header">
        <h2 class="text-2xl flex gap-2 ml-2"><i class="material-symbols-outlined text-3xl">medication</i>
          Prises de médicaments</h2>
        <div class="form-modal">
          <Dialog v-model:open="showAddPriseDialog">
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
                          <SelectValue placeholder="Sélectionner un médicament"/>
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
                    <div v-if="medicaments.length === 0" class="mt-3">
                      <Button type="button" variant="outline" size="sm" @click="openAddMedicamenteuxDialog">
                        Créer un traitement médicamenteux
                      </Button>
                    </div>
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
                <div class="flex items-center gap-8 prise-date-time-row">
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
                <Button class="mt-4" variant="custom" type="submit" :disabled="!isPriseFormValid">
                  Enregistrer
                </Button>
              </form>
            </DialogContent>
          </Dialog>
        </div>
      </div>
      <SelectMonth v-model="selectedMonthYear"/>
      <div class="grid grid-cols-2 gap-2 mt-4 mb-2">
        <div class="bg-white border border-indigo-100 rounded-xl p-3 text-center">
          <p class="text-xs text-muted-foreground">Prises ce mois</p>
          <p class="text-xl font-bold text-headline">{{ prisesStats.totalPrises }}</p>
        </div>
        <div class="bg-white border border-indigo-100 rounded-xl p-3 text-center">
          <p class="text-xs text-muted-foreground">Comprimés totaux</p>
          <p class="text-xl font-bold text-headline">{{ prisesStats.totalComprimes }}</p>
        </div>
      </div>
      <div v-if="isLoading" class="flex flex-col space-y-3">
        <Skeleton class="h-[300px] w-full mt-4 rounded-xl"/>
      </div>
      <template v-else-if="listePrises.length > 0">
        <div class="md:hidden">
          <GenericCardList
            :entries="listePrises"
            titleField="nom"
            dateField="date"
            timeField="time"
            :extraFields="[{ key: 'nombreComprimes', label: 'Comprimés' }, { key: 'commentaire', label: 'Note' }]"
            :iconConfig="priseIconConfig"
            :onDelete="handleDeletePrise"
            emptyMessage="Aucune prise enregistrée ce mois"
          />
        </div>
        <div class="hidden md:block">
          <Datatable :entries="listePrises" :columns="columns" :deleteFunction="handleDeletePrise" />
        </div>
      </template>
      <div v-else class="flex justify-center items-center h-32">
        <p class="text-2xl text-center">Aucune donnée enregistrée</p>
      </div>
    </section>

    <!-- Section Sessions de traitements non médicamenteux -->
    <section class="container !mt-0 mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
      <div class="flex justify-between items-center mb-4 medicament-section-header">
        <h2 class="text-2xl flex gap-2 ml-2">
          <i class="material-symbols-outlined text-3xl">healing</i>
          Sessions de traitements
        </h2>
        <div class="form-modal">
          <Dialog v-model:open="showAddSessionDialog">
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
              <form class="flex flex-col gap-6" @submit.prevent="onSubmitSessionForm">
                <FormField v-slot="{ componentField }" name="traitement">
                  <FormItem>
                    <FormLabel>Traitement</FormLabel>
                    <FormControl>
                      <Select v-model="session.medicamentId">
                        <SelectTrigger>
                          <SelectValue placeholder="Sélectionner un traitement"/>
                        </SelectTrigger>
                        <SelectContent>
                          <SelectGroup v-if="traitementsNonMedicamenteux.length > 0" label="Traitements non médicamenteux">
                            <SelectItem v-for="traitement in traitementsNonMedicamenteux" :key="traitement.id" :value="traitement.id">
                              {{ traitement.nom }}
                            </SelectItem>
                          </SelectGroup>
                          <p v-else class="px-4 py-2">Pas de traitements non médicamenteux enregistrés</p>
                        </SelectContent>
                      </Select>
                    </FormControl>
                    <div v-if="traitementsNonMedicamenteux.length === 0" class="mt-3 rounded-lg border border-dashed border-emerald-300 bg-emerald-50 p-3">
                      <p class="text-sm text-emerald-800 mb-2">Ajoute d'abord un traitement non médicamenteux (ex: yoga, kiné, ostéo).</p>
                      <Button type="button" size="sm" variant="outline" @click="openAddNonMedicamentDialog">
                        Créer un traitement non médicamenteux
                      </Button>
                    </div>
                    <FormMessage/>
                  </FormItem>
                </FormField>
                <FormField v-slot="{ componentField }" name="duree">
                  <FormItem>
                    <FormLabel>Durée (minutes) - optionnel</FormLabel>
                    <FormControl>
                      <Input type="number" v-bind="componentField" v-model="session.duree" min="1" placeholder="Ex: 30"/>
                    </FormControl>
                    <FormMessage/>
                  </FormItem>
                </FormField>
                <div class="flex items-center gap-8 session-date-time-row">
                  <FormField v-slot="{ componentField }" name="date">
                    <FormItem>
                      <FormLabel>Date</FormLabel>
                      <FormControl>
                        <Input type="date" v-bind="componentField" v-model="session.date" class="min-w-28"/>
                      </FormControl>
                      <FormMessage/>
                    </FormItem>
                  </FormField>
                  <FormField v-slot="{ componentField }" name="time">
                    <FormItem>
                      <FormLabel>Heure</FormLabel>
                      <FormControl>
                        <Input type="time" v-bind="componentField" v-model="session.time" class="min-w-28"/>
                      </FormControl>
                      <FormMessage/>
                    </FormItem>
                  </FormField>
                </div>
                <FormField v-slot="{ componentField }" name="commentaire">
                  <FormItem>
                    <FormLabel>Un commentaire ? (optionnel)</FormLabel>
                    <FormControl>
                      <Input type="text" placeholder="Écrivez ici" v-bind="componentField" v-model="session.commentaire"/>
                    </FormControl>
                    <FormMessage/>
                  </FormItem>
                </FormField>
                <div class="flex flex-wrap gap-2 -mt-2">
                  <Button
                    v-for="duration in sessionDurationPresets"
                    :key="duration"
                    type="button"
                    size="sm"
                    variant="outline"
                    @click="setSessionDuration(duration)"
                  >
                    {{ duration }} min
                  </Button>
                </div>
                <Button class="mt-4" variant="custom" type="submit" :disabled="!isSessionFormValid">
                  Enregistrer
                </Button>
              </form>
            </DialogContent>
          </Dialog>
        </div>
      </div>
      <SelectMonth v-model="selectedMonthYearSessions"/>
      <div class="grid grid-cols-2 gap-2 mt-4 mb-2">
        <div class="bg-white border border-emerald-100 rounded-xl p-3 text-center">
          <p class="text-xs text-muted-foreground">Sessions ce mois</p>
          <p class="text-xl font-bold text-headline">{{ sessionsStats.totalSessions }}</p>
        </div>
        <div class="bg-white border border-emerald-100 rounded-xl p-3 text-center">
          <p class="text-xs text-muted-foreground">Minutes cumulées</p>
          <p class="text-xl font-bold text-headline">{{ sessionsStats.totalMinutes }}</p>
        </div>
      </div>
      <div v-if="isLoadingSessions" class="flex flex-col space-y-3">
        <Skeleton class="h-[300px] w-full mt-4 rounded-xl"/>
      </div>
      <template v-else-if="listeSessions.length > 0">
        <div class="md:hidden">
          <GenericCardList
            :entries="listeSessions"
            titleField="nom"
            dateField="date"
            timeField="time"
            :extraFields="[{ key: 'duree', label: 'Durée', suffix: ' min' }, { key: 'commentaire', label: 'Note' }]"
            :iconConfig="sessionIconConfig"
            :onDelete="handleDeleteSession"
            emptyMessage="Aucune session enregistrée ce mois"
          />
        </div>
        <div class="hidden md:block">
          <Datatable :entries="listeSessions" :columns="columnsSessions" :deleteFunction="handleDeleteSession" />
        </div>
      </template>
      <div v-else class="flex justify-center items-center h-32">
        <p class="text-2xl text-center">Aucune donnée enregistrée</p>
      </div>
    </section>

    <div class="flex-row-container w-full gap-8 mb-16">
      <section class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
        <div class="flex gap-4 items-center justify-between mb-4 traitement-list-header">
          <h2 class="text-2xl flex gap-2">
            <i class="material-symbols-outlined text-3xl">{{ materialSymbols.treatments }}</i>
            Traitements en cours
          </h2>
          <Dialog v-model:open="showAddTraitementDialog">
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
                <FormField v-slot="{ componentField }" name="type">
                  <FormItem>
                    <FormLabel>Type de traitement</FormLabel>
                    <FormControl>
                      <Select v-model="traitement.type">
                        <SelectTrigger>
                          <SelectValue/>
                        </SelectTrigger>
                        <SelectContent>
                          <SelectGroup label="Type">
                            <SelectItem :value="String(TypeTraitement.Medicamenteux)">Médicamenteux</SelectItem>
                            <SelectItem :value="String(TypeTraitement.NonMedicamenteux)">Non médicamenteux</SelectItem>
                          </SelectGroup>
                        </SelectContent>
                      </Select>
                    </FormControl>
                    <FormMessage/>
                  </FormItem>
                </FormField>
                <FormField v-if="showPosologieInAddForm" v-slot="{ componentField }" name="posologie">
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
          <li v-for="traitement in traitementsEnCours" :key="traitement.id" class="relative pr-20 traitement-item">
            <div class="absolute top-0 right-0 flex gap-2 traitement-actions">
              <Dialog v-model:open="showEditTraitementDialog">
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
                  <FormField v-slot="{ componentField }" name="type">
                    <FormItem>
                      <FormLabel>Type de traitement</FormLabel>
                      <FormControl>
                        <Select v-model="traitementForm.type">
                          <SelectTrigger>
                            <SelectValue/>
                          </SelectTrigger>
                          <SelectContent>
                            <SelectGroup label="Type">
                              <SelectItem :value="String(TypeTraitement.Medicamenteux)">Médicamenteux</SelectItem>
                              <SelectItem :value="String(TypeTraitement.NonMedicamenteux)">Non médicamenteux</SelectItem>
                            </SelectGroup>
                          </SelectContent>
                        </Select>
                      </FormControl>
                      <FormMessage/>
                    </FormItem>
                  </FormField>
                  <FormField v-if="showPosologieInEditForm" v-slot="{ componentField }" name="posologie">
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
              <span @click="confirmDeleteMedicament(traitement.id)" class="material-symbols-outlined delete-btn cursor-pointer">delete</span>
            </div>
            <p class="text-headline font-bold text-xl">{{ traitement.nom }}</p>
            <p class="text-sm text-gray-600">{{ traitement.type === TypeTraitement.Medicamenteux ? 'Médicamenteux' : 'Non médicamenteux' }}</p>
            <p v-if="traitement.posologie">{{ traitement.posologie }}</p>
            <p>Depuis le {{ formatDateDisplay(traitement.dateDebutTraitement) }}</p>
          </li>
        </ul>
        <p v-else class="text-2xl text-center">Pas de traitements en cours</p>
      </section>
      <section class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
        <div class="flex gap-4 items-center justify-between mb-4 traitement-list-header">
          <h2 class="text-2xl flex gap-2">
            <i class="material-symbols-outlined text-3xl">{{ materialSymbols.pastTreatments }}</i>
            Traitements passés
          </h2>
        </div>
        <ul class="w-full">
          <li v-for="traitement in traitementsPasses" :key="traitement.id" class="relative pr-12 past-treatment-item">
            <div class="absolute top-0 right-0 past-treatment-actions">
              <span @click="confirmDeleteMedicament(traitement.id)" class="material-symbols-outlined delete-btn cursor-pointer">delete</span>
            </div>
            <div>
              <p class="text-headline font-bold text-xl">{{ traitement.nom }}</p>
              <p class="text-sm text-gray-600">{{ traitement.type === TypeTraitement.Medicamenteux ? 'Médicamenteux' : 'Non médicamenteux' }}</p>
              <p v-if="traitement.posologie">{{ traitement.posologie }}</p>
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
import { onMounted, ref, computed, watch } from 'vue'

import { Button } from '@/shared/components/ui/button'
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/shared/components/ui/form'
import { Input } from '@/shared/components/ui/input'
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from '@/shared/components/ui/select'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/shared/components/ui/dialog'
import { Skeleton } from "@/shared/components/ui/skeleton"
import Datatable from "@/shared/components/Datatable.vue"
import BackButton from "@/shared/components/BackButton.vue"
import SelectMonth from "@/shared/components/SelectMonth.vue"
import GenericCardList from "@/shared/components/GenericCardList.vue"

import apiService from "@/shared/services/apiService"
import { useAuthStore } from '@/features/auth/store/auth'
import { useMonthData } from '@/shared/composables/useMonthData'
import { useDateTimeFormat } from '@/shared/composables/useDateTimeFormat'
import { useCrudOperations } from '@/shared/composables/useCrudOperations'
import { useDialogForm } from '@/shared/composables/useDialogForm'
import { useToast } from '@/shared/components/ui/toast'
import { useSync } from '@/shared/composables/useSync'
import { format, parseISO } from 'date-fns'
import { TypeTraitement } from '@/features/medicament/types/type-traitement'
import { materialSymbols, traitementPriseIconConfig } from '@/shared/config/materialSymbols'

const authStore = useAuthStore()
const { formatDateDisplay, formatTimeDisplay, combineDateTime, getCurrentDateInput, getCurrentTimeInput } = useDateTimeFormat()
const { toast } = useToast()
const { handleOfflineOperation } = useSync()

// Contrôle des dialogs
const showAddPriseDialog = ref(false)
const showAddTraitementDialog = ref(false)
const showAddSessionDialog = ref(false)
const showEditTraitementDialog = ref(false)

// Initialiser les composables de formulaire pour chaque dialog
const { submitForm: submitPriseForm } = useDialogForm(showAddPriseDialog)
const { submitForm: submitTraitementForm } = useDialogForm(showAddTraitementDialog)
const { submitForm: submitSessionForm } = useDialogForm(showAddSessionDialog)
const { submitForm: submitEditForm } = useDialogForm(showEditTraitementDialog)

const traitementsEnCours = ref<Medicament[]>([])
const medicaments = ref<Medicament[]>([])
const traitementsPasses = ref<Medicament[]>([])
const traitementsNonMedicamenteux = ref<Medicament[]>([])
const medicamentToDelete = ref<string | null>(null)
const showDeleteDialog = ref(false)

// Données pour les sessions de traitements non médicamenteux
const listeSessions = ref<any[]>([])
const isLoadingSessions = ref(false)
const selectedMonthYearSessions = ref<string>(
    `${new Date().getFullYear()}-${String(new Date().getMonth() + 1).padStart(2, '0')}`
)

const session = ref({
  medicamentId: '',
  duree: undefined as number | undefined,
  date: getCurrentDateInput(),
  time: getCurrentTimeInput(),
  commentaire: ''
})

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

const { deleteEntry } = useCrudOperations(listePrises)

const prise = ref({
  nom: '',
  date: getCurrentDateInput(),
  time: getCurrentTimeInput(),
  commentaire: '',
  medicamentId: '',
  nombreComprimes: 1
})

const traitement = ref({ nom: '', type: String(TypeTraitement.Medicamenteux), posologie: '', dateDebutTraitement: '' })

const traitementForm: any = ref({
  id: '',
  nom: '',
  type: String(TypeTraitement.Medicamenteux),
  posologie: '',
  dateDebutTraitement: '',
  dateFinTraitement: '',
  traitementEnCours: '',
})

// Computed pour savoir si on doit afficher le champ posologie dans le formulaire d'ajout
const showPosologieInAddForm = computed(() => Number(traitement.value.type) === TypeTraitement.Medicamenteux)

// Computed pour savoir si on doit afficher le champ posologie dans le formulaire d'édition
const showPosologieInEditForm = computed(() => Number(traitementForm.value.type) === TypeTraitement.Medicamenteux)

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

const columnsSessions: any = [
  { data: 'nom', title: 'Traitement' },
  { data: 'duree', title: 'Durée' },
  { data: 'date', title: 'Date' },
  { data: 'time', title: 'Heure' },
  { data: 'commentaire', title: 'Commentaire' },
  {
    data: null,
    defaultContent: '<span class="material-symbols-outlined delete-btn">delete</span>'
  },
]

const priseIconConfig = traitementPriseIconConfig

const sessionIconConfig = {
  'Yoga': { color: 'text-emerald-600', bg: 'bg-emerald-100', icon: 'self_improvement' },
  'Kine': { color: 'text-orange-600', bg: 'bg-orange-100', icon: 'healing' },
  'Osteo': { color: 'text-cyan-600', bg: 'bg-cyan-100', icon: 'accessibility_new' },
  'Sophrologie': { color: 'text-violet-600', bg: 'bg-violet-100', icon: 'spa' }
}

const sessionDurationPresets = [20, 30, 45, 60]
const shouldReturnToSessionDialog = ref(false)

const prisesStats = computed(() => {
  const totalPrises = listePrises.value.length
  const totalComprimes = listePrises.value.reduce((acc: number, item: any) => acc + Number(item.nombreComprimes || 0), 0)
  return { totalPrises, totalComprimes }
})

const sessionsStats = computed(() => {
  const totalSessions = listeSessions.value.length
  const totalMinutes = listeSessions.value.reduce((acc: number, item: any) => {
    const value = Number(item.duree)
    return Number.isFinite(value) ? acc + value : acc
  }, 0)
  return { totalSessions, totalMinutes }
})

const isPriseFormValid = computed(() => !!prise.value.medicamentId && Number(prise.value.nombreComprimes) > 0 && !!prise.value.date && !!prise.value.time)
const isSessionFormValid = computed(() => !!session.value.medicamentId && !!session.value.date && !!session.value.time)

const setSessionDuration = (minutes: number) => {
  session.value.duree = minutes
}

const openAddNonMedicamentDialog = () => {
  shouldReturnToSessionDialog.value = true
  traitement.value = {
    nom: '',
    type: String(TypeTraitement.NonMedicamenteux),
    posologie: '',
    dateDebutTraitement: getCurrentDateInput(),
  }
  showAddSessionDialog.value = false
  showAddTraitementDialog.value = true
}

const openAddMedicamenteuxDialog = () => {
  traitement.value = {
    nom: '',
    type: String(TypeTraitement.Medicamenteux),
    posologie: '',
    dateDebutTraitement: getCurrentDateInput(),
  }
  showAddPriseDialog.value = false
  showAddTraitementDialog.value = true
}

interface Medicament {
  id: string
  nom: string
  type: TypeTraitement
  posologie?: string
  dateDebutTraitement: any
  dateFinTraitement?: any
  traitementEnCours: boolean
}

const fetchAllMedicaments = async () => {
  try {
    const response = await apiService.getAllMedicaments(authStore.user!.carnetSanteId)

    // Séparer les traitements médicamenteux et non médicamenteux
    const allMedicaments = response.$values
    const medicamenteux = allMedicaments.filter((med: Medicament) => med.type === TypeTraitement.Medicamenteux)
    const nonMedicamenteux = allMedicaments.filter((med: Medicament) => med.type === TypeTraitement.NonMedicamenteux)

    // Pour les prises de médicaments (seulement médicamenteux)
    medicaments.value = medicamenteux.map((med: Medicament) => ({
      id: med.id,
      nom: med.nom,
      posologie: med.posologie,
    }))

    // Pour les traitements en cours/passés
    traitementsEnCours.value = allMedicaments.filter(med => med.traitementEnCours)
    traitementsPasses.value = allMedicaments.filter(med => med.traitementEnCours == false)

    // Pour les sessions (seulement non médicamenteux)
    traitementsNonMedicamenteux.value = nonMedicamenteux.map((med: Medicament) => ({
      id: med.id,
      nom: med.nom,
    }))
  } catch (error) {
    console.error(error)
  }
}

const fetchSessions = async (monthYear: string) => {
  isLoadingSessions.value = true
  try {
    const [year, month] = monthYear.split('-').map(Number)
    const response = await apiService.getDonneesTraitementNonMedicamenteuxByMonth(authStore.user!.carnetSanteId, month, year)
    listeSessions.value = response.map((d: any) => ({
      id: d.id,
      nom: d.nomTraitement,
      duree: d.duree || '-',
      date: formatDateDisplay(d.date),
      time: formatTimeDisplay(d.date),
      commentaire: d.commentaire || 'Pas de détails',
    }))
  } catch (error) {
    console.error('Error loading sessions data:', error)
  } finally {
    isLoadingSessions.value = false
  }
}

// Watcher pour charger les sessions quand le mois change
watch(selectedMonthYearSessions, (newValue) => {
  fetchSessions(newValue)
})

onMounted(async () => {
  isLoading.value = true
  isLoadingSessions.value = true

  try {
    await fetchAllMedicaments()

    // Charger les prises de médicaments
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

    // Charger les sessions de traitements
    await fetchSessions(selectedMonthYearSessions.value)
  } catch (error) {
    console.error('Error loading medicament data:', error)
  } finally {
    isLoading.value = false
    isLoadingSessions.value = false
  }
})

const handleDeletePrise = async (id: string | number) => {
  await deleteEntry(id as number, (entryId) => apiService.deleteDonneesMedicament(entryId as number), {
    successMessage: 'La prise de médicament a été supprimée avec succès',
    errorMessage: 'Une erreur est survenue lors de la suppression de la prise de médicament',
    endpoint: 'DonneesMedicament'
  })
}

const onSubmitPriseForm = () => {
  const values = prise.value

  if (!values.medicamentId) {
    toast({
      title: 'Traitement requis',
      description: 'Sélectionnez un traitement médicamenteux avant d\'enregistrer la prise.',
      variant: 'destructive'
    })
    return
  }

  const medicamentName = medicaments.value.find(med => med.id === values.medicamentId)?.nom

  const dataToSend = {
    medicamentId: values.medicamentId,
    nombreComprimes: values.nombreComprimes,
    date: combineDateTime(values.date, values.time),
    commentaire: values.commentaire || 'Pas de commentaire',
    carnetSanteId: authStore.user?.carnetSanteId,
  }

  submitPriseForm(dataToSend, {
    submitFunction: (data) => apiService.postDonneesPriseMedicament(data),
    successMessage: 'La prise de médicament a été enregistrée avec succès',
    errorMessage: 'Une erreur est survenue lors de l\'enregistrement de la prise de médicament',
    onSuccess: (response) => {
      listePrises.value.push({
        id: response.id,
        nom: medicamentName,
        nombreComprimes: values.nombreComprimes,
        commentaire: values.commentaire || 'Pas de commentaire',
        date: formatDateDisplay(values.date),
        time: formatTimeDisplay(values.time),
      })
    },
    resetFormData: () => {
      prise.value = {
        nom: '',
        date: getCurrentDateInput(),
        time: getCurrentTimeInput(),
        commentaire: '',
        medicamentId: '',
        nombreComprimes: 1
      }
    }
  })
}

const onSubmitSessionForm = () => {
  const values = session.value

  if (!values.medicamentId) {
    toast({
      title: 'Traitement requis',
      description: 'Ajoutez ou sélectionnez un traitement non médicamenteux (yoga, kiné...) avant d\'enregistrer la session.',
      variant: 'destructive'
    })
    return
  }

  const traitementName = traitementsNonMedicamenteux.value.find(t => t.id === values.medicamentId)?.nom

  const dataToSend = {
    medicamentId: values.medicamentId,
    duree: values.duree || null,
    date: combineDateTime(values.date, values.time),
    commentaire: values.commentaire || 'Pas de commentaire',
    carnetSanteId: authStore.user?.carnetSanteId,
  }

  submitSessionForm(dataToSend, {
    submitFunction: (data) => apiService.postDonneesTraitementNonMedicamenteux(data),
    successMessage: 'La session a été enregistrée avec succès',
    errorMessage: 'Une erreur est survenue lors de l\'enregistrement de la session',
    onSuccess: (response) => {
      listeSessions.value.push({
        id: response.id,
        nom: traitementName,
        duree: values.duree || '-',
        date: formatDateDisplay(values.date),
        time: formatTimeDisplay(values.time),
        commentaire: values.commentaire || 'Pas de commentaire',
      })
    },
    resetFormData: () => {
      session.value = {
        medicamentId: '',
        duree: undefined,
        date: getCurrentDateInput(),
        time: getCurrentTimeInput(),
        commentaire: ''
      }
    }
  })
}

const handleDeleteSession = async (id: string | number) => {
  try {
    await apiService.deleteDonneesTraitementNonMedicamenteux(id as number)
    listeSessions.value = listeSessions.value.filter(s => s.id !== (id as number))
    toast({
      title: 'Succès',
      description: 'La session a été supprimée avec succès',
    })
  } catch (error) {
    console.error(error)
    toast({
      title: 'Erreur',
      description: 'Une erreur est survenue lors de la suppression de la session',
      variant: 'destructive'
    })
  }
}

const onSubmitTraitementForm = () => {
  const values = traitement.value
  const dataToSend = {
    nom: values.nom,
    type: Number(values.type),
    posologie: values.posologie || null,
    dateDebutTraitement: new Date(values.dateDebutTraitement),
    carnetSanteId: authStore.user?.carnetSanteId,
    traitementEnCours: true
  }

  submitTraitementForm(dataToSend, {
    submitFunction: (data) => apiService.postMedicament(data),
    successMessage: 'Le traitement a été ajouté avec succès',
    errorMessage: 'Une erreur est survenue lors de l\'ajout du traitement',
    onSuccess: (response) => {
      const medicamentAdded: any = {
        ...dataToSend,
        id: response.id
      }
      traitementsEnCours.value.push(medicamentAdded)

      // Ajouter à la liste appropriée selon le type
      if (Number(values.type) === TypeTraitement.Medicamenteux) {
        medicaments.value.push(medicamentAdded)
      } else {
        traitementsNonMedicamenteux.value.push(medicamentAdded)

        if (shouldReturnToSessionDialog.value) {
          session.value.medicamentId = String(response.id)
          shouldReturnToSessionDialog.value = false
          showAddSessionDialog.value = true
        }
      }
    },
    resetFormData: () => {
      traitement.value = {
        nom: '',
        type: String(TypeTraitement.Medicamenteux),
        posologie: '',
        dateDebutTraitement: ''
      }
    }
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
      type: String(traitementToEdit.type),
      dateDebutTraitement: formattedStartDate,
      dateFinTraitement: formattedEndDate
    }
  }
}

const onSubmitEditTraitement = () => {
  const values = traitementForm.value
  const traitementId: any = values.id

  // Créer un objet propre avec seulement les propriétés nécessaires
  const dataToSend = {
    id: traitementId,
    nom: values.nom,
    type: Number(values.type),
    posologie: values.posologie || null,
    dateDebutTraitement: new Date(values.dateDebutTraitement),
    dateFinTraitement: values.dateFinTraitement ? new Date(values.dateFinTraitement) : null,
    traitementEnCours: values.traitementEnCours === 'true' || values.traitementEnCours === true,
    carnetSanteId: authStore.user?.carnetSanteId
  }

  submitEditForm(dataToSend, {
    submitFunction: (data) => apiService.putDonneesMedicament(traitementId, data),
    successMessage: 'Le traitement a été modifié avec succès',
    errorMessage: 'Une erreur est survenue lors de la modification du traitement',
    onSuccess: () => {
      const indexEnCours = traitementsEnCours.value.findIndex(traitement => traitement.id === traitementId)
      const indexPasses = traitementsPasses.value.findIndex(traitement => traitement.id === traitementId)

      if (dataToSend.traitementEnCours) {
        if (indexEnCours !== -1) {
          traitementsEnCours.value[indexEnCours] = dataToSend
        } else {
          traitementsEnCours.value.push(dataToSend)
        }
        if (indexPasses !== -1) {
          traitementsPasses.value.splice(indexPasses, 1)
        }
      } else {
        if (indexEnCours !== -1) {
          traitementsEnCours.value.splice(indexEnCours, 1)
        }
        if (indexPasses === -1) {
          traitementsPasses.value.push(dataToSend)
        }
      }
    }
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

<style scoped>
@media (max-width: 425px) {
  .medicament-section-header,
  .traitement-list-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.75rem;
  }

  .prise-date-time-row,
  .session-date-time-row {
    flex-direction: column;
    align-items: stretch;
    gap: 0.75rem;
  }

  .treatment-item,
  .past-treatment-item {
    padding-right: 0;
  }

  .treatment-actions,
  .past-treatment-actions {
    position: static;
    margin-bottom: 0.5rem;
    justify-content: flex-end;
  }
}
</style>

