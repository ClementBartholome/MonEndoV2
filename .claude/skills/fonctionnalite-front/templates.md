# Gabarits front MonEndo

Remplacer `Xxx`/`xxx` par le nom métier (en français pour le domaine : `Hydratation`, `hydratation`).
Ces gabarits reprennent le pattern de la chaîne acné (`features/cycle/`) sans les `any`.

## 1. Contrat — `features/<domaine>/types/xxx-section.ts`
```ts
import type { DonneesXxx } from './donnees-xxx'

/** État affiché, en lecture seule pour la présentation. */
export interface XxxSectionModel {
  entries: DonneesXxx[]
  isLoading: boolean
  selectedMonthYear: string
  totalCount: number
}

/** Actions déclenchables depuis la présentation. */
export interface XxxSectionActions {
  selectMonth: (monthYear: string) => void
  openAddDialog: () => void
  editEntry: (entry: DonneesXxx) => void
  deleteEntry: (id: number) => Promise<void>
}
```

## 2. Composable — `features/<domaine>/composables/useXxxSection.ts`
```ts
import { computed, ref } from 'vue'
import { useToast } from '@/shared/components/ui/toast'
import { useDateTimeFormat } from '@/shared/composables/useDateTimeFormat'
import apiService from '@/shared/services/apiService'
import type { DonneesXxx } from '@/features/<domaine>/types/donnees-xxx'
import type { XxxSectionActions, XxxSectionModel } from '@/features/<domaine>/types/xxx-section'

interface UseXxxSectionOptions {
  carnetSanteId: number
  onRequestAdd?: () => void
  onRequestEdit?: (entry: DonneesXxx) => void
}

// Fonctions pures au niveau module : faciles à tester et à relire.
const sortByDateDesc = (entries: DonneesXxx[]): DonneesXxx[] =>
  [...entries].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())

export const useXxxSection = (options: UseXxxSectionOptions) => {
  const { toast } = useToast()
  const { getCurrentMonthYear } = useDateTimeFormat()

  const entries = ref<DonneesXxx[]>([])
  const isLoading = ref(false)
  const selectedMonthYear = ref(getCurrentMonthYear())

  const load = async () => {
    isLoading.value = true
    try {
      const [year, month] = selectedMonthYear.value.split('-').map(Number)
      entries.value = sortByDateDesc(await apiService.getDonneesXxxByMonth(options.carnetSanteId, month, year))
    } catch (error) {
      console.error('Erreur de chargement xxx :', error)
      toast({ title: 'Erreur', description: 'Impossible de charger les données', variant: 'destructive' })
    } finally {
      isLoading.value = false
    }
  }

  const actions: XxxSectionActions = {
    selectMonth: (monthYear) => {
      selectedMonthYear.value = monthYear
      void load()
    },
    openAddDialog: () => options.onRequestAdd?.(),
    editEntry: (entry) => options.onRequestEdit?.(entry),
    deleteEntry: async (id) => {
      try {
        await apiService.deleteDonneesXxx(id)
        entries.value = entries.value.filter((entry) => entry.id !== id)
        toast({ title: 'Succès', description: 'Entrée supprimée', variant: 'custom' })
      } catch (error) {
        console.error('Erreur de suppression xxx :', error)
        toast({ title: 'Erreur', description: "Impossible de supprimer l'entrée", variant: 'destructive' })
      }
    },
  }

  const model = computed<XxxSectionModel>(() => ({
    entries: entries.value,
    isLoading: isLoading.value,
    selectedMonthYear: selectedMonthYear.value,
    totalCount: entries.value.length,
  }))

  return { model, actions, load }
}
```

## 3. Conteneur — `features/<domaine>/components/XxxSection.vue`
```vue
<script setup lang="ts">
import { onMounted } from 'vue'
import XxxSectionContent from '@/features/<domaine>/components/XxxSectionContent.vue'
import { useXxxSection } from '@/features/<domaine>/composables/useXxxSection'
import type { DonneesXxx } from '@/features/<domaine>/types/donnees-xxx'

const props = defineProps<{ carnetSanteId: number }>()

const emit = defineEmits<{
  'open-add': []
  'edit-entry': [entry: DonneesXxx]
}>()

const { model, actions, load } = useXxxSection({
  carnetSanteId: props.carnetSanteId,
  onRequestAdd: () => emit('open-add'),
  onRequestEdit: (entry) => emit('edit-entry', entry),
})

onMounted(load)
defineExpose({ reload: load })
</script>

<template>
  <XxxSectionContent :model="model" :actions="actions" />
</template>
```

## 4. Présentation — `features/<domaine>/components/XxxSectionContent.vue`
```vue
<script setup lang="ts">
import GenericCardList from '@/shared/components/GenericCardList.vue'
import EmptyStateAction from '@/shared/components/EmptyStateAction.vue'
import { Skeleton } from '@/shared/components/ui/skeleton'
import type { XxxSectionActions, XxxSectionModel } from '@/features/<domaine>/types/xxx-section'

// Aucun état, aucun appel API : tout passe par model et actions.
const props = defineProps<{
  model: XxxSectionModel
  actions: XxxSectionActions
}>()
</script>

<template>
  <section class="container !mt-0 mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl">
    <Skeleton v-if="props.model.isLoading" class="h-24 w-full" />
    <EmptyStateAction
      v-else-if="props.model.totalCount === 0"
      title="Aucune entrée ce mois-ci"
      action-label="Ajouter"
      @action="props.actions.openAddDialog"
    />
    <GenericCardList
      v-else
      :entries="props.model.entries"
      title-field="type"
      date-field="date"
      :on-edit="props.actions.editEntry"
      :on-delete="(id) => props.actions.deleteEntry(Number(id))"
    />
  </section>
</template>

<style scoped>
@media (max-width: 425px) {
  /* ajustements petits écrans */
}
</style>
```
Vérifier les props exactes de `GenericCardList` et `EmptyStateAction` dans `src/shared/components/` avant usage.
