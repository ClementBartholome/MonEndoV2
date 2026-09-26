import './assets/index.css'

import { createApp } from 'vue'
import OneSignalVuePlugin from '@onesignal/onesignal-vue3'
import { createPinia } from 'pinia';
import App from './App.vue'
import router from './router'
import ApiService from "@/shared/services/apiService";

const pinia = createPinia();

createApp(App).use(router).use(pinia).use(OneSignalVuePlugin, {
    appId: "d3434227-a679-4122-b83d-3d1a4e7c1b19",
}).mount('#app')

ApiService.init(pinia);

// Nettoyage ponctuel des données laissées par l'ancien mode hors ligne (abandonné) :
// base IndexedDB contenant des données de santé et cache localStorage des événements.
// À retirer après le 2026-12-31.
try {
    indexedDB?.deleteDatabase('MonEndoOffline');
    localStorage.removeItem('events');
} catch {
    // Stockage indisponible (navigation privée, etc.) : rien à nettoyer.
}
