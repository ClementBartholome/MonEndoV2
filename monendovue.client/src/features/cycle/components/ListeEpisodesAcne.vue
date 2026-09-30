<template>
  <section v-if="episodes.length" aria-labelledby="titre-episodes" class="flex flex-col gap-2">
    <h2 id="titre-episodes" class="m-0 text-titre-carte font-semibold tracking-normal text-texte">Épisodes</h2>
    <ul class="m-0 flex list-none flex-col rounded-carte bg-surface p-0 shadow-elevation">
      <li v-for="(episode, index) in affiches" :key="episode.id" :class="{ 'border-t border-trait': index > 0 }">
        <button type="button" class="flex min-h-[58px] w-full items-center gap-3 px-3.5 py-2.5 text-left" @click="emit('modifier', episode)">
          <span class="grow text-corps text-texte">
            {{ periode(episode) }}
          </span>
          <span class="shrink-0 text-sm font-semibold text-texte-2">{{ jours(episode.jours) }}</span>
          <i class="material-symbols-outlined text-texte-3" aria-hidden="true">chevron_right</i>
        </button>
      </li>
    </ul>
    <button v-if="!tous && episodes.length > LIMITE" type="button" class="inline-flex min-h-11 items-center gap-1.5 self-start text-sm font-medium text-lien" @click="tous = true">
      <i class="material-symbols-outlined text-lg" aria-hidden="true">expand_more</i>Voir tous les épisodes ({{ episodes.length }})
    </button>
  </section>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import type { EpisodeAcne } from '../types/acne';
import { jourCourt, jours } from '../utils/cycle';

const props = defineProps<{ episodes: EpisodeAcne[] }>();

/** Les épisodes durent des semaines : une poignée par an, tous chargés, mais seuls les plus récents sont affichés d'abord. */
const LIMITE = 6;
const tous = ref(false);
const affiches = computed(() => (tous.value ? props.episodes : props.episodes.slice(0, LIMITE)));
const emit = defineEmits<{ modifier: [episode: EpisodeAcne] }>();

/** « 3 août → 21 août », « 3 août → en cours », ou le jour seul pour un épisode d'une journée. */
function periode(episode: EpisodeAcne): string {
  const debut = jourCourt(episode.debut);
  if (!episode.fin) return `${debut} → en cours`;
  return episode.fin === episode.debut ? debut : `${debut} → ${jourCourt(episode.fin)}`;
}
</script>
