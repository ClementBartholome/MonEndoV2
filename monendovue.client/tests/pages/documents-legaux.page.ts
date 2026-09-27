import type { Locator, Page } from '@playwright/test';

/** Documents légaux publics (/confidentialite, /mentions-legales) et leurs liens depuis les autres écrans. */
export class DocumentsLegauxPage {
  readonly page: Page;

  readonly titre: Locator;

  constructor(page: Page) {
    this.page = page;
    this.titre = page.getByRole('heading', { level: 1 });
  }

  async ouvrirConnexion() {
    await this.page.goto('/login');
  }

  async ouvrirParametres() {
    await this.page.goto('/parametres');
  }

  /** Lien du pied des écrans de connexion, d'inscription et des paramètres. */
  lienPied(nom: 'Confidentialité' | 'Mentions légales') {
    return this.page.getByRole('navigation', { name: 'Informations légales' }).getByRole('link', { name: nom });
  }

  lienRetour() {
    return this.page.getByRole('link', { name: /^Retour à/ });
  }

  section(titre: string) {
    return this.page.getByRole('heading', { level: 2, name: titre });
  }
}
