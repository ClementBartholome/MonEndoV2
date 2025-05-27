<template>
  <section class="md:container flex flex-wrap h-auto mt-20 py-8 bg-clearer rounded-3xl shadow-xl ml-auto">
    <div class="w-full mb-8">
      <div class="flex flex-wrap gap-2 m-2 justify-around">
        <router-link to="/bilan-quotidien" class="w-full flex-1">
          <Card>
            <CardHeader>
              <i class="material-symbols-outlined bg-[#A8D8EA] rounded-full p-2" style="font-size: 40px;">event_note</i>  
              <CardTitle>Bilan quotidien</CardTitle>
            </CardHeader>
            <CardContent>
              <p>
                Humeur, stress, fatigue... fais le point sur ta journée
              </p>
            </CardContent>
          </Card>
        </router-link>
        <Card class="w-full flex-1">
          <router-link to="/cycle">
            <CardHeader>
              <i class="material-symbols-outlined bg-red-200 rounded-full p-2" style="font-size: 40px;">menstrual_health</i>
              <CardTitle>Cycle menstruel</CardTitle>
            </CardHeader>
          </router-link>
          <div v-if="isLoading" class="flex flex-col space-y-3 p-6 pt-0">
            <Skeleton class="h-[52px] w-full rounded-xl"/>
          </div>
          <CardContent v-else class="flex flex-col">
            As-tu eu tes règles aujourd'hui?
            <div class="ml-auto mt-2">
              <Button v-if="!periodMarked" variant="custom" @click="markPeriodToday">Oui</Button>
              <div v-else class="checkmark-animation">
                <i class="material-symbols-outlined" style="font-size: 40px; color: var(--button);">check_circle</i>
              </div>
            </div>
          </CardContent>
        </Card>
        <router-link to="/douleurs" class="w-full flex-1">
          <Card>
            <CardHeader>
              <i class="material-symbols-outlined bg-[#FFB7B2] rounded-full p-2" style="font-size: 40px;">sick</i>
              <CardTitle>Douleurs</CardTitle>
            </CardHeader>
            <div v-if="isLoading" class="flex flex-col space-y-3 p-6 pt-0">
              <Skeleton class="h-[52px] w-full rounded-xl"/>
            </div>
            <CardContent v-else>
              <p v-if="lastDouleurEntry">
                {{ lastDouleurEntry.typeDouleur }} à <br> <span class="highlight">{{ lastDouleurEntry.time }}</span>
                <br> le
                {{ lastDouleurEntry.date }}
              </p>
              <p v-else>Pas de données</p>
            </CardContent>
          </Card>
        </router-link>
        <router-link to="/activite" class="w-full flex-1">
          <Card>
            <CardHeader>
              <i class="material-symbols-outlined bg-teal-200 rounded-full p-2" style="font-size: 40px;">directions_run</i>
              <CardTitle>Activité Physique</CardTitle>
            </CardHeader>
            <div v-if="isLoading" class="flex flex-col space-y-3 p-6 pt-0">
              <Skeleton class="h-[52px] w-full rounded-xl"/>
            </div>
            <CardContent v-else>
              <p v-if="lastActiviteEntry">
                {{ lastActiviteEntry.typeActivite }} à <br><span class="highlight">{{
                  lastActiviteEntry.time
                }}</span><br> le
                {{ lastActiviteEntry.date }}
              </p>
              <p v-else>Pas de données</p>
            </CardContent>
          </Card>
        </router-link>
        <router-link to="/medicaments" class="w-full flex-1">
          <Card>
            <CardHeader>
              <i class="material-symbols-outlined bg-[#B5EAD7] rounded-full p-2" style="font-size: 40px;">medical_services</i>
              <CardTitle>Traitements</CardTitle>
            </CardHeader>
            <div v-if="isLoading" class="flex flex-col space-y-3 p-6 pt-0">
              <Skeleton class="h-[52px] w-full rounded-xl"/>
            </div>
            <CardContent v-else>
              <p v-if="lastMedicamentEntry">
                {{ lastMedicamentEntry.nom }} à <br><span class="highlight">{{ lastMedicamentEntry.heure }}</span> <br>
                le
                {{ lastMedicamentEntry.date }}
              </p>
              <p v-else>Pas de données</p>
            </CardContent>
          </Card>
        </router-link>
        <router-link to="/transit" class="w-full flex-1">
          <Card>
            <CardHeader>
              <i class="material-symbols-outlined bg-[#C7CEEA] rounded-full p-2" style="font-size: 40px;">gastroenterology</i>
              <CardTitle>Transit</CardTitle>
            </CardHeader>
            <div v-if="isLoading" class="flex flex-col space-y-3 p-6 pt-0">
              <Skeleton class="h-[52px] w-full rounded-xl"/>
            </div>
            <CardContent v-else>
              <p v-if="lastTransitEntry">
                {{ lastTransitEntry.typeTransit }} à <br> <span class="highlight">{{ lastTransitEntry.time }}</span>
                <br> le
                {{ lastTransitEntry.date }}
              </p>
              <p v-else>Pas de données</p>
            </CardContent>
          </Card>
        </router-link>
        <router-link to="/agenda" class="w-full col-span-3">
          <Card>
            <CardHeader class="w-full">
              <i class="material-symbols-outlined bg-[#FFDAC1] rounded-full p-2" style="font-size: 40px;">event</i>
              <CardTitle>Mes prochains RDV</CardTitle>
            </CardHeader>
            <CardContent class="flex flex-col md:flex-row text-center gap-4 md:gap-16">
              <div v-if="isLoading" class="flex flex-col space-y-3 p-6 pt-0">
                <Skeleton class="h-[52px] w-full rounded-xl"/>
              </div>
              <div v-else-if="upcomingEvents.length === 0" class="text-start">Pas de rendez-vous à venir</div>
              <div v-else v-for="event in upcomingEvents" :key="event.id"
                   class="rounded-3xl px-4 mb-4 bg-white flex flex-col min-w-40  justify-evenly">
                <h3 class="text-lg">
                  {{ event.summary }}
                </h3>
                <p>
                  {{ event.location }}
                </p>
                <p class="mt-auto text-highlight font-bold">
                  {{
                    event.start.dateTime
                        ? format(new Date(event.start.dateTime), "dd/MM 'à' H'h'mm")
                        : event.start.date
                            ? format(new Date(event.start.date), "dd/MM")
                            : 'Date invalide'
                  }}
                </p>
                </div>
            </CardContent>
          </Card>
        </router-link>
        <router-link to="/export" class="w-full">
          <Card>
            <CardHeader>
              <i class="material-symbols-outlined bg-[#E2F0CB] rounded-full p-2" style="font-size: 40px;">picture_as_pdf</i>
              <CardTitle>Export PDF</CardTitle>
            </CardHeader>
            <CardContent>
              <p>Exportez vos données de santé au format PDF</p>
            </CardContent>
          </Card>
        </router-link>
      </div>
    </div>
  </section>

</template>

<script setup lang="ts">
import {computed, onMounted, ref} from 'vue'

import {type DateValue, getLocalTimeZone} from '@internationalized/date'

import {Card, CardContent, CardHeader, CardTitle,} from '@/components/ui/card'
import {Button} from '@/components/ui/button'
import apiService from "@/services/apiService";
import googleApiService from "@/services/googleApiService";
import {format, parseISO} from 'date-fns';
import {Skeleton} from "@/components/ui/skeleton";
import {useAuthStore} from "@/store/auth";

import {initializeApp} from "firebase/app";
import {getMessaging, getToken, onMessage} from 'firebase/messaging';

const carnetSanteId = useAuthStore().user!.carnetSanteId;
const donneesCarnetSante = ref();
const isLoading = ref(true);
const periodMarked = ref(false);

const upcomingEvents = ref<Event[]>([]);

onMounted(async () => {
  donneesCarnetSante.value = await apiService.getLastDonneesCarnetSante(carnetSanteId);
  const today = format(new Date(), 'yyyy-MM-dd');
  const jourRegleDate = donneesCarnetSante.value?.jourRegle?.date ? format(parseISO(donneesCarnetSante.value.jourRegle.date), 'yyyy-MM-dd') : null;
  if (jourRegleDate === today) {
    periodMarked.value = true;
  }
  isLoading.value = false;

  // const urlParams = new URLSearchParams(window.location.search);
  // let accessToken: any = urlParams.get('accessToken');
  // let refreshToken: any = urlParams.get('refreshToken');
  //
  // // Store tokens in localStorage if they are in the URL
  // if (accessToken) {
  //   localStorage.setItem('googleAccessToken', accessToken);
  // }
  //
  // if (refreshToken) {
  //   localStorage.setItem('googleRefreshToken', refreshToken);
  // }
  //
  // // If the tokens are not in the URL, try to get them from localStorage
  // if (!accessToken) {
  //   accessToken = localStorage.getItem('googleAccessToken');
  // }
  // if (!refreshToken) {
  //   refreshToken = localStorage.getItem('googleRefreshToken');
  // }
  //
  // // Redirect if both tokens are still null after attempting to retrieve them
  // if (!accessToken && !refreshToken) {
  //   window.location.href = await googleApiService.getAuthorizationUrl(userId);
  //   return;
  // }
  //
  // try {
  //   upcomingEvents.value = await googleApiService.getUpcomingEvents(accessToken);
  //   console.log(upcomingEvents.value)
  //   isLoading.value = false;
  // } catch (error: any) {
  //   if ((error.status === 401 || error.response.data.error.message.includes('invalid authentication credentials')) && refreshToken) { // Invalid token
  //     try {
  //       // Use the refresh token to get a new access token
  //       accessToken = await googleApiService.refreshAccessToken(userId, refreshToken);
  //       localStorage.setItem('googleAccessToken', accessToken);
  //
  //       // Retry the request with the new access token
  //       upcomingEvents.value = await googleApiService.getUpcomingEvents(accessToken);
  //       isLoading.value = false;
  //     } catch (refreshError) {
  //       console.error(refreshError);
  //       window.location.href = await googleApiService.getAuthorizationUrl(userId);
  //     }
  //   } else {
  //     console.error(error);
  //   }
  // }
});


onMounted(async () => {
  const response = await googleApiService.getThreeNextEvents();
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
      nom: lastEntry.medicament.nom,
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

const markPeriodToday = async () => {
  const today = format(new Date(), 'yyyy-MM-dd');
  await apiService.postJourRegle({date: today, carnetSanteId: carnetSanteId});
  periodMarked.value = true;
};

const formatDate = (date: Date | DateValue) => {
  const day = new Intl.DateTimeFormat('fr', {day: 'numeric'}).format(date instanceof Date ? date : date.toDate(getLocalTimeZone()));
  const monthYear = new Intl.DateTimeFormat('fr', {
    month: 'long',
    year: 'numeric'
  }).format(date instanceof Date ? date : date.toDate(getLocalTimeZone()));
  return `${day === '1' ? '1er' : day} ${monthYear}`;
};

// Firebase 
// const firebaseConfig = {
//   apiKey: "AIzaSyARgk0J8Uoge1GMFrDvKy3DDiS_NbMAgMk",
//   authDomain: "monendo-c8eb1.firebaseapp.com",
//   projectId: "monendo-c8eb1",
//   storageBucket: "monendo-c8eb1.firebasestorage.app",
//   messagingSenderId: "802043569398",
//   appId: "1:802043569398:web:f4328ecde5b7c74b4038d7"
// };
//
// const app = initializeApp(firebaseConfig);
// const messaging = getMessaging(app);
//
// // Fonction d'initialisation de FCM
// async function initializeFCM() {
//   try {
//     // Vérifier et demander la permission des notifications
//     if (Notification.permission !== "granted") {
//       await Notification.requestPermission();
//     }
//
//     // Enregistrer le service worker
//     if ('serviceWorker' in navigator) {
//       const registration = await navigator.serviceWorker.register('../firebase-messaging-sw.js', {
//         scope: '/'
//       });
//       console.log('Service Worker registered with scope:', registration.scope);
//     }
//
//     // Obtenir le token
//     const currentToken = await getToken(messaging, {
//       vapidKey: 'BEDLjyhj2TWhs4OUhotP_hPQ3J4IBJle5xx58B9pXOVscRDFGtSGumwGoHhg4AZVWbftCy3-s8w_j9H1cWIZs7Q',
//       serviceWorkerRegistration: await navigator.serviceWorker.getRegistration()
//     });
//
//     if (currentToken) {
//       console.log('FCM registration token:', currentToken);
//       if (!user.deviceToken) {
//         user.deviceToken = currentToken;
//         await apiService.saveDeviceToken(currentToken);
//       }
//       setupMessageListener();
//       return currentToken;
//     } else {
//       console.log('No registration token available.');
//       return null;
//     }
//   } catch (err) {
//     console.error('FCM initialization error:', err);
//     return null;
//   }
// }
//
// onMounted(() => {
//   setTimeout(() => {
//     initializeFCM().then((token) => {
//       // if (token) {
//       //   sendNotification(token);
//       // }
//     }).catch(console.error);
//   }, 10000);
// });
//
// function setupMessageListener() {
//   onMessage(messaging, (payload) => {
//     const notificationTitle = payload.notification.title;
//     const notificationOptions = {
//       body: payload.notification.body
//     };
//
//     if (Notification.permission === "granted") {
//       new Notification(notificationTitle, notificationOptions);
//     }
//   });
// }

// const sendNotification = async (deviceToken) => {
//   try {
//     const notificationData = {
//       title: 'Test Notification',
//       body: 'This is a test notification',
//       deviceToken: deviceToken
//     };
//    
//     const response = await axios.post('https://monendoapp.fr/Message/SendNotification',
//         notificationData,
//         {
//           headers: {
//             'Content-Type': 'application/json'
//           }
//         }
//     );
//     console.log('Notification sent successfully:', response.data);
//     return response.data;
//   } catch (error) {
//     console.error('Error sending notification:', error);
//     if (error.response) {
//       console.error('Error response:', error.response.data);
//     }
//     throw error;
//   }
// };




</script>

<style scoped>
.checkmark-animation {
  display: flex;
  align-items: center;
  justify-content: center;
  animation: fadeIn 0.5s ease-in-out;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: scale(0.5);
  }
  to {
    opacity: 1;
    transform: scale(1);
  }
}
</style>