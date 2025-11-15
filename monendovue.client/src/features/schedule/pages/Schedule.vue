<template>
  <section class="container mt-20">
    <div class="flex justify-between items-center w-full gap-4 mb-4">
      <router-link to="/">
        <Button variant="custom"
                class="flex gap-2 items-center cursor-pointer hover:opacity-80 transition-opacity">
          <i class="material-symbols-outlined ">arrow_back</i>
          <span class="hide-xsm">Revenir en arrière</span>
        </Button>
      </router-link>
      <Button variant="custom" @click="refreshData"
              class="flex gap-2 items-center cursor-pointer hover:opacity-80 transition-opacity">
        <span class="hide-xsm">Actualiser les données</span>
        <span class="material-symbols-outlined">refresh</span>
      </Button>
    </div>

    <div v-if="loading">Chargement des données du calendrier...</div>
    <div v-else-if="events.length === 0">Aucun événement trouvé</div>
    <FullCalendar v-else :options="calendarOptions"/>
  </section>
</template>

<script setup lang="ts">
import {computed, onMounted, onUnmounted, ref, type Ref} from 'vue'
import FullCalendar from '@fullcalendar/vue3'
import googleCalendarPlugin from '@fullcalendar/google-calendar';
import dayGridMonth from "@fullcalendar/daygrid";
import dayGridWeek from "@fullcalendar/daygrid";
import frLocale from '@fullcalendar/core/locales/fr';
import interactionPlugin from "@fullcalendar/interaction";
import 'vue-popperjs/dist/vue-popper.css';
import {Button} from '@/shared/components/ui/button';
import type {CalendarEvent} from '@/features/schedule/models/calendar-events/calendar-event';
import type { CalendarOptions } from '@fullcalendar/core' 

const events = ref<CalendarEvent[]>([]);
const loading = ref(true);
const selectedDate = ref<string | null>(null);

type PositionType = { x: number; y: number; };
const popperPosition: Ref<PositionType> = ref({x: 0, y: 0});

let refreshIntervalId: number;

const fetchEvents = async () => {
  localStorage.removeItem('events')
  events.value = [];
  loading.value = true;

  const calendarOptions = {
    googleCalendarApiKey: import.meta.env.VITE_GOOGLE_CALENDAR_API_KEY,
    googleCalendarId: import.meta.env.VITE_GOOGLE_CALENDAR_ID
  };

  try {
    let allEvents: CalendarEvent[] = [];
    let pageToken: string | undefined;

    do {
      const url = `https://www.googleapis.com/calendar/v3/calendars/${calendarOptions.googleCalendarId}/events?key=${calendarOptions.googleCalendarApiKey}${pageToken ? '&pageToken=' + pageToken : ''}`;
      const response = await fetch(url);
      const data = await response.json();

      // Mapper les événements au format FullCalendar
      const mappedEvents: CalendarEvent[] = data.items
          .filter((item: any) => item.summary) // Filtrer les événements sans titre
          .map((item: any) => ({
            title: item.summary,
            start: item.start.dateTime || item.start.date,
            end: item.end.dateTime || item.end.date,
            url: item.htmlLink
          }));

      allEvents = [...allEvents, ...mappedEvents];
      pageToken = data.nextPageToken;

    } while (pageToken);

    // Sauvegarder et mettre à jour l'état
    events.value = allEvents;
    localStorage.setItem('events', JSON.stringify(allEvents));
  } catch (error) {
    console.error('Erreur lors de la récupération des événements:', error);
  } finally {
    loading.value = false;
  }
};

const calendarOptions = computed<CalendarOptions>(() => { 
  const isMobile = window.matchMedia('(max-width: 767px)').matches;
  const initialView = isMobile ? 'dayGridFourWeek' : 'dayGridMonth';

  return {
    plugins: [googleCalendarPlugin, dayGridMonth, interactionPlugin, dayGridWeek],
    initialView: initialView,
    views: {
      dayGridFourWeek: {
        type: 'dayGridWeek',
        duration: {days: 4},
        dayHeaderFormat: { weekday: 'narrow', day: 'numeric', omitCommas: true }
      }
    },
    headerToolbar: {
      left: 'prev,next today',
      center: 'title',
      right: 'dayGridMonth,dayGridFourWeek'
    },
    buttonText: {
      dayGridFourWeek: 'semaine'
    },
    googleCalendarApiKey: import.meta.env.VITE_GOOGLE_CALENDAR_API_KEY,
    events: events.value, // Utilisation directe des événements mappés
    height: 850,
    locale: frLocale,
    dateClick: function (info: any) {
      selectedDate.value = info.dateStr;
      popperPosition.value = {
        x: info.jsEvent.clientX,
        y: info.jsEvent.clientY
      };
    },
  }
});

onMounted(async () => {
  const localEvents = localStorage.getItem('events');

  if (localEvents) {
    try {
      events.value = JSON.parse(localEvents) as CalendarEvent[];
      loading.value = false;
    } catch (error) {
      console.error('Erreur lors du parsing des événements localStorage:', error);
      await fetchEvents();
    }
  } else {
    await fetchEvents();
  }

  // Actualiser les événements toutes les 60 secondes
  refreshIntervalId = setInterval(fetchEvents, 60000);
});

onUnmounted(() => {
  if (refreshIntervalId) {
    clearInterval(refreshIntervalId);
  }
});

// Actions
const refreshData = async () => {
  await fetchEvents();
};
</script>
