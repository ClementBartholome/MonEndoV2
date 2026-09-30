import type { Locator, Page } from '@playwright/test';

/** Page « Préparer un rendez-vous » (/export) : période, rubriques à inclure, questions, création du PDF. */
export class RendezVousPage {
  readonly page: Page;

  readonly periode: Locator;
  readonly rubriques: Locator;
  readonly questions: Locator;
  readonly boutonCreer: Locator;

  constructor(page: Page) {
    this.page = page;
    this.periode = page.getByRole('group', { name: 'Période' });
    this.rubriques = page.getByRole('group', { name: 'À inclure' });
    this.questions = page.getByRole('textbox', { name: /^Mes questions/ });
    this.boutonCreer = page.getByRole('button', { name: 'Créer le PDF' });
  }

  async ouvrir() {
    await this.page.goto('/export');
  }

  choixPeriode(libelle: '1 mois' | '3 mois' | 'Autre') {
    return this.periode.getByRole('button', { name: libelle, exact: true });
  }

  rubrique(titre: string) {
    return this.rubriques.getByRole('checkbox', { name: new RegExp(`^${titre}`) });
  }

  /** Crée le PDF et renvoie le téléchargement déclenché. */
  async creer() {
    const telechargement = this.page.waitForEvent('download');
    await this.boutonCreer.click();
    return telechargement;
  }
}
