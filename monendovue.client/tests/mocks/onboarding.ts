import type { Page } from '@playwright/test';
import type { FauxServeur } from './faux-serveur';

/** Rappels tels que les renvoie `NotificationsController` à un compte sans réglage : le bilan est inactif, à 20 h. */
const RAPPELS = [
  { type: 'BilanQuotidien', estHebdomadaire: false, actif: false, heure: '20:00', jourSemaine: null, fuseauHoraire: 'Europe/Paris' },
  { type: 'SuiviAcne', estHebdomadaire: true, actif: false, heure: '19:00', jourSemaine: 0, fuseauHoraire: 'Europe/Paris' },
];

/** Notifications configurées sur le serveur : clé publique, abonnement d'appareil, rappels (le dernier PUT du bilan est gardé). */
export function simulerNotifications(serveur: FauxServeur) {
  serveur
    .on('GET', /^Notifications\/cle-publique$/, () => ({ body: { clePublique: 'AQIDBA' } }))
    .on('POST', /^Notifications\/abonnements$/, () => ({ status: 204 }))
    .on('GET', /^Notifications\/rappels$/, () => ({ body: RAPPELS }))
    .on('PUT', /^Notifications\/rappels\/[A-Za-z]+$/, () => ({ status: 204 }));
}

/**
 * Navigateur qui sait recevoir des notifications : abonnement factice, et permission qui sera accordée ou refusée au clic
 * (la vraie fenêtre de permission n'existe pas en test).
 */
export async function simulerNavigateurPush(page: Page, permission: 'granted' | 'denied') {
  await page.addInitScript((reponse) => {
    const abonnement = {
      endpoint: 'https://push.example/abonnement',
      options: { applicationServerKey: null },
      toJSON: () => ({ keys: { p256dh: 'Faux', auth: 'Faux' } }),
      unsubscribe: async () => true,
    };
    const registration = { pushManager: { getSubscription: async () => null, subscribe: async () => abonnement } };
    Object.defineProperty(Notification, 'permission', { get: () => 'default', configurable: true });
    Notification.requestPermission = async () => reponse;
    Object.defineProperty(navigator.serviceWorker, 'register', { value: async () => registration, configurable: true });
    Object.defineProperty(navigator.serviceWorker, 'getRegistration', { value: async () => undefined, configurable: true });
    Object.defineProperty(navigator.serviceWorker, 'ready', { get: () => Promise.resolve(registration), configurable: true });
  }, permission);
}

/** iPhone dans Safari, hors écran d'accueil : pas de Web Push, donc l'étape explique l'installation. */
export async function simulerIphoneDansSafari(page: Page) {
  await page.addInitScript(() => {
    Object.defineProperty(navigator, 'userAgent', {
      get: () => 'Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1',
    });
    delete (window as unknown as { PushManager?: unknown }).PushManager;
  });
}
