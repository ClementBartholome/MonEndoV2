// Service worker dédié aux notifications Web Push de MonEndo.
// Il n'intercepte aucune requête et ne met rien en cache (pas de mode hors ligne) : il affiche seulement
// les notifications envoyées par le serveur ({ title, body, url }) et ouvre l'app au clic.

self.addEventListener('install', () => {
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    event.waitUntil(self.clients.claim());
});

self.addEventListener('push', (event) => {
    let donnees = {};
    try {
        donnees = event.data ? event.data.json() : {};
    } catch {
        donnees = { body: event.data ? event.data.text() : '' };
    }

    event.waitUntil(
        self.registration.showNotification(donnees.title || 'MonEndo', {
            body: donnees.body || '',
            data: { url: donnees.url || '/' },
        }),
    );
});

self.addEventListener('notificationclick', (event) => {
    event.notification.close();
    const url = new URL(event.notification.data?.url || '/', self.location.origin).href;

    event.waitUntil((async () => {
        const fenetres = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
        const fenetre = fenetres.find((client) => client.url.startsWith(self.location.origin));
        if (fenetre) {
            try {
                await fenetre.focus();
                return await fenetre.navigate(url);
            } catch {
                // Fenêtre non contrôlée par ce worker : on ouvre l'URL à la place.
            }
        }
        return self.clients.openWindow(url);
    })());
});
