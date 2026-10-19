import type { Locator, Page } from '@playwright/test';

/** Accueil « Aujourd'hui » (/) : bilan du jour, ajouts rapides, traitements, rendez-vous, semaine. */
export class AccueilPage {
  readonly page: Page;

  readonly bilan: Locator;

  readonly traitements: Locator;

  readonly semaine: Locator;

  readonly acne: Locator;

  constructor(page: Page) {
    this.page = page;
    this.bilan = page.getByRole('region', { name: /^Bilan du jour/ });
    this.traitements = page.getByRole('region', { name: 'Traitements du jour' });
    this.semaine = page.getByRole('region', { name: 'Ta semaine' });
    this.acne = page.getByRole('region', { name: /^Depuis le/ });
  }

  async ouvrir() {
    await this.page.goto('/');
  }

  pastilleCycle(texte: string | RegExp) {
    return this.page.getByText(texte);
  }

  boutonPrise(nom: string, heure: string) {
    return this.traitements.getByRole('button', { name: `Je l'ai pris : ${nom}, ${heure}` });
  }
}
