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
      <Button v-if="agendaDisponible" variant="custom" @click="refreshData"
              class="flex gap-2 items-center cursor-pointer hover:opacity-80 transition-opacity">
        <span class="hide-xsm">Actualiser les données</span>
        <span class="material-symbols-outlined">refresh</span>
      </Button>
    </div>

    <p v-if="!agendaDisponible">Aucun agenda n'est associé à ton compte.</p>
    <template v-else>
      <p v-if="loading">Chargement des données du calendrier...</p>
      <p v-if="erreur" class="text-sm text-muted-foreground mb-2">{{ erreur }}</p>
      <FullCalendar ref="calendrier" :options="calendarOptions"/>
    </template>
  </section>
</template>

<script setup lang="ts">
import {ref} from 'vue'
import FullCalendar from '@fullcalendar/vue3'
import dayGridPlugin from "@fullcalendar/daygrid";
import frLocale from '@fullcalendar/core/locales/fr';
import interactionPlugin from "@fullcalendar/interaction";
import type {CalendarOptions, EventInput, EventSourceFuncArg} from '@fullcalendar/core'
import {Button} from '@/shared/components/ui/button';
import apiService from '@/shared/services/apiService';
import type {EvenementAgenda} from '@/features/schedule/types/agenda';

const loading = ref(true);
const agendaDisponible = ref(true);
const erreur = ref<string | null>(null);
const calendrier = ref<InstanceType<typeof FullCalendar> | null>(null);

const versEvenementCalendrier = (evenement: EvenementAgenda): EventInput => ({
  id: evenement.id,
  title: evenement.titre,
  start: evenement.debut,
  end: evenement.fin ?? undefined,
  allDay: evenement.journeeEntiere,
  url: evenement.lien ?? undefined,
});

// Les événements sont demandés au serveur pour la période affichée, à chaque changement de vue ou de mois.
const chargerEvenements = async (periode: EventSourceFuncArg): Promise<EventInput[]> => {
  erreur.value = null;
  try {
    const evenements = await apiService.getEvenementsAgenda(periode.start, periode.end);
    if (evenements === null) {
      agendaDisponible.value = false;
      return [];
    }
    return evenements.map(versEvenementCalendrier);
  } catch {
    erreur.value = "L'agenda est momentanément indisponible. Réessaie dans quelques instants.";
    return [];
  }
};

const isMobile = window.matchMedia('(max-width: 767px)').matches;

const calendarOptions: CalendarOptions = {
  plugins: [dayGridPlugin, interactionPlugin],
  initialView: isMobile ? 'dayGridFourWeek' : 'dayGridMonth',
  views: {
    dayGridFourWeek: {
      type: 'dayGridWeek',
      duration: {days: 4},
      dayHeaderFormat: {weekday: 'narrow', day: 'numeric', omitCommas: true}
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
  events: chargerEvenements,
  loading: (enCours: boolean) => {
    loading.value = enCours;
  },
  height: 850,
  locale: frLocale,
};

// Actions
const refreshData = () => {
  calendrier.value?.getApi().refetchEvents();
};
</script>
