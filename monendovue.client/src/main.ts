import './assets/index.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia';
import App from './App.vue'
import router from './router'
import ApiService from "@/shared/services/apiService";

const pinia = createPinia();

createApp(App).use(router).use(pinia).mount('#app')

ApiService.init(pinia);

// Nettoyage ponctuel des données laissées par l'ancien mode hors ligne et par OneSignal (abandonnés) :
// base IndexedDB contenant des données de santé, caches localStorage et ancien service worker OneSignal.
// À retirer après le 2026-12-31.
try {
    indexedDB?.deleteDatabase('MonEndoOffline');
    localStorage.removeItem('events');
    localStorage.removeItem('notification-permission');
    navigator.serviceWorker?.getRegistrations().then((registrations) => registrations
        .filter((registration) => registration.active?.scriptURL.endsWith('/OneSignalSDKWorker.js'))
        .forEach((registration) => registration.unregister()))
        .catch(() => undefined);
} catch {
    // Stockage indisponible (navigation privée, etc.) : rien à nettoyer.
}
