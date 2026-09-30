import type { Locator, Page } from '@playwright/test';

/** Consentement aux données de santé : case de l'inscription (/register) et page d'accord (/consentement). */
export class ConsentementPage {
  readonly page: Page;

  readonly titre: Locator;

  readonly caseAccord: Locator;

  readonly boutonAccord: Locator;

  readonly boutonInscription: Locator;

  readonly navigation: Locator;

  readonly boutonDeconnexion: Locator;

  constructor(page: Page) {
    this.page = page;
    this.titre = page.getByRole('heading', { name: 'Ton accord pour tes données de santé' });
    this.caseAccord = page.getByRole('checkbox', { name: /J'accepte que MonEndo enregistre/ });
    this.boutonAccord = page.getByRole('button', { name: "J'accepte et je continue" });
    this.boutonInscription = page.getByRole('button', { name: 'Créer un compte' });
    this.navigation = page.getByRole('link', { name: 'Douleurs' });
    this.boutonDeconnexion = page.getByRole('button', { name: 'Me déconnecter' });
  }

  async ouvrirInscription() {
    await this.page.goto('/register');
  }

  /** L'email et le mot de passe n'ont pas de libellé associé (champ de formulaire shadcn) : ciblés par leur nom. */
  async remplirIdentifiants(email: string, motDePasse: string) {
    await this.page.locator('input[name="email"]').fill(email);
    await this.page.locator('input[name="password"]').fill(motDePasse);
  }
}
