import { test as base, expect } from '@playwright/test';
import { FauxServeur } from './mocks/faux-serveur';
import { MAINTENANT, UTILISATRICE, CARNET_ID } from './mocks/session';
import { ActivitePage } from './pages/activite.page';
import { BilanQuotidienPage } from './pages/bilan-quotidien.page';
import { DocumentsLegauxPage } from './pages/documents-legaux.page';
import { ConsentementPage } from './pages/consentement.page';
import { ParametresPage } from './pages/parametres.page';
import { NavigationPage } from './pages/navigation.page';
import { AccueilPage } from './pages/accueil.page';
import { DouleursPage } from './pages/douleurs.page';
import { ACCUEIL_VIDE } from './mocks/accueil';

interface Options {
  /** Session ouverte au chargement de la page (faux utilisateur dans localStorage, comme après une connexion). */
  connectee: boolean;
}

interface Fixtures {
  /** Faux backend du test : les données y sont préparées (`simulerBilans(serveur, …)`) puis vérifiées. */
  serveur: FauxServeur;
  activitePage: ActivitePage;
  bilanQuotidienPage: BilanQuotidienPage;
  documentsLegauxPage: DocumentsLegauxPage;
  consentementPage: ConsentementPage;
  parametresPage: ParametresPage;
  navigationPage: NavigationPage;
  accueilPage: AccueilPage;
  douleursPage: DouleursPage;
}

/**
 * `test` des parcours E2E : chaque test a sa page, son faux serveur, une horloge fixée à MAINTENANT et, par défaut, une
 * session ouverte. Les objets de page (`bilanQuotidienPage`…) sont fournis comme fixtures.
 * `test.use({ connectee: false })` pour un parcours sans session (connexion).
 */
export const test = base.extend<Options & Fixtures>({
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

      expect(serveur.nonGerees, 'appels API sans réponse simulée (route à ajouter dans tests/mocks)').toEqual([]);
    },
    { auto: true },
  ],

  activitePage: async ({ page }, use) => {
    await use(new ActivitePage(page));
  },

  bilanQuotidienPage: async ({ page }, use) => {
    await use(new BilanQuotidienPage(page));
  },

  documentsLegauxPage: async ({ page }, use) => {
    await use(new DocumentsLegauxPage(page));
  },

  consentementPage: async ({ page }, use) => {
    await use(new ConsentementPage(page));
  },

  parametresPage: async ({ page }, use) => {
    await use(new ParametresPage(page));
  },

  navigationPage: async ({ page }, use) => {
    await use(new NavigationPage(page));
  },

  accueilPage: async ({ page }, use) => {
    await use(new AccueilPage(page));
  },

  douleursPage: async ({ page }, use) => {
    await use(new DouleursPage(page));
  },
});

export { expect };
export { MAINTENANT, CARNET_ID };

/** Réponses des appels faits sur toutes les pages (accueil, session, agenda). */
function routesCommunes(serveur: FauxServeur) {
  serveur
    // Accueil vide par défaut (tests/mocks/accueil.ts pour un accueil rempli).
    .on('GET', /^Accueil\/aujourdhui$/, () => ({ body: ACCUEIL_VIDE }))
    // Pas d'agenda associé au compte par défaut : pas de bloc « Prochains rendez-vous ».
    .on('GET', /^Agenda\//, () => ({ status: 404 }))
    .on('POST', /^Account\/logout$/, () => ({ status: 200 }));
}
