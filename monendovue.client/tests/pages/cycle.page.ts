import type { Locator, Page } from '@playwright/test';

/** Page « Cycle » (/cycle) : onglets Règles, Symptômes et Acné ; saisie d'un symptôme dans le panneau du bas. */
export class CyclePage {
  readonly page: Page;

  readonly panneau: Locator;

  readonly cycleEnCours: Locator;

  readonly calendrier: Locator;

  readonly mesCycles: Locator;

  /** Flux et caillots du jour choisi (titre = le jour, « Mardi 15 septembre »). */
  readonly detailDuJour: Locator;

  constructor(page: Page) {
    this.page = page;
    this.panneau = page.getByRole('dialog');
    this.cycleEnCours = page.getByRole('region', { name: /^(Règles|Jour|Pas de règles)/ });
    this.calendrier = page.getByRole('region', { name: /\d{4}$/ });
    this.mesCycles = page.getByRole('region', { name: 'Mes cycles' });
    this.detailDuJour = page.getByRole('region', { name: /^(Lundi|Mardi|Mercredi|Jeudi|Vendredi|Samedi|Dimanche) \d/ });
  }

  async ouvrir(requete = '') {
    await this.page.goto(`/cycle${requete}`);
  }

  onglet(nom: 'Règles' | 'Symptômes' | 'Acné') {
    return this.page.getByRole('tab', { name: nom });
  }

  /** Un jour du calendrier, par son libellé (« Jeudi 10 septembre »). */
  jour(libelle: string) {
    return this.calendrier.getByRole('button', { name: new RegExp(`^${libelle}`) });
  }

  entree(libelle: string) {
    return this.page.getByRole('button', { name: new RegExp(libelle) });
  }

  /** Bouton flottant (mobile), de l'en-tête (desktop) ou de l'état vide : tous ouvrent la même saisie. */
  async ouvrirAjout() {
    await this.page.getByRole('button', { name: 'Noter un symptôme' }).and(this.page.locator(':visible')).first().click();
  }
}
