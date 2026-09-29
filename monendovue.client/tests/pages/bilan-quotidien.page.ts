import type { Locator, Page } from '@playwright/test';
import { format } from 'date-fns';
import { fr } from 'date-fns/locale';

type Onglet = 'Historique' | 'Tendances';

/**
 * Page « Bilan quotidien » (/bilan-quotidien) : saisie d'un bilan, historique (calendrier, détail du jour) et analyse.
 * Les éléments sont trouvés par leur rôle et leur nom accessible, comme une lectrice d'écran les annoncerait.
 */
export class BilanQuotidienPage {
  // --- Saisie ---
  readonly titreSaisie: Locator;
  readonly emotions: Locator;
  readonly boutonEnregistrer: Locator;
  readonly boutonCommeHier: Locator;
  readonly boutonAnnuler: Locator;
  readonly confirmationSortie: Locator;
  readonly aideSaisie: Locator;

  // --- Historique ---
  readonly calendrier: Locator;
  readonly boutonModifier: Locator;
  readonly boutonRemplir: Locator;

  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
    this.titreSaisie = page.getByRole('heading', { level: 2, name: /^(Bilan|Modifier le bilan)/ });
    this.emotions = page.getByRole('group', { name: 'Émotions du jour' });
    this.boutonEnregistrer = page.getByRole('button', { name: 'Enregistrer', exact: true });
    this.boutonCommeHier = page.getByRole('button', { name: 'Comme hier' });
    this.boutonAnnuler = page.getByRole('button', { name: 'Annuler', exact: true });
    this.confirmationSortie = page.getByRole('dialog', { name: 'Quitter sans enregistrer ?' });
    this.aideSaisie = page.getByText(/^À renseigner/);

    this.calendrier = page.getByRole('group', { name: 'Bilans de la période' });
    this.boutonModifier = page.getByRole('button', { name: 'Modifier', exact: true });
    this.boutonRemplir = page.getByRole('button', { name: 'Remplir le bilan de ce jour' });
  }

  async ouvrir() {
    await this.page.goto('/bilan-quotidien');
  }

  // --- Saisie ---

  /** Pastille d'une échelle : « Douleur » sur 10, « Fatigue », « Stress · vie pro », « Stress · vie perso » sur 5. */
  pastille(echelle: string, niveau: number) {
    const max = echelle === 'Douleur' ? 10 : 5;
    return this.page.getByRole('button', { name: `${echelle} ${niveau} sur ${max}`, exact: true });
  }

  async choisirNiveau(echelle: string, niveau: number) {
    await this.pastille(echelle, niveau).click();
  }

  emotion(libelle: string) {
    return this.emotions.getByRole('button', { name: libelle, exact: true });
  }

  async choisirEmotions(...libelles: string[]) {
    for (const libelle of libelles) {
      await this.emotion(libelle).click();
    }
  }

  /** Saisie minimale : les deux réponses obligatoires. */
  async remplirEssentiel(douleur: number, ...emotions: string[]) {
    await this.choisirNiveau('Douleur', douleur);
    await this.choisirEmotions(...emotions);
  }

  async enregistrer() {
    await this.boutonEnregistrer.click();
  }

  // --- Historique ---

  onglet(nom: Onglet) {
    return this.page.getByRole('tab', { name: nom });
  }

  async afficherOnglet(nom: Onglet) {
    await this.onglet(nom).click();
  }

  modePeriode(mode: 'Semaine' | 'Mois') {
    return this.page.getByRole('group', { name: 'Période affichée' }).getByRole('button', { name: mode, exact: true });
  }

  async periodePrecedente() {
    await this.page.getByRole('button', { name: /^(Mois précédent|Semaine précédente)$/ }).click();
  }

  /** Titre de la période affichée (« Septembre 2026 », « 14 - 20 septembre 2026 »). */
  titrePeriode(libelle: string | RegExp) {
    return this.page.getByRole('heading', { level: 2, name: libelle });
  }

  /**
   * Case d'un jour dans le calendrier. Son nom accessible décrit le jour : « mardi 15 septembre, douleur 3 sur 10, règles ».
   */
  jour(date: Date) {
    const libelle = format(date, 'EEEE d MMMM', { locale: fr });
    return this.calendrier.getByRole('button', { name: new RegExp(`^${libelle}(,|$)`) });
  }

  async selectionnerJour(date: Date) {
    await this.jour(date).click();
  }

  /** Titre de la carte du jour sélectionné (« Aujourd'hui », « lundi 14 septembre »). */
  titreDuJour(titre: string) {
    return this.page.getByRole('heading', { name: titre, exact: true });
  }

  // --- Tendances ---

  /** Tuile d'un indicateur (« Douleur moyenne », « Fatigue moyenne »…), avec sa moyenne et son évolution. */
  indicateur(libelle: string) {
    return this.page.getByRole('listitem').filter({ hasText: libelle });
  }
}
