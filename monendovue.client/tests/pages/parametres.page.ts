import type { Locator, Page } from '@playwright/test';

/** Page « Paramètres » (/parametres) : section « Mes données ». */
export class ParametresPage {
  readonly page: Page;

  readonly boutonExport: Locator;

  readonly erreur: Locator;

  constructor(page: Page) {
    this.page = page;
    this.boutonExport = page.getByRole('button', { name: 'Télécharger toutes mes données' });
    this.erreur = page.getByRole('alert');
  }

  async ouvrir() {
    await this.page.goto('/parametres');
  }
}
