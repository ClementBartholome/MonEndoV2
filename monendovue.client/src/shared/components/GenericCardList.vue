<script setup lang="ts">
import type { CardFieldConfig, CardIconConfig } from '@/shared/types/card'

export type { CardFieldConfig, CardIconConfig }

const props = defineProps<{
  entries: any[]
  titleField: string
  dateField: string
  timeField?: string
  intensityField?: string
  extraFields?: CardFieldConfig[]
  photoField?: string
  iconConfig?: Record<string, CardIconConfig>
  defaultIcon?: CardIconConfig
  hideTitle?: boolean
  hideIcon?: boolean
  onDelete: (id: string | number) => Promise<void>
  onEdit?: (id: string | number) => void
  onPhotoClick?: (url: string) => void
  emptyMessage?: string
}>()

const defaultIconFallback: CardIconConfig = {
  color: 'text-gray-500',
  bg: 'bg-gray-100',
  icon: 'help'
}

const getIcon = (value: string): CardIconConfig => {
  if (props.iconConfig && props.iconConfig[value]) return props.iconConfig[value]
  return props.defaultIcon ?? defaultIconFallback
}

const intensityColor = (v: number | string) => {
  const n = Number(v)
  if (n <= 3) return 'bg-green-400'
  if (n <= 6) return 'bg-yellow-400'
  return 'bg-red-400'
}
</script>

<template>
  <div class="flex flex-col gap-3">
    <div
      v-for="entry in entries"
      :key="entry.id"
      class="bg-white rounded-2xl p-4 shadow-sm border border-gray-100"
    >
      <!-- Header: titre + date + actions -->
      <div class="flex items-start justify-between gap-2 mb-3">
        <div class="flex items-center gap-2 min-w-0">
          <span
            v-if="!hideIcon"
            class="material-symbols-outlined rounded-lg p-1.5 text-base shrink-0"
            :class="[getIcon(entry[titleField]).bg, getIcon(entry[titleField]).color]"
          >{{ getIcon(entry[titleField]).icon }}</span>
          <div class="min-w-0">
            <p v-if="!hideTitle" class="font-semibold text-headline text-sm leading-tight truncate">{{ entry[titleField] }}</p>
            <p class="text-xs text-muted-foreground">
              {{ entry[dateField] }}
              <span v-if="timeField && entry[timeField]"> · {{ entry[timeField] }}</span>
            </p>
          </div>
        </div>
        <div class="flex items-center gap-1 shrink-0">
          <button
            v-if="onEdit"
            @click="onEdit(entry.id)"
            class="text-gray-400 hover:text-[var(--button)] transition-colors p-1 rounded-full hover:bg-[var(--background-clearer)]"
            aria-label="Modifier"
          >
            <span class="material-symbols-outlined text-base">edit</span>
          </button>
          <button
            @click="onDelete(entry.id)"
            class="text-gray-400 hover:text-red-500 transition-colors p-1 rounded-full hover:bg-red-50"
            aria-label="Supprimer"
          >
            <span class="material-symbols-outlined text-base">delete</span>
          </button>
        </div>
      </div>

      <!-- Barre d'intensité -->
      <div v-if="intensityField && entry[intensityField]" class="flex items-center gap-2 mb-2">
        <span class="text-xs text-muted-foreground w-16 shrink-0">Intensité</span>
        <div class="flex-1 h-1.5 bg-gray-100 rounded-full overflow-hidden">
          <div
            class="h-full rounded-full transition-all"
            :class="intensityColor(entry[intensityField])"
            :style="{ width: `${(Number(entry[intensityField]) / 10) * 100}%` }"
          ></div>
        </div>
        <span class="text-xs font-semibold text-headline w-6 text-right">{{ entry[intensityField] }}</span>
      </div>

      <!-- Champs extra (duree, commentaire, etc.) -->
      <div class="flex flex-wrap items-center justify-between gap-x-3 gap-y-1 mt-1">
        <template v-for="field in (extraFields ?? [])" :key="field.key">
          <div v-if="!field.hide && entry[field.key] && entry[field.key] !== 'Pas de commentaire'" class="flex items-center gap-1">
            <span v-if="field.label" class="text-xs text-muted-foreground">{{ field.label }} :</span>
            <span class="text-xs text-paragraph italic">{{ entry[field.key] }}{{ field.suffix ?? '' }}</span>
          </div>
        </template>

        <!-- Photo -->
        <button
          v-if="photoField && entry[photoField] && onPhotoClick"
          @click="onPhotoClick(entry[photoField])"
          class="text-[var(--button)] hover:opacity-70 transition-opacity ml-auto"
          aria-label="Voir la photo"
        >
          <span class="material-symbols-outlined text-xl">photo_camera</span>
        </button>
      </div>
    </div>

    <p v-if="entries.length === 0" class="text-center text-muted-foreground py-8 italic">
      {{ emptyMessage ?? 'Aucune donnée enregistrée ce mois' }}
    </p>
  </div>
</template>


