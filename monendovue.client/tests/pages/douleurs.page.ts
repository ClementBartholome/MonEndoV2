import type { Locator, Page } from '@playwright/test';

/** Page « Douleurs » (/douleurs) : mois affiché, chiffres, liste par jour, saisie dans le panneau du bas. */
export class DouleursPage {
  readonly page: Page;

  readonly panneau: Locator;

  readonly moisAffiche: Locator;

  readonly chiffres: Locator;

  constructor(page: Page) {
    this.page = page;
    this.panneau = page.getByRole('dialog');
    this.moisAffiche = page.getByRole('navigation', { name: 'Mois affiché' });
    this.chiffres = page.getByRole('region', { name: 'Ce mois-ci' });
  }

  async ouvrir(requete = '') {
    await this.page.goto(`/douleurs${requete}`);
  }

  /** Bouton flottant (mobile), de l'en-tête (desktop) ou de l'état vide : tous ouvrent la même saisie. */
  async ouvrirAjout() {
    await this.page.getByRole('button', { name: 'Noter une douleur' }).and(this.page.locator(':visible')).first().click();
  }

  entree(libelle: string) {
    return this.page.getByRole('button', { name: new RegExp(libelle) });
  }

  async remplir({ type, intensite, commentaire }: { type?: string; intensite?: number; commentaire?: string }) {
    if (type) await this.panneau.getByRole('button', { name: type, exact: true }).click();
    if (intensite) await this.panneau.getByRole('button', { name: `Intensité ${intensite} sur 10` }).click();
    if (commentaire !== undefined) await this.panneau.getByRole('textbox').fill(commentaire);
  }

  async enregistrer() {
    await this.panneau.getByRole('button', { name: 'Enregistrer' }).click();
  }
}
