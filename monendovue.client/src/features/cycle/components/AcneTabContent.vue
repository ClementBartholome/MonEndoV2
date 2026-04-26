<script setup lang="ts">
import { Button } from '@/shared/components/ui/button'
import GenericCardList from '@/shared/components/GenericCardList.vue'
import SelectMonth from '@/shared/components/SelectMonth.vue'
import type { CardIconConfig, CardFieldConfig } from '@/shared/types/card'

defineProps<{
  isQuickAddingAcne: boolean
  acneMarkedToday: boolean
  ongoingAcnePeriods: any[]
  acneWindowMonths: Array<{ key: string; label: string; entries: any[] }>
  acnePhotoEntries: any[]
  orderedComparePhotos: any[]
  displayedAcneHistoryEntries: any[]
  processedAcneEntriesCount: number
  acneHistoryEntriesCount: number
  acneHistoryHiddenByPhotoCount: number
  acneHistoryInitialCount: number
  showFullAcneHistory: boolean
  isAcneHistoryExpanded: boolean
  showAcneHistoryWithPhotos: boolean
  acneMonthYear: string
  acneWindowLabel: string
  iconConfig: Record<string, CardIconConfig>
  historyExtraFields: CardFieldConfig[]
  isPhotoSelectedForCompare: (id: number) => boolean
  onOpenAddAcneDialog: () => void
  onQuickAddAcneToday: () => Promise<void>
  onExtendAcnePeriod: (period: any) => Promise<void>
  onOpenEndPeriodDialog: (period: any) => void
  onOpenPhotoModal: (url: string) => void
  onSlideAcneWindow: (direction: -1 | 1) => void
  onSelectLatestComparePair: () => void
  onTogglePhotoCompare: (id: number) => void
  onResetPhotoCompare: () => void
  onEditSymptome: (id: string | number) => void
  onDeleteSymptome: (id: string | number) => Promise<void>
  onToggleShowFullHistory: () => void
  onToggleAcneHistoryExpanded: () => void
  onToggleAcneHistoryWithPhotos: () => void
  onAcneMonthYearChange: (value: string) => void
}>()
</script>

<template>
  <section class="container !mt-0 mx-auto py-4 w-full bg-clearer rounded-3xl shadow-xl">
    <div class="flex flex-col gap-3 mb-4">
      <div class="flex items-center justify-between gap-2">
        <h2 class="text-2xl flex items-center gap-2 ml-2">
          <i class="material-symbols-outlined text-3xl">face</i>
          Suivi acné
        </h2>
        <Button variant="custom" size="sm" @click="onOpenAddAcneDialog">
          <span class="material-symbols-outlined text-base mr-1">add_a_photo</span>
          Bilan acné
        </Button>
      </div>

      <button
        v-if="!acneMarkedToday"
        @click="onQuickAddAcneToday"
        :disabled="isQuickAddingAcne"
        class="w-full flex items-center gap-3 bg-purple-50 border-2 border-purple-200 rounded-xl p-3 hover:bg-purple-100 transition-colors"
      >
        <span class="material-symbols-outlined text-purple-500 text-2xl shrink-0">face_retouching_natural</span>
        <span class="font-medium text-headline text-sm text-left flex-1">
          {{ isQuickAddingAcne ? 'Enregistrement...' : 'Acné aujourd\'hui ?' }}
        </span>
        <span class="material-symbols-outlined text-purple-400 text-base shrink-0">add_circle</span>
      </button>

      <SelectMonth :model-value="acneMonthYear" @update:model-value="onAcneMonthYearChange" />
    </div>

    <section v-if="ongoingAcnePeriods.length > 0" class="mb-4">
      <h3 class="text-lg font-semibold flex items-center gap-2 mb-2 ml-1">
        <i class="material-symbols-outlined">schedule</i>
        Période d'acné en cours
      </h3>
      <div class="flex flex-col gap-2">
        <div
          v-for="period in ongoingAcnePeriods"
          :key="`${period.startDate}-${period.endDate}`"
          class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 p-3 bg-white rounded-lg border border-gray-200 shadow-sm"
        >
          <div class="flex flex-col">
            <span class="font-medium">{{ period.startDate }} - {{ period.endDate }}</span>
            <span class="text-sm text-gray-600">{{ period.duration }} jour{{ period.duration > 1 ? 's' : '' }} • Intensité moyenne: {{ period.avgIntensity }}</span>
          </div>
          <div class="flex gap-2 flex-wrap sm:flex-nowrap">
            <Button variant="custom" size="sm" @click="onExtendAcnePeriod(period)">
              <i class="material-symbols-outlined mr-1">add</i>
              Ajouter aujourd'hui
            </Button>
            <Button variant="outline" size="sm" @click="onOpenEndPeriodDialog(period)">
              <i class="material-symbols-outlined mr-1">close</i>
              Terminer
            </Button>
          </div>
        </div>
      </div>
    </section>

    <section class="mb-4">
      <div class="flex items-center justify-between gap-2 mb-2 ml-1">
        <h3 class="text-lg font-semibold flex items-center gap-2">
          <i class="material-symbols-outlined">view_timeline</i>
          Évolution photo
        </h3>
        <div class="inline-flex items-center gap-1 rounded-full border border-purple-200 bg-white px-1 py-0.5">
          <button
            type="button"
            class="rounded-full p-1 text-purple-700 hover:bg-purple-50"
            @click="onSlideAcneWindow(-1)"
            aria-label="Fenêtre précédente"
          >
            <i class="material-symbols-outlined text-base">chevron_left</i>
          </button>
          <button
            type="button"
            class="rounded-full p-1 text-purple-700 hover:bg-purple-50"
            @click="onSlideAcneWindow(1)"
            aria-label="Fenêtre suivante"
          >
            <i class="material-symbols-outlined text-base">chevron_right</i>
          </button>
        </div>
      </div>

      <p class="ml-1 mb-2 text-xs text-purple-700">
        Fenêtre active: <span class="font-semibold capitalize">{{ acneWindowLabel }}</span>
      </p>

      <div v-if="acnePhotoEntries.length > 0" class="space-y-3">
        <article
          v-for="month in acneWindowMonths"
          :key="month.key"
          class="rounded-xl border bg-white p-3"
          :class="month.key === acneMonthYear ? 'border-purple-300 ring-1 ring-purple-200' : 'border-purple-100'"
        >
          <div class="mb-2 flex items-center justify-between gap-2">
            <h4 class="text-sm font-semibold text-purple-800 capitalize">{{ month.label }}</h4>
            <span v-if="month.key === acneMonthYear" class="rounded-full bg-purple-100 px-2 py-0.5 text-[11px] font-medium text-purple-700">
              Mois sélectionné
            </span>
          </div>
          <div v-if="month.entries.length > 0" class="grid grid-cols-2 sm:grid-cols-3 gap-3">
            <article
              v-for="entry in month.entries"
              :key="`acne-photo-${entry.id}`"
              class="group rounded-xl border border-purple-100 bg-white p-2 text-left shadow-sm hover:shadow-md transition-shadow"
              :class="isPhotoSelectedForCompare(entry.id) ? 'ring-2 ring-purple-400' : ''"
            >
              <button type="button" class="w-full text-left" @click="onOpenPhotoModal(entry.photoUrl)">
                <img :src="entry.photoUrl" alt="Photo acné" class="w-full aspect-[3/4] rounded-lg object-cover object-top bg-gray-50 mb-2" />
              </button>
              <p class="text-xs font-semibold text-headline">{{ entry.date }}</p>
              <p class="text-[11px] text-muted-foreground">Intensité {{ entry.intensite }}/10</p>
              <button
                type="button"
                class="mt-2 text-[11px] inline-flex items-center gap-1 text-purple-700 hover:underline"
                @click.stop="onTogglePhotoCompare(entry.id)"
              >
                <i class="material-symbols-outlined text-sm">compare</i>
                {{ isPhotoSelectedForCompare(entry.id) ? 'Retirer' : 'Sélectionner' }}
              </button>
            </article>
          </div>
          <p v-else class="text-xs text-muted-foreground border border-dashed border-purple-200 rounded-lg p-2">
            Aucune photo sur cette période.
          </p>
        </article>
      </div>

      <div v-else class="rounded-xl border border-dashed border-gray-300 p-4 text-sm text-muted-foreground">
        Aucune photo d'acné sur la fenêtre glissante. Ajoutez un bilan avec photo pour suivre l'évolution.
      </div>
    </section>

    <section class="rounded-xl border border-purple-200 bg-purple-50/50 p-3">
      <div class="flex items-center justify-between mb-2">
        <h3 class="text-sm font-semibold text-headline">Comparaison visuelle</h3>
        <Button variant="ghost" size="sm" @click="onResetPhotoCompare">Réinitialiser</Button>
      </div>

      <div v-if="orderedComparePhotos.length === 2" class="grid grid-cols-2 gap-3">
        <figure
          v-for="(photo, index) in orderedComparePhotos"
          :key="`compare-${photo.id}`"
          class="bg-white rounded-lg border border-purple-100 p-2"
        >
          <figcaption class="text-[11px] font-semibold text-purple-700 mb-1">{{ index === 0 ? 'Avant' : 'Après' }}</figcaption>
          <img :src="photo.photoUrl" alt="Photo comparaison acné" class="w-full aspect-[3/4] rounded-md object-cover object-top bg-gray-50 mb-2" />
          <p class="text-xs font-semibold text-headline">{{ photo.date }}</p>
          <p class="text-[11px] text-muted-foreground">Intensité {{ photo.intensite }}/10</p>
        </figure>
      </div>

      <div v-else class="rounded-lg border border-dashed border-purple-200 bg-white/70 p-3 text-xs text-muted-foreground">
        <p>Sélectionnez deux photos dans la galerie pour les comparer côte à côte.</p>
        <Button
          v-if="acnePhotoEntries.length >= 2"
          variant="outline"
          size="sm"
          class="mt-2"
          @click="onSelectLatestComparePair"
        >
          <i class="material-symbols-outlined text-sm mr-1">auto_awesome</i>
          Comparer les 2 plus récentes
        </Button>
      </div>

      <div v-if="orderedComparePhotos.length > 0" class="mt-3 flex gap-2 overflow-x-auto pb-1">
        <button
          v-for="photo in orderedComparePhotos"
          :key="`selected-${photo.id}`"
          type="button"
          class="shrink-0 rounded-md border border-purple-200 bg-white px-2 py-1 text-[11px] text-purple-800"
          @click="onTogglePhotoCompare(photo.id)"
        >
          {{ photo.date }} · retirer
        </button>
      </div>
    </section>

    <section class="mt-4 rounded-xl border border-purple-200 bg-white/60 p-3">
      <button
        type="button"
        class="w-full flex items-center justify-between gap-2"
        @click="onToggleAcneHistoryExpanded"
      >
        <h3 class="text-base font-semibold flex items-center gap-2">
          <i class="material-symbols-outlined">history</i>
          Journal acné détaillé
        </h3>
        <span class="inline-flex items-center gap-1 text-xs text-muted-foreground">
          {{ acneHistoryEntriesCount }} entrée{{ acneHistoryEntriesCount > 1 ? 's' : '' }}
          <i class="material-symbols-outlined text-base">{{ isAcneHistoryExpanded ? 'expand_less' : 'expand_more' }}</i>
        </span>
      </button>

      <div v-if="isAcneHistoryExpanded" class="space-y-2 mt-3">
        <div v-if="acneHistoryHiddenByPhotoCount > 0" class="flex justify-end">
          <Button variant="outline" size="sm" @click="onToggleAcneHistoryWithPhotos">
            {{ showAcneHistoryWithPhotos
              ? 'Masquer les entrées avec photo'
              : `Inclure aussi les entrées avec photo (${acneHistoryHiddenByPhotoCount})` }}
          </Button>
        </div>

        <div v-if="displayedAcneHistoryEntries.length > 0" class="space-y-2">
        <GenericCardList
          :entries="displayedAcneHistoryEntries"
          titleField="typeSymptome"
          dateField="date"
          timeField="time"
          intensityField="intensite"
          photoField="photoUrl"
          :extraFields="historyExtraFields"
          :iconConfig="iconConfig"
          :onDelete="onDeleteSymptome"
          :onEdit="onEditSymptome"
          :onPhotoClick="onOpenPhotoModal"
          :hideTitle="true"
          emptyMessage="Aucune entrée acné ce mois"
        />

        <div v-if="acneHistoryEntriesCount > acneHistoryInitialCount" class="flex justify-center pt-1">
          <Button variant="outline" size="sm" @click="onToggleShowFullHistory">
            {{ showFullAcneHistory ? 'Voir moins' : `Voir plus (${acneHistoryEntriesCount - acneHistoryInitialCount})` }}
          </Button>
        </div>
        </div>

        <div v-else class="rounded-xl border border-dashed border-gray-300 p-4 text-sm text-muted-foreground">
          {{ acneHistoryHiddenByPhotoCount > 0 && !showAcneHistoryWithPhotos
            ? 'Aucune entrée sans photo pour ce mois. Activez le filtre pour inclure les entrées avec photo.'
            : 'Aucune entrée acné enregistrée ce mois.' }}
        </div>
      </div>
    </section>
  </section>
</template>


