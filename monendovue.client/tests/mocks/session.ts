/** Session et horloge communes à tous les tests E2E. */

/** Date du jour vue par l'application pendant les tests (mardi 15 septembre 2026, 10 h) : les parcours ne dépendent pas du jour où ils tournent. */
// 10 h à Paris (heure d'été, +02:00) quel que soit le fuseau de la machine : le navigateur de test est réglé sur
// Europe/Paris (playwright.config.ts), la CI tourne en UTC.
export const MAINTENANT = new Date('2026-09-15T10:00:00+02:00');

export const CARNET_ID = 1;

/** Utilisatrice connectée, telle que l'application la garde dans localStorage après une connexion. */
export const UTILISATRICE = {
  email: 'utilisatrice@test.local',
  carnetSanteId: CARNET_ID,
  // Loin dans le futur : l'application ne tente pas de rafraîchir le jeton.
  tokenExpiry: new Date(2099, 0, 1).toISOString(),
};
