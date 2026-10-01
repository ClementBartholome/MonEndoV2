import type { Locator, Page } from '@playwright/test';

/** Sous cette largeur, barre du bas et menu « Plus » ; au-delà, barre latérale (point de rupture `lg` de Tailwind). */
const LARGEUR_BARRE_LATERALE = 1024;

/** Libellé commençant par `nom` (le détail de la rubrique suit son titre dans le lien) ; les caractères spéciaux (« ? ») sont échappés. */
const debutDe = (nom: string) => new RegExp(`^${nom.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}`);

/** Navigation principale : barre du bas et menu « Plus » (mobile), barre latérale (desktop). */
export class NavigationPage {
  readonly page: Page;

  readonly menuPlus: Locator;

  constructor(page: Page) {
    this.page = page;
    this.menuPlus = page.getByRole('dialog', { name: 'Plus' });
  }

  get estMobile() {
    return (this.page.viewportSize()?.width ?? 0) < LARGEUR_BARRE_LATERALE;
  }

  /** La navigation visible à cette largeur (les deux sont dans la page, l'une est masquée). */
  get principale() {
    return this.page.getByRole('navigation', { name: 'Navigation principale' }).and(this.page.locator(':visible'));
  }

  lien(nom: string) {
    return this.principale.getByRole('link', { name: nom, exact: true });
  }

  /** Ouvre une rubrique secondaire : par le menu « Plus » sur mobile, directement dans la barre latérale sinon. */
  async ouvrirRubrique(nom: string) {
    if (this.estMobile) {
      await this.principale.getByRole('button', { name: 'Plus' }).click();
      await this.menuPlus.getByRole('link', { name: debutDe(nom) }).click();
    } else {
      await this.lien(nom).click();
    }
  }
}
