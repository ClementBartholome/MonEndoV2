import type { Locator, Page } from '@playwright/test';

/** Sous cette largeur, la page affiche des cartes ; au-delà, un tableau (point de rupture `md` de Tailwind). */
const LARGEUR_DESKTOP = 768;

/** Page « Suivi de l'activité » (/activite) : liste des séances du mois, ajout, modification, suppression. */
export class ActivitePage {
  readonly formulaire: Locator;

  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
    this.formulaire = page.getByRole('dialog');
  }

  private get estMobile() {
    return (this.page.viewportSize()?.width ?? 0) < LARGEUR_DESKTOP;
  }

  async ouvrir() {
    await this.page.goto('/activite');
  }

  /** Texte visible : la page contient à la fois les cartes (mobile) et le tableau (desktop), l'un des deux est masqué. */
  seance(titre: string) {
    return this.page.getByText(titre, { exact: true }).and(this.page.locator(':visible'));
  }

  async ouvrirAjout() {
    // Sur mobile, le bouton n'affiche que son icône : on le trouve par sa place dans l'en-tête de la page.
    await this.page.locator('.form-modal button').first().click();
  }

  async remplir(champs: { type?: string; date?: string; heure?: string; duree?: number }) {
    if (champs.type !== undefined) await this.formulaire.getByPlaceholder('Course à pied').fill(champs.type);
    if (champs.date !== undefined) await this.formulaire.locator('input[type="date"]').fill(champs.date);
    if (champs.heure !== undefined) await this.formulaire.locator('input[type="time"]').fill(champs.heure);
    if (champs.duree !== undefined) await this.formulaire.getByPlaceholder('Durée en minutes').fill(String(champs.duree));
  }

  async valider(bouton: 'Enregistrer' | 'Mettre à jour') {
    await this.formulaire.getByRole('button', { name: bouton }).click();
  }

  /** Bouton « Modifier » ou « Supprimer » d'une séance : carte sur mobile, ligne du tableau sur desktop. */
  action(titre: string, action: 'Modifier' | 'Supprimer') {
    if (this.estMobile) {
      return this.page.locator('div', { has: this.seance(titre) }).getByRole('button', { name: action }).last();
    }
    return this.page.getByRole('row', { name: new RegExp(titre) })
      .locator(action === 'Modifier' ? '.edit-btn' : '.delete-btn');
  }
}
