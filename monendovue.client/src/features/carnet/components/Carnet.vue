<template>
  <section class="md:container flex flex-wrap h-auto mt-20 py-8 bg-clearer rounded-3xl shadow-xl ml-auto">
    <div class="w-full mb-3 px-2">
      <router-link to="/bilan-quotidien">
        <div class="bg-white rounded-2xl p-4 shadow-md hover:shadow-lg transition-shadow border border-gray-100 flex items-center gap-4">
          <i class="material-symbols-outlined bg-blue-100 text-blue-600 rounded-xl p-3 shrink-0" style="font-size: 28px;">event_note</i>
          <div class="flex-1 min-w-0">
            <h3 class="font-semibold text-headline">Bilan quotidien</h3>
            <p class="text-sm text-paragraph truncate">Humeur, stress, fatigue... fais le point sur ta journée</p>
          </div>
          <i class="material-symbols-outlined text-muted-foreground shrink-0">chevron_right</i>
        </div>
      </router-link>
    </div>

    <div class="w-full mb-3 px-2">
      <div class="grid grid-cols-2 gap-3 home-section-grid">
        <router-link
          v-for="card in sectionCards"
          :key="card.key"
          :to="card.to"
        >
          <div class="bg-white rounded-2xl p-3 sm:p-4 h-full shadow-sm hover:shadow-md transition-shadow border border-gray-100 flex items-start gap-2 sm:gap-3">
            <i class="material-symbols-outlined rounded-lg p-2 shrink-0" :class="[card.iconBg, card.iconColor]" style="font-size: 24px;">{{ card.icon }}</i>
            <div class="flex-1 min-w-0">
              <p class="font-semibold text-headline text-sm">{{ card.title }}</p>
              <p class="text-xs text-muted-foreground mt-0.5 leading-snug">{{ card.subtitle }}</p>

              <div v-if="isLoading" class="mt-2 h-2 bg-gray-200 rounded-full w-3/4"></div>
              <template v-else-if="card.lastLabel && card.lastDateShort">
                <p
                  class="text-xs text-muted-foreground mt-2 leading-snug"
                  :title="`${card.lastLabel} — ${card.lastDateShort}`"
                >
                  {{ card.lastLabel }} — {{ card.lastDateShort }}
                </p>
                <p class="text-[11px] text-muted-foreground">{{ card.lastDateRelative }}</p>
              </template>
              <p v-else class="text-xs text-muted-foreground mt-2 italic">{{ card.emptyText }}</p>
            </div>
            <div class="shrink-0 flex flex-col items-end gap-2">
              <span
                v-if="card.isStale"
                class="text-[10px] font-medium px-2 py-1 rounded-full bg-red-50 text-red-600 border border-red-100"
              >
                À mettre à jour
              </span>
              <i class="material-symbols-outlined text-muted-foreground">chevron_right</i>
            </div>
          </div>
        </router-link>
      </div>
    </div>

    <div class="w-full px-2 pb-8">
      <div class="bg-white rounded-2xl p-5 shadow-md border border-gray-100">
        <div class="flex items-center gap-3 mb-4">
          <i class="material-symbols-outlined bg-amber-100 text-amber-600 rounded-xl p-3" style="font-size: 32px;">event</i>
          <h3 class="font-semibold text-headline text-lg">Prochains rendez-vous</h3>
        </div>
        <div v-if="isLoading" class="flex flex-col space-y-2">
          <div class="h-3 bg-gray-200 rounded-full w-3/4"></div>
          <div class="h-3 bg-gray-200 rounded-full w-1/2"></div>
        </div>
        <div v-else-if="upcomingEvents.length === 0" class="text-sm text-muted-foreground italic">
          Pas de rendez-vous à venir
        </div>
        <div v-else class="flex flex-col md:flex-row gap-4">
          <component
            v-for="event in upcomingEvents"
            :key="event.id"
            :is="event.location ? 'a' : 'div'"
            :href="event.location ? getDirectionsUrl(event.location) : undefined"
            :target="event.location ? '_blank' : undefined"
            :rel="event.location ? 'noopener noreferrer' : undefined"
            class="flex-1 rounded-xl px-4 py-3 bg-amber-50 border border-amber-100 transition-colors"
            :class="event.location ? 'hover:bg-amber-100 cursor-pointer' : ''"
          >
            <div class="flex items-start justify-between gap-2">
              <h4 class="font-medium text-headline text-sm">{{ event.summary }}</h4>
              <i v-if="event.location" class="material-symbols-outlined text-amber-700 text-base shrink-0">route</i>
            </div>
            <p v-if="event.location" class="text-xs text-paragraph mt-1">{{ event.location }}</p>
            <p class="text-xs font-semibold text-highlight mt-2">
              {{
                event.start.dateTime
                  ? format(new Date(event.start.dateTime), "dd/MM 'à' H'h'mm")
                  : event.start.date
                    ? format(new Date(event.start.date), "dd/MM")
                    : 'Date invalide'
              }}
            </p>
            <p v-if="event.location" class="text-[11px] text-amber-700 mt-1">Ouvrir l'itinéraire</p>
          </component>
        </div>
      </div>
    </div>
  </section>

</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import apiService from "@/shared/services/apiService";
import googleApiService from "@/shared/services/googleApiService";
import { format, formatDistanceToNow, isValid, differenceInCalendarDays } from 'date-fns';
import { fr } from 'date-fns/locale';
import { useAuthStore } from "@/features/auth/store/auth";

const carnetSanteId = useAuthStore().user!.carnetSanteId;
const donneesCarnetSante = ref<any>(null);
const isLoading = ref(true);

const upcomingEvents = ref<Event[]>([]);

const getDirectionsUrl = (location: string) => {
  return `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(location)}`;
};

onMounted(async () => {
  try {
    const [lastEntries, nextEvents] = await Promise.all([
      apiService.getLastDonneesCarnetSante(carnetSanteId),
      googleApiService.getThreeNextEvents()
    ]);

    donneesCarnetSante.value = lastEntries;
    upcomingEvents.value = (nextEvents ?? []).map(event => ({
      id: event.id,
      summary: event.summary || '',
      description: event.description || '',
      location: event.location || '',
      start: {
        dateTime: event.start.dateTime || '',
        date: event.start.date || ''
      }
    }));
  } catch (error) {
    console.error('Erreur de chargement de la page d\'accueil:', error);
  } finally {
    isLoading.value = false;
  }
});

interface Event {
  id: string;
  summary: string;
  description: string;
  location: string;
  start: {
    dateTime: string;
    date: string;
  };
}

interface LastEntryInfo {
  label: string;
  date: Date;
}

interface SectionCard {
  key: string;
  to: string;
  title: string;
  subtitle: string;
  icon: string;
  iconBg: string;
  iconColor: string;
  emptyText: string;
  staleAfterDays: number;
  lastLabel?: string;
  lastDateShort?: string;
  lastDateRelative?: string;
  isStale: boolean;
}

const getEntryDate = (value: string | Date | undefined | null): Date | null => {
  if (!value) return null;
  const date = new Date(value);
  return isValid(date) ? date : null;
};

const buildLastEntry = (label: string | undefined | null, dateValue: string | Date | undefined | null): LastEntryInfo | null => {
  const date = getEntryDate(dateValue);
  if (!label || !date) return null;
  return { label, date };
};

const getDateDisplay = (date: Date) => {
  return {
    short: format(date, 'dd/MM/yyyy'),
    relative: formatDistanceToNow(date, { addSuffix: true, locale: fr }),
    ageInDays: differenceInCalendarDays(new Date(), date)
  };
};

const lastDouleurEntry = computed(() => {
  const entry = donneesCarnetSante.value?.donneesDouleur;
  return buildLastEntry(entry?.typeDouleur, entry?.date);
});

const lastActiviteEntry = computed(() => {
  const entry = donneesCarnetSante.value?.donneesActivitePhysique;
  return buildLastEntry(entry?.typeActivite, entry?.date);
});

const lastMedicamentEntry = computed(() => {
  const entry = donneesCarnetSante.value?.donneesMedicament;
  return buildLastEntry(entry?.nomMedicament, entry?.date);
});

const lastTransitEntry = computed(() => {
  const entry = donneesCarnetSante.value?.donneesTransit;
  return buildLastEntry(entry?.typeEvenement, entry?.date);
});

const lastCycleEntry = computed(() => {
  const entry = donneesCarnetSante.value?.jourRegle;
  const date = getEntryDate(entry?.date);
  if (!date) return null;
  return { label: 'Dernières règles', date };
});

const sectionCards = computed<SectionCard[]>(() => {
  const config = [
    {
      key: 'cycle',
      to: '/cycle',
      title: 'Cycle',
      subtitle: 'Règles et symptômes',
      icon: 'menstrual_health',
      iconBg: 'bg-red-100',
      iconColor: 'text-red-500',
      emptyText: 'Aucune donnée cycle',
      staleAfterDays: 35,
      last: lastCycleEntry.value,
    },
    {
      key: 'douleurs',
      to: '/douleurs',
      title: 'Douleurs',
      subtitle: 'Suivi des épisodes',
      icon: 'sick',
      iconBg: 'bg-orange-100',
      iconColor: 'text-orange-500',
      emptyText: 'Aucune douleur enregistrée',
      staleAfterDays: 7,
      last: lastDouleurEntry.value,
    },
    {
      key: 'activite',
      to: '/activite',
      title: 'Activité',
      subtitle: 'Sessions et types d\'activité',
      icon: 'directions_run',
      iconBg: 'bg-teal-100',
      iconColor: 'text-teal-500',
      emptyText: 'Aucune activité enregistrée',
      staleAfterDays: 5,
      last: lastActiviteEntry.value,
    },
    {
      key: 'medicaments',
      to: '/medicaments',
      title: 'Traitements',
      subtitle: 'Prises et sessions',
      icon: 'pill',
      iconBg: 'bg-green-100',
      iconColor: 'text-green-500',
      emptyText: 'Aucun traitement saisi',
      staleAfterDays: 10,
      last: lastMedicamentEntry.value,
    },
    {
      key: 'transit',
      to: '/transit',
      title: 'Transit',
      subtitle: 'Confort digestif',
      icon: 'gastroenterology',
      iconBg: 'bg-purple-100',
      iconColor: 'text-purple-500',
      emptyText: 'Aucune donnée transit',
      staleAfterDays: 4,
      last: lastTransitEntry.value,
    },
    {
      key: 'export',
      to: '/export',
      title: 'Export PDF',
      subtitle: 'Synthèse à partager en consultation',
      icon: 'picture_as_pdf',
      iconBg: 'bg-blue-100',
      iconColor: 'text-blue-600',
      emptyText: 'Disponible à tout moment',
      staleAfterDays: 999,
      last: null,
    },
  ];

  return config.map((card) => {
    const lastEntry = card.last;
    const dateInfo = lastEntry ? getDateDisplay(lastEntry.date) : null;

    return {
      key: card.key,
      to: card.to,
      title: card.title,
      subtitle: card.subtitle,
      icon: card.icon,
      iconBg: card.iconBg,
      iconColor: card.iconColor,
      emptyText: card.emptyText,
      staleAfterDays: card.staleAfterDays,
      lastLabel: lastEntry?.label,
      lastDateShort: dateInfo?.short,
      lastDateRelative: dateInfo?.relative,
      isStale: !!dateInfo && dateInfo.ageInDays > card.staleAfterDays,
    };
  });
});

</script>

<style scoped>
@media (max-width: 425px) {
  .home-section-grid {
    grid-template-columns: 1fr;
  }
}
</style>
