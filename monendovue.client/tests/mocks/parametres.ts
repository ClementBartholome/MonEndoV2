import type { FauxServeur } from './faux-serveur';

/**
 * Routes simulées chargées à l'ouverture des paramètres (`NotificationsController`) : notifications non configurées
 * sur le serveur (404 sur la clé publique), la page s'affiche sans le réglage des rappels.
 */
export function simulerParametresSansNotifications(serveur: FauxServeur) {
  serveur.on('GET', /^Notifications\/cle-publique$/, () => ({ status: 404 }));
}

/** Export des données (`DonneesPersonnellesController`) : archive factice, ou échec du serveur. */
export function simulerExportDonnees(serveur: FauxServeur, reussi = true) {
  serveur.on('GET', /^DonneesPersonnelles\/export$/, () => (reussi ? { body: 'archive-zip-factice' } : { status: 500 }));
}

/** Suppression du compte (`DonneesPersonnellesController`) : réussie seulement avec le bon mot de passe. */
export function simulerSuppressionCompte(serveur: FauxServeur, motDePasseAttendu = 'MotDePasse1!') {
  serveur.on('POST', /^DonneesPersonnelles\/suppression-compte$/, ({ corps }) => (corps?.password === motDePasseAttendu
    ? { status: 204 }
    : { status: 400, body: { message: 'Le mot de passe est incorrect.' } }));
}
