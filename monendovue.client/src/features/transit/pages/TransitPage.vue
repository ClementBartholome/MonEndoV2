<template>
  <div class="flex-column-container">
    <BackButton/>
    <section class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
      <div class="flex justify-between items-center mb-4 transit-header">
        <h2 class="text-2xl flex gap-2 ml-2"><i class="material-symbols-outlined text-3xl ml-auto">gastroenterology</i>
          Suivi du transit</h2>
        <div class="form-modal">
          <Dialog v-model:open="showAddDialog">
            <DialogTrigger class="flex gap-2 items-center cursor-pointer hover:opacity-80 transition-opacity">
              <Button variant="custom">
                <span class="hide-xsm">Ajouter une entrée</span>
                <i class="material-symbols-outlined">add</i>
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle class="text-2xl">Ajouter une entrée</DialogTitle>
              </DialogHeader>
              <form class="flex flex-col gap-6" @submit.prevent="onSubmit">
                <FormField v-slot="{ componentField }" name="typeEvenement">
                  <FormItem>
                    <FormLabel>Type de trouble</FormLabel>
                    <FormControl>
                      <Select v-model="entry.typeEvenement" v-bind="componentField">
                        <SelectTrigger>
                          <SelectValue v-bind="componentField">
                            {{ entry.typeEvenement || 'Sélectionnez un type de trouble' }}
                          </SelectValue>
                        </SelectTrigger>
                        <SelectContent>
                          <SelectGroup label="Type de trouble">
                            <SelectItem value="Diarrhée">Diarrhée</SelectItem>
                            <SelectItem value="Constipation">Constipation</SelectItem>
                            <SelectItem value="Crampes">Crampes</SelectItem>
                            <SelectItem value="Ballonnements">Ballonnements</SelectItem>
                            <SelectItem value="Nausée">Nausée</SelectItem>
                            <SelectItem value="Autre">Autre</SelectItem>
                          </SelectGroup>
                        </SelectContent>
                      </Select>
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                </FormField>
                <div class="flex items-center justify-between transit-date-time-row">
                  <FormField name="date" v-slot="{ componentField }">
                    <FormItem>
                      <FormLabel>Date</FormLabel>
                      <FormControl>
                        <Input type="date" v-model="entry.date" v-bind="componentField" class="min-w-28"/>
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  </FormField>
                  <FormField v-slot="{ componentField }" name="time">
                    <FormItem>
                      <FormLabel>Heure</FormLabel>
                      <FormControl>
                        <Input type="time" v-model="entry.time" v-bind="componentField" class="min-w-28"/>
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  </FormField>
                </div>
                <FormField name="intensite" v-slot="{ componentField }">
                  <FormItem>
                    <FormLabel>Intensité</FormLabel>
                    <FormControl>
                      <Select v-model="entry.intensite" v-bind="componentField">
                        <SelectTrigger>
                          <SelectValue v-bind="componentField">
                            {{ entry.intensite || 'Sélectionnez un niveau d\'intensité' }}
                          </SelectValue>
                        </SelectTrigger>
                        <SelectContent>
                          <SelectGroup label="Intensité">
                            <SelectItem value="Légère">Légère</SelectItem>
                            <SelectItem value="Modérée">Modérée</SelectItem>
                            <SelectItem value="Sévère">Sévère</SelectItem>
                          </SelectGroup>
                        </SelectContent>
                      </Select>
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                </FormField>
                <div class="flex items-center justify-between transit-radios-row">
                  <FormField v-slot="{ componentField }" name="saignement">
                    <FormItem>
                      <FormControl>
                        <FormLabel>Saignement</FormLabel>
                        <div class="flex items-center">
                          <input type="radio" v-bind="componentField" v-model="entry.saignement"
                                 :value="true"/>
                          <label class="mx-2">Oui</label>
                          <input type="radio" v-bind="componentField" v-model="entry.saignement"
                                 :value="false"/>
                          <label class="ml-2">Non</label>
                        </div>
                      </FormControl>
                      <FormMessage/>
                    </FormItem>
                  </FormField>
                  <FormField v-slot="{ componentField }" name="douleurs">
                    <FormItem>
                      <FormControl>
                        <FormLabel>Douleurs</FormLabel>
                        <div class="flex items-center">
                          <input type="radio" v-bind="componentField" v-model="entry.douleurs"
                                 :value="true"/>
                          <label class="mx-2">Oui</label>
                          <input type="radio" v-bind="componentField" v-model="entry.douleurs"
                                 :value="false"/>
                          <label class="ml-2">Non</label>
                        </div>
                      </FormControl>
                      <FormMessage/>
                    </FormItem>
                  </FormField>
                </div>
                <FormField v-slot="{ componentField }" name="commentaire">
                  <FormItem>
                    <FormLabel>Un commentaire ? (optionnel)</FormLabel>
                    <FormControl>
                      <Input type="text" placeholder="Écrivez ici" v-bind="componentField" v-model="entry.commentaire"/>
                    </FormControl>
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
      <SectionKpiHeader :items="transitKpis" />

      <div v-if="isLoading" class="flex flex-col space-y-3">
        <Skeleton class="h-[300px] w-full mt-4 rounded-xl"/>
      </div>
      <template v-else-if="entries.length > 0">
        <div class="md:hidden">
          <GenericCardList
            :entries="entries"
            titleField="typeEvenement"
            dateField="date"
            timeField="time"
            :extraFields="[
              { key: 'intensite', label: 'Intensité' },
              { key: 'saignement', label: 'Saignement' },
              { key: 'douleurs', label: 'Douleurs' },
              { key: 'commentaire', label: 'Note' }
            ]"
            :defaultIcon="{ color: 'text-purple-600', bg: 'bg-purple-100', icon: 'gastroenterology' }"
            :onDelete="handleDelete"
            emptyMessage="Aucune donnée de transit ce mois"
          />
        </div>
        <div class="hidden md:block">
          <Datatable :entries="entries" :columns="columns" :deleteFunction="handleDelete">
            <thead>
            <tr>
              <th>Type</th>
              <th>Date</th>
              <th>Heure</th>
              <th>Intensité</th>
              <th>Saignement</th>
              <th>Douleurs</th>
              <th>Commentaire</th>
              <th></th>
            </tr>
            </thead>
          </Datatable>
        </div>
      </template>
      <div v-else class="flex justify-center items-center h-32">
        <p class="text-2xl text-center">Aucune donnée enregistrée</p>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import * as z from 'zod'

import { Button } from '@/shared/components/ui/button'
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/shared/components/ui/form'
import { Input } from '@/shared/components/ui/input'
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from '@/shared/components/ui/select'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/shared/components/ui/dialog'
import { Skeleton } from "@/shared/components/ui/skeleton"
import Datatable from "@/shared/components/Datatable.vue"
import GenericCardList from "@/shared/components/GenericCardList.vue"
import BackButton from "@/shared/components/BackButton.vue"
import SelectMonth from "@/shared/components/SelectMonth.vue"
import SectionKpiHeader from "@/shared/components/SectionKpiHeader.vue"

import apiService from "@/shared/services/apiService"
import { useAuthStore } from '@/features/auth/store/auth'
import { useMonthData } from '@/shared/composables/useMonthData'
import { useDateTimeFormat } from '@/shared/composables/useDateTimeFormat'
import { useCrudOperations } from '@/shared/composables/useCrudOperations'
import { useDialogForm } from '@/shared/composables/useDialogForm'

const authStore = useAuthStore()
const { formatDateDisplay, formatTimeDisplay, combineDateTime } = useDateTimeFormat()

// Dialog control
const showAddDialog = ref(false)
const { submitForm } = useDialogForm(showAddDialog)

const { selectedMonthYear, entries, isLoading } = useMonthData({
  fetchFunction: async (month, year) => {
    return apiService.getDonneesTransitByMonth(authStore.user!.carnetSanteId, month, year)
  },
  transformData: (data) => {
    return data.map((entry: any) => ({
      ...entry,
      date: formatDateDisplay(entry.date),
      time: formatTimeDisplay(entry.date),
      douleurs: entry.douleur ? 'Oui' : 'Non',
      saignement: entry.saignement ? 'Oui' : 'Non',
      commentaire: entry.commentaires || 'Pas de commentaire'
    }))
  },
  dataType: 'transit'
})

const { deleteEntry } = useCrudOperations(entries)

const columns: any = [
  {data: 'typeEvenement'},
  {data: 'date'},
  {data: 'time'},
  {data: 'intensite'},
  {data: 'saignement'},
  {data: 'douleurs'},
  {data: 'commentaire'},
  {
    data: null,
    defaultContent: '<span class="material-symbols-outlined delete-btn">delete</span>'
  },
]

const entry = ref({
  typeEvenement: '',
  date: '',
  time: '',
  intensite: '',
  saignement: false,
  douleurs: false,
  commentaire: ''
})

const transitKpis = ref([
  { label: 'Entrées', value: 0 },
  { label: 'Saignement', value: 0 },
  { label: 'Avec douleurs', value: 0 }
])

watch(entries, (value) => {
  transitKpis.value = [
    { label: 'Entrées', value: value.length },
    { label: 'Saignement', value: value.filter(item => item.saignement === 'Oui').length },
    { label: 'Avec douleurs', value: value.filter(item => item.douleurs === 'Oui').length }
  ]
}, { immediate: true, deep: true })

const handleDelete = async (id: string | number) => {
  await deleteEntry(id as number, (entryId) => apiService.deleteDonneesTransit(entryId as number), {
    successMessage: 'Donnée supprimée avec succès',
    errorMessage: 'Une erreur est survenue lors de la suppression de la donnée',
    endpoint: 'DonneesTransit'
  })
}

const formSchema = toTypedSchema(z.object({
  typeEvenement: z.string({
    required_error: 'Le type de trouble est requis'
  }),
  date: z.string({
    required_error: 'La date est requise'
  }),
  time: z.string({
    required_error: 'L\'heure est requise'
  }),
  intensite: z.string({
    required_error: 'L\'intensité est requise'
  }),
  saignement: z.boolean().optional(),
  douleurs: z.boolean().optional(),
  commentaire: z.string().optional()
}))

const form = useForm({
  validationSchema: formSchema,
  initialValues: {
    intensite: "Sélectionnez un niveau d'intensité",
    typeEvenement: "Sélectionnez un type de trouble",
  }
})

const onSubmit = form.handleSubmit((values) => {
  const dataToSend = {
    typeEvenement: entry.value.typeEvenement,
    intensite: entry.value.intensite,
    saignement: entry.value.saignement,
    douleur: entry.value.douleurs,
    date: combineDateTime(values.date, values.time),
    carnetSanteId: authStore.user?.carnetSanteId,
    commentaire: entry.value.commentaire || 'Pas de commentaire'
  }

  submitForm(dataToSend, {
    submitFunction: (data) => apiService.postDonneesTransit(data),
    successMessage: 'Les données de transit ont été ajoutées avec succès',
    errorMessage: 'Une erreur est survenue lors de l\'ajout des données de transit',
    onSuccess: (response) => {
      entries.value.push({
        id: response.id,
        typeEvenement: entry.value.typeEvenement,
        date: formatDateDisplay(values.date),
        time: formatTimeDisplay(values.time),
        intensite: entry.value.intensite,
        douleurs: entry.value.douleurs ? 'Oui' : 'Non',
        saignement: entry.value.saignement ? 'Oui' : 'Non',
        commentaire: entry.value.commentaire || 'Pas de commentaire'
      })
    },
    resetFormData: () => {
      entry.value = {
        typeEvenement: '',
        date: '',
        time: '',
        intensite: '',
        saignement: false,
        douleurs: false,
        commentaire: ''
      }
      form.resetForm()
    }
  })
})
</script>

<style scoped>
@media (max-width: 425px) {
  .transit-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.75rem;
  }

  .transit-date-time-row,
  .transit-radios-row {
    flex-direction: column;
    align-items: stretch;
    gap: 0.75rem;
  }
}
</style>

