import type { FauxServeur } from './faux-serveur';

/**
 * Routes simulées chargées à l'ouverture des paramètres (`NotificationsController`) : notifications non configurées
 * sur le serveur (404 sur la clé publique), la page s'affiche sans le réglage des rappels.
 */
export function simulerParametresSansNotifications(serveur: FauxServeur) {
  serveur.on('GET', /^Notifications\/cle-publique$/, () => ({ status: 404 }));
}
