import type { Locator, Page } from '@playwright/test';

/** Page « Paramètres » (/parametres) : section « Mes données » (export, suppression du compte). */
export class ParametresPage {
  readonly page: Page;

  readonly boutonExport: Locator;

  readonly erreur: Locator;

  readonly boutonSuppression: Locator;

  readonly fenetreSuppression: Locator;

  readonly champMotDePasse: Locator;

  readonly boutonConfirmerSuppression: Locator;

  constructor(page: Page) {
    this.page = page;
    this.boutonExport = page.getByRole('button', { name: 'Télécharger toutes mes données' });
    this.erreur = page.getByRole('alert');
    this.boutonSuppression = page.getByRole('button', { name: 'Supprimer mon compte' });
    this.fenetreSuppression = page.getByRole('dialog', { name: 'Supprimer ton compte ?' });
    this.champMotDePasse = this.fenetreSuppression.getByLabel('Ton mot de passe, pour confirmer');
    this.boutonConfirmerSuppression = this.fenetreSuppression.getByRole('button', { name: 'Supprimer définitivement' });
  }

  async ouvrir() {
    await this.page.goto('/parametres');
  }
}
