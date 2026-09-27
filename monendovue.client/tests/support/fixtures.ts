import { test as base, expect } from '@playwright/test';
import { FauxServeur } from './faux-serveur';

/** Date du jour vue par l'application pendant les tests : les parcours ne dépendent pas du jour où ils tournent. */
export const MAINTENANT = new Date(2026, 8, 15, 10, 0, 0);

export const CARNET_ID = 1;

const UTILISATRICE = {
  email: 'utilisatrice@test.local',
  carnetSanteId: CARNET_ID,
  // Loin dans le futur : l'application ne tente pas de rafraîchir le jeton.
  tokenExpiry: new Date(2099, 0, 1).toISOString(),
};

interface Options {
  /** Session ouverte au chargement de la page (faux utilisateur dans localStorage, comme après une connexion). */
  connectee: boolean;
}

/**
 * `test` des parcours E2E : chaque test a sa page, son faux serveur (`serveur`), une horloge fixée à MAINTENANT et,
 * par défaut, une session ouverte. `test.use({ connectee: false })` pour un parcours sans session (connexion).
 */
export const test = base.extend<Options & { serveur: FauxServeur }>({
  connectee: [true, { option: true }],

  serveur: [
    async ({ page, connectee }, use) => {
      const serveur = new FauxServeur();
      routesCommunes(serveur);
      await page.clock.setFixedTime(MAINTENANT);
      if (connectee) {
        await page.addInitScript((utilisatrice) => {
          localStorage.setItem('user', JSON.stringify(utilisatrice));
        }, UTILISATRICE);
      }
      await serveur.brancher(page);

      await use(serveur);

      expect(serveur.nonGerees, 'appels API sans réponse simulée (route à ajouter dans tests/support)').toEqual([]);
    },
    { auto: true },
  ],
});

export { expect };

/** Réponses des appels faits sur toutes les pages (accueil, session, agenda). */
function routesCommunes(serveur: FauxServeur) {
  serveur
    .on('GET', /^CarnetSante\/last-entries\/\d+$/, () => ({ body: { carnetSanteId: CARNET_ID } }))
    // Pas d'agenda associé au compte par défaut : pas de bloc « Prochains rendez-vous ».
    .on('GET', /^Agenda\//, () => ({ status: 404 }))
    .on('POST', /^Account\/logout$/, () => ({ status: 200 }));
}
