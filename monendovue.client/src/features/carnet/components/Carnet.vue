<template>
  <section class="md:container flex flex-wrap h-auto mt-20 py-8 bg-clearer rounded-3xl shadow-xl ml-auto">
    <div v-if="!isOnline" class="w-full mb-4 p-3 bg-yellow-100 border border-yellow-400 rounded-lg text-yellow-800">
      <i class="material-symbols-outlined mr-2">cloud_off</i>
      Mode hors ligne - Affichage des dernières données sauvegardées
    </div>

    <!-- Pending operations banner -->
    <div v-if="pendingOperationsCount > 0" class="w-full mb-4 p-3 bg-blue-100 border border-blue-400 rounded-lg text-blue-800 flex items-center justify-between">
      <div class="flex items-center">
        <i class="material-symbols-outlined mr-2">sync</i>
        <span>{{ pendingOperationsCount }} opération(s) en attente de synchronisation</span>
      </div>
      <Button
        v-if="isOnline"
        variant="custom"
        @click="performSync"
        :disabled="isSyncing"
        class="ml-4"
      >
        <i v-if="!isSyncing" class="material-symbols-outlined mr-1">cloud_upload</i>
        <i v-else class="material-symbols-outlined mr-1 animate-spin">sync</i>
        {{ isSyncing ? 'Synchronisation...' : 'Synchroniser' }}
      </Button>
    </div>
    <!-- Bilan quotidien : pleine largeur, action principale du jour -->
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

    <!-- Grille 2 colonnes pour les sections secondaires -->
    <div class="w-full mb-3 px-2">
      <div class="grid grid-cols-2 gap-3">

        <router-link to="/cycle">
          <div class="bg-white rounded-2xl p-4 h-full shadow-sm hover:shadow-md transition-shadow border border-gray-100">
            <i class="material-symbols-outlined bg-red-100 text-red-500 rounded-lg p-2 mb-2" style="font-size: 24px;">menstrual_health</i>
            <p class="font-semibold text-headline text-sm">Cycle</p>
            <p class="text-xs text-muted-foreground mt-1">Règles & symptômes</p>
          </div>
        </router-link>

        <router-link to="/douleurs">
          <div class="bg-white rounded-2xl p-4 h-full shadow-sm hover:shadow-md transition-shadow border border-gray-100">
            <i class="material-symbols-outlined bg-orange-100 text-orange-500 rounded-lg p-2 mb-2" style="font-size: 24px;">sick</i>
            <p class="font-semibold text-headline text-sm">Douleurs</p>
            <div v-if="isLoading" class="mt-1 h-2 bg-gray-200 rounded-full w-3/4"></div>
            <p v-else-if="lastDouleurEntry" class="text-xs text-muted-foreground mt-1 truncate">
              {{ lastDouleurEntry.typeDouleur }} — {{ lastDouleurEntry.date }}
            </p>
            <p v-else class="text-xs text-muted-foreground mt-1 italic">Pas de données</p>
          </div>
        </router-link>

        <router-link to="/activite">
          <div class="bg-white rounded-2xl p-4 h-full shadow-sm hover:shadow-md transition-shadow border border-gray-100">
            <i class="material-symbols-outlined bg-teal-100 text-teal-500 rounded-lg p-2 mb-2" style="font-size: 24px;">directions_run</i>
            <p class="font-semibold text-headline text-sm">Activité</p>
            <div v-if="isLoading" class="mt-1 h-2 bg-gray-200 rounded-full w-3/4"></div>
            <p v-else-if="lastActiviteEntry" class="text-xs text-muted-foreground mt-1 truncate">
              {{ lastActiviteEntry.typeActivite }} — {{ lastActiviteEntry.date }}
            </p>
            <p v-else class="text-xs text-muted-foreground mt-1 italic">Pas de données</p>
          </div>
        </router-link>

        <router-link to="/medicaments">
          <div class="bg-white rounded-2xl p-4 h-full shadow-sm hover:shadow-md transition-shadow border border-gray-100">
            <i class="material-symbols-outlined bg-green-100 text-green-500 rounded-lg p-2 mb-2" style="font-size: 24px;">pill</i>
            <p class="font-semibold text-headline text-sm">Traitements</p>
            <div v-if="isLoading" class="mt-1 h-2 bg-gray-200 rounded-full w-3/4"></div>
            <p v-else-if="lastMedicamentEntry" class="text-xs text-muted-foreground mt-1 truncate">
              {{ lastMedicamentEntry.nom }} — {{ lastMedicamentEntry.date }}
            </p>
            <p v-else class="text-xs text-muted-foreground mt-1 italic">Pas de données</p>
          </div>
        </router-link>

        <router-link to="/transit" class="col-span-2">
          <div class="bg-white rounded-2xl p-4 shadow-sm hover:shadow-md transition-shadow border border-gray-100 flex items-center gap-4">
            <i class="material-symbols-outlined bg-purple-100 text-purple-500 rounded-lg p-2 shrink-0" style="font-size: 24px;">gastroenterology</i>
            <div class="flex-1 min-w-0">
              <p class="font-semibold text-headline text-sm">Transit</p>
              <div v-if="isLoading" class="mt-1 h-2 bg-gray-200 rounded-full w-1/2"></div>
              <p v-else-if="lastTransitEntry" class="text-xs text-muted-foreground truncate">
                {{ lastTransitEntry.typeTransit }} — {{ lastTransitEntry.date }}
              </p>
              <p v-else class="text-xs text-muted-foreground italic">Pas de données</p>
            </div>
            <i class="material-symbols-outlined text-muted-foreground shrink-0">chevron_right</i>
          </div>
        </router-link>

      </div>
    </div>

    <!-- Prochains RDV -->
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
          <div v-for="event in upcomingEvents" :key="event.id"
               class="flex-1 rounded-xl px-4 py-3 bg-amber-50 border border-amber-100">
            <h4 class="font-medium text-headline text-sm">{{ event.summary }}</h4>
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
          </div>
        </div>
      </div>
    </div>

  </section>

</template>

<script setup lang="ts">
import {computed, onMounted, ref} from 'vue'

import {type DateValue, getLocalTimeZone} from '@internationalized/date'

import {Card, CardContent, CardHeader, CardTitle,} from '@/shared/components/ui/card'
import {Button} from '@/shared/components/ui/button'
import apiService from "@/shared/services/apiService";
import googleApiService from "@/shared/services/googleApiService";
import offlineStorage from "@/shared/services/offlineStorage";
import syncService from "@/shared/services/syncService";
import {format, parseISO} from 'date-fns';
import {Skeleton} from "@/shared/components/ui/skeleton";
import {useAuthStore} from "@/features/auth/store/auth";
import {useOnlineStatus} from "@/shared/composables/useOnlineStatus";
import {useSync} from "@/shared/composables/useSync";
import {useToast} from "@/shared/components/ui/toast";

import {initializeApp} from "firebase/app";
import {getMessaging, getToken, onMessage} from 'firebase/messaging';

const carnetSanteId = useAuthStore().user!.carnetSanteId;
const donneesCarnetSante = ref();
const isLoading = ref(true);
const { isOnline } = useOnlineStatus();
const { toast } = useToast();
const { pendingOperationsCount, isSyncing, performSync, updatePendingCount } = useSync();

const upcomingEvents = ref<Event[]>([]);

onMounted(async () => {
  await offlineStorage.init();

  // Auto-sync when coming back online
  window.addEventListener('online', async () => {
    console.log('App is online - auto-syncing...');
    await performSync();
  });
  
  const cachedData = await offlineStorage.getCarnetData(carnetSanteId);
  if (cachedData) {
    donneesCarnetSante.value = cachedData;
    isLoading.value = false;
  }
  
  if (navigator.onLine) {
    try {
      const freshData = await apiService.getLastDonneesCarnetSante(carnetSanteId);
      if (freshData) {
        donneesCarnetSante.value = freshData;
        await offlineStorage.saveCarnetData(carnetSanteId, freshData);
      }
    } catch (error) {
      console.log('Failed to fetch fresh data, using cached data if available');
    }
  }
  
  isLoading.value = false;
});


onMounted(async () => {
  const cachedEvents = await offlineStorage.getCalendarEvents();
  if (cachedEvents) {
    upcomingEvents.value = cachedEvents.map(event => ({
      id: event.id,
      summary: event.summary || '',
      description: event.description || '',
      location: event.location || '',
      start: {
        dateTime: event.start.dateTime || '',
        date: event.start.date || ''
      }
    }));
  }
  
  // Try to fetch fresh calendar events if online
  if (navigator.onLine) {
    try {
      const response = await googleApiService.getThreeNextEvents();
      if (response) {
        upcomingEvents.value = response.map(event => ({
          id: event.id,
          summary: event.summary || '',
          description: event.description || '',
          location: event.location || '',
          start: {
            dateTime: event.start.dateTime || '',
            date: event.start.date || ''
          }
        }));
        await offlineStorage.saveCalendarEvents(response);
      }
    } catch (error) {
      console.log('Failed to fetch fresh calendar events, using cached events if available');
    }
  }
})

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

const lastDouleurEntry = computed(() => {
  if (donneesCarnetSante.value && donneesCarnetSante.value.donneesDouleur) {
    const lastEntry = donneesCarnetSante.value.donneesDouleur;
    const date = new Date(lastEntry.date);
    const hours = date.getHours().toString().padStart(2, '0');
    const minutes = date.getMinutes().toString().padStart(2, '0');
    const time = `${hours}h${minutes}`;
    return {
      typeDouleur: lastEntry.typeDouleur,
      time: time,
      date: date.toLocaleDateString()
    };
  }
  return null;
});

const lastActiviteEntry = computed(() => {
  if (donneesCarnetSante.value && donneesCarnetSante.value.donneesActivitePhysique) {
    const lastEntry = donneesCarnetSante.value.donneesActivitePhysique;
    const date = new Date(lastEntry.date);
    const hours = date.getHours().toString().padStart(2, '0');
    const minutes = date.getMinutes().toString().padStart(2, '0');
    const time = `${hours}h${minutes}`;
    return {
      typeActivite: lastEntry.typeActivite,
      time: time,
      date: date.toLocaleDateString()
    };
  }
  return null;
});

const lastMedicamentEntry = computed(() => {
  if (donneesCarnetSante.value && donneesCarnetSante.value.donneesMedicament) {
    const lastEntry = donneesCarnetSante.value.donneesMedicament;
    const date = new Date(lastEntry.date);
    const hours = date.getHours().toString().padStart(2, '0');
    const minutes = date.getMinutes().toString().padStart(2, '0');
    const time = `${hours}h${minutes}`;
    return {
      nom: lastEntry.nomMedicament,
      heure: time,
      date: date.toLocaleDateString()
    };
  }
  return null;
});

const lastTransitEntry = computed(() => {
  if (donneesCarnetSante.value && donneesCarnetSante.value.donneesTransit) {
    const lastEntry = donneesCarnetSante.value.donneesTransit;
    const date = new Date(lastEntry.date);
    const hours = date.getHours().toString().padStart(2, '0');
    const minutes = date.getMinutes().toString().padStart(2, '0');
    const time = `${hours}h${minutes}`;
    return {
      typeTransit: lastEntry.typeEvenement,
      time: time,
      date: date.toLocaleDateString()
    };
  }
  return null;
});

</script>