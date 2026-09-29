import type { Locator, Page } from '@playwright/test';

/** Page « Traitements » (/medicaments) : prises du jour, au besoin, soins, mes traitements, saisie dans le panneau du bas. */
export class TraitementsPage {
  readonly page: Page;

  readonly panneau: Locator;

  readonly aujourdhui: Locator;

  readonly auBesoin: Locator;

  readonly soins: Locator;

  readonly mesTraitements: Locator;

  constructor(page: Page) {
    this.page = page;
    this.panneau = page.getByRole('dialog');
    this.aujourdhui = page.getByRole('region', { name: 'Aujourd\'hui' });
    this.auBesoin = page.getByRole('region', { name: 'Au besoin' });
    this.soins = page.getByRole('region', { name: 'Soins' });
    this.mesTraitements = page.getByRole('region', { name: 'Mes traitements' });
  }

  async ouvrir(requete = '') {
    await this.page.goto(`/medicaments${requete}`);
  }

  /** Bouton flottant (mobile), de l'en-tête (desktop) ou de l'état vide : tous ouvrent la même saisie. */
  async ouvrirAjout() {
    await this.page.getByRole('button', { name: 'Ajouter un traitement' }).and(this.page.locator(':visible')).first().click();
  }

  async enregistrer() {
    await this.panneau.getByRole('button', { name: 'Enregistrer' }).click();
  }
}
