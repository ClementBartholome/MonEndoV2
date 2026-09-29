import type { Locator, Page } from '@playwright/test';

/** Page « Activité » (/activite) : mois affiché, chiffres, séances par jour, saisie dans le panneau du bas. */
export class ActivitePage {
  readonly page: Page;

  readonly panneau: Locator;

  readonly chiffres: Locator;

  constructor(page: Page) {
    this.page = page;
    this.panneau = page.getByRole('dialog');
    this.chiffres = page.getByRole('region', { name: 'Ce mois-ci' });
  }

  async ouvrir(requete = '') {
    await this.page.goto(`/activite${requete}`);
  }

  /** Bouton flottant (mobile), de l'en-tête (desktop) ou de l'état vide : tous ouvrent la même saisie. */
  async ouvrirAjout() {
    await this.page.getByRole('button', { name: 'Noter une activité' }).and(this.page.locator(':visible')).first().click();
  }

  seance(libelle: string) {
    return this.page.getByRole('button', { name: new RegExp(libelle) });
  }

  async choisir(...libelles: string[]) {
    for (const libelle of libelles) await this.panneau.getByRole('button', { name: libelle, exact: true }).click();
  }

  async enregistrer() {
    await this.panneau.getByRole('button', { name: 'Enregistrer' }).click();
  }
}
