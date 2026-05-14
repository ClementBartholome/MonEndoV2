import './assets/index.css'

import { createApp } from 'vue'
import OneSignalVuePlugin from '@onesignal/onesignal-vue3'
import { createPinia } from 'pinia';
import App from './App.vue'
import router from './router'
import ApiService from "@/shared/services/apiService";
// import { registerSW } from 'virtual:pwa-register';

const pinia = createPinia();

createApp(App).use(router).use(pinia).use(OneSignalVuePlugin, {
    appId: "d3434227-a679-4122-b83d-3d1a4e7c1b19",
    // appId: '1d08f33e-c4bb-4157-8939-8d7721eb2fd0',
}).mount('#app')

ApiService.init(pinia);

// if ('serviceWorker' in navigator) {
//     const updateSW = registerSW({
//         immediate: true,
//         onNeedRefresh() {
//             console.log('🔄 New content available, refresh to update');
//         },
//         onOfflineReady() {
//             console.log('📱 App ready to work offline');
//         },
//         onRegistered(r) {
//             console.log('✅ SW Registered:', r);
//         },
//         onRegisterError(error) {
//             console.error('❌ SW registration error:', error);
//         }
//     });
// } else {
//     console.warn('⚠️ Service Worker not supported in this browser');
// }