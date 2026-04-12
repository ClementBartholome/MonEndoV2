<script setup lang="ts">
import { symptomeIconConfig } from '@/shared/config/materialSymbols'

const props = defineProps<{
  entries: any[]
  onDelete: (id: string | number) => Promise<void>
  onPhotoClick: (url: string) => void
}>()

const getConfig = (type: string) =>
  symptomeIconConfig[type] ?? { color: 'text-gray-500', bg: 'bg-gray-100', icon: 'help' }

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
      <!-- Header row: type + date + delete -->
      <div class="flex items-start justify-between gap-2 mb-3">
        <div class="flex items-center gap-2">
          <span
            class="material-symbols-outlined rounded-lg p-1.5 text-base"
            :class="[getConfig(entry.typeSymptome).bg, getConfig(entry.typeSymptome).color]"
          >{{ getConfig(entry.typeSymptome).icon }}</span>
          <div>
            <p class="font-semibold text-headline text-sm leading-tight">{{ entry.typeSymptome }}</p>
            <p class="text-xs text-muted-foreground">{{ entry.date }}<span v-if="entry.time && !entry.time.includes('jour')"> · {{ entry.time }}</span><span v-else-if="entry.time?.includes('jour')" class="ml-1 italic">{{ entry.time }}</span></p>
          </div>
        </div>
        <button
          @click="onDelete(entry.id)"
          class="text-gray-400 hover:text-red-500 transition-colors p-1 rounded-full hover:bg-red-50 shrink-0"
          aria-label="Supprimer"
        >
          <span class="material-symbols-outlined text-base">delete</span>
        </button>
      </div>

      <!-- Intensity bar -->
      <div class="flex items-center gap-2 mb-2">
        <span class="text-xs text-muted-foreground w-16 shrink-0">Intensité</span>
        <div class="flex-1 h-1.5 bg-gray-100 rounded-full overflow-hidden">
          <div
            class="h-full rounded-full transition-all"
            :class="intensityColor(entry.intensite)"
            :style="{ width: `${(Number(entry.intensite) / 10) * 100}%` }"
          ></div>
        </div>
        <span class="text-xs font-semibold text-headline w-6 text-right">{{ entry.intensite }}</span>
      </div>

      <!-- Comment + photo -->
      <div class="flex items-center justify-between gap-2 mt-2">
        <p
          v-if="entry.commentaire && entry.commentaire !== 'Pas de commentaire'"
          class="text-xs text-paragraph italic truncate flex-1"
        >
          "{{ entry.commentaire }}"
        </p>
        <span v-else class="text-xs text-muted-foreground italic">Pas de commentaire</span>
        <button
          v-if="entry.photoUrl"
          @click="onPhotoClick(entry.photoUrl)"
          class="text-[var(--button)] hover:opacity-70 transition-opacity shrink-0"
          aria-label="Voir la photo"
        >
          <span class="material-symbols-outlined text-xl">photo_camera</span>
        </button>
      </div>
    </div>

    <p v-if="entries.length === 0" class="text-center text-muted-foreground py-8 italic">
      Aucun symptôme enregistré ce mois
    </p>
  </div>
</template>

