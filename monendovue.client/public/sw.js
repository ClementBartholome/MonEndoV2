// Service worker d'autodestruction.
//
// La PWA (Workbox, /sw.js) a été abandonnée, mais des navigateurs ont encore l'ancien
// service worker installé : il sert une version figée de l'application et garde en cache
// des réponses API. Les navigateurs vérifient périodiquement /sw.js : ce fichier remplace
// l'ancien worker, vide tous les caches, se désinscrit puis recharge les onglets ouverts.
// Il ne touche pas au worker OneSignal (/OneSignalSDKWorker.js), enregistré séparément.
//
// À supprimer après le 2026-12-31.

self.addEventListener('install', () => {
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    event.waitUntil((async () => {
        const cacheNames = await caches.keys();
        await Promise.all(cacheNames.map((name) => caches.delete(name)));
        await self.registration.unregister();
        const windowClients = await self.clients.matchAll({ type: 'window' });
        windowClients.forEach((client) => client.navigate(client.url));
    })());
});
