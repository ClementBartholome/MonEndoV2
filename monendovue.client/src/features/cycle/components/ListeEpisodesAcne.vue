<template>
  <section v-if="episodes.length" aria-labelledby="titre-episodes" class="flex flex-col gap-2">
    <h2 id="titre-episodes" class="m-0 text-[17px] font-semibold tracking-normal text-texte">Épisodes</h2>
    <ul class="m-0 flex list-none flex-col rounded-carte bg-surface p-0 shadow-elevation">
      <li v-for="(episode, index) in episodes" :key="episode.id" :class="{ 'border-t border-trait': index > 0 }">
        <button type="button" class="flex min-h-[58px] w-full items-center gap-3 px-3.5 py-2.5 text-left" @click="emit('modifier', episode)">
          <span class="grow text-[15px] text-texte">
            {{ jourCourt(episode.debut) }} → {{ episode.fin ? jourCourt(episode.fin) : 'en cours' }}
          </span>
          <span class="shrink-0 text-sm font-semibold text-texte-2">{{ jours(episode.jours) }}</span>
          <i class="material-symbols-outlined text-texte-3" aria-hidden="true">chevron_right</i>
        </button>
      </li>
    </ul>
  </section>
</template>

<script setup lang="ts">
import type { EpisodeAcne } from '../types/acne';
import { jourCourt, jours } from '../utils/cycle';

defineProps<{ episodes: EpisodeAcne[] }>();
const emit = defineEmits<{ modifier: [episode: EpisodeAcne] }>();
</script>
