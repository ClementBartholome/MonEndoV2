import type { Locator, Page } from '@playwright/test';
import { expect, test } from '../fixtures';
import { simulerBilans } from '../mocks/bilan-quotidien';

// Aujourd'hui (MAINTENANT) : mardi 15 septembre 2026.
const bloc = (page: Page, titre: string): Locator => page.getByRole('button', { name: new RegExp(`^${titre}`) });
const serie = (page: Page, question: string): Locator => page.getByRole('group', { name: question, exact: true });
const choix = (groupe: Locator, libelle: string): Locator => groupe.getByRole('button', { name: libelle, exact: true });

test.describe('Bilan quotidien — catégories facultatives', () => {
  test('enregistre les réponses données ; le reste reste non renseigné (null)', async ({ bilanQuotidienPage: bilanPage, page, serveur }) => {
    const bilans = simulerBilans(serveur);
    await bilanPage.ouvrirSaisie();
    await bilanPage.remplirEssentiel(4, 'Calme');

    await bloc(page, 'Urinaire').click();
    await choix(serie(page, 'Une douleur en urinant ?'), 'Oui').click();
    await choix(serie(page, 'Intensité'), 'Modérée').click();
    await bloc(page, 'Nuit et journée').click();
    await choix(serie(page, 'Comment était ta nuit ?'), 'Moyenne').click();
    await choix(serie(page, 'Tes symptômes ont-ils limité ta journée ?'), 'Un peu limitée').click();
    await bilanPage.enregistrer();

    await expect(page.getByText('Bilan enregistré', { exact: true })).toBeVisible();
    expect(bilans).toHaveLength(1);
    expect(bilans[0]).toMatchObject({
      douleurUriner: true, intensiteDouleurUriner: 'Modérée', nuit: 'Moyenne', limitationJournee: 'PeuLimitee',
      // Jamais « non » ni zéro par défaut : ce qui n'a pas été répondu est null.
      sangUrines: null, enviesUrinaires: null, saignementsHorsRegles: null, reveilsDouleur: null, absenceTravail: null,
      douleurRapport: null, douleurSelle: null, nausees: null,
    });
  });

  test('une réponse « Oui » exige son intensité ; « Non » ou re-toucher la retire', async ({ bilanQuotidienPage: bilanPage, page, serveur }) => {
    simulerBilans(serveur);
    await bilanPage.ouvrirSaisie();
    await bilanPage.remplirEssentiel(2, 'Calme');
    await bloc(page, 'Urinaire').click();
    const douleur = serie(page, 'Une douleur en urinant ?');

    await choix(douleur, 'Oui').click();
    await expect(bilanPage.boutonEnregistrer).toBeDisabled();
    await expect(page.getByText('Douleur en urinant : choisis une intensité')).toBeVisible();

    await choix(serie(page, 'Intensité'), 'Forte').click();
    await expect(bilanPage.boutonEnregistrer).toBeEnabled();

    // Passer à « Non » vide l'intensité : plus rien ne bloque.
    await choix(douleur, 'Non').click();
    await expect(page.getByRole('group', { name: 'Intensité' })).toHaveCount(0);
    await expect(bilanPage.boutonEnregistrer).toBeEnabled();

    // Re-toucher la réponse choisie la retire (non renseigné).
    await choix(douleur, 'Non').click();
    await expect(choix(douleur, 'Non')).toHaveAttribute('aria-pressed', 'false');
  });

  test('le résumé d\'un bloc replié ne dit rien du contenu des réponses', async ({ bilanQuotidienPage: bilanPage, page, serveur }) => {
    simulerBilans(serveur);
    await bilanPage.ouvrirSaisie();
    await expect(bloc(page, 'Urinaire')).toContainText('Rien de noté');

    await bloc(page, 'Urinaire').click();
    await choix(serie(page, 'Du sang visible dans les urines ?'), 'Non').click();
    await choix(serie(page, 'Du mal à vider complètement la vessie ?'), 'Oui').click();
    await bloc(page, 'Urinaire').click();

    await expect(bloc(page, 'Urinaire')).toContainText('2 réponses notées');
    await expect(bloc(page, 'Urinaire')).not.toContainText('sang');
  });

  test('la suite du transit se saisit dans le bloc Corps', async ({ bilanQuotidienPage: bilanPage, page, serveur }) => {
    const bilans = simulerBilans(serveur);
    await bilanPage.ouvrirSaisie();
    await bilanPage.remplirEssentiel(3, 'Calme');

    await bloc(page, 'Corps').click();
    await choix(serie(page, 'Des nausées ?'), 'Oui').click();
    await choix(serie(page, 'Une douleur en allant à la selle ?'), 'Oui').click();
    await expect(bilanPage.boutonEnregistrer).toBeDisabled();
    await expect(page.getByText('Transit : choisis une intensité')).toBeVisible();
    await choix(serie(page, 'Intensité'), 'Légère').click();
    await bilanPage.enregistrer();

    await expect(page.getByText('Bilan enregistré', { exact: true })).toBeVisible();
    expect(bilans[0]).toMatchObject({ nausees: true, douleurSelle: true, intensiteDouleurSelle: 'Légère', sangSelles: null });
  });

  test('« Rapports » est discrète : absente sans activation dans Paramètres', async ({ bilanQuotidienPage: bilanPage, page, serveur }) => {
    simulerBilans(serveur);
    await bilanPage.ouvrirSaisie();

    await expect(bilanPage.titreSaisie).toBeVisible();
    await expect(bloc(page, 'Urinaire')).toBeVisible();
    await expect(bloc(page, 'Rapports')).toHaveCount(0);
  });

  test('« Rapports » apparaît quand elle est activée dans Paramètres', async ({ bilanQuotidienPage: bilanPage, page, serveur }) => {
    simulerBilans(serveur);
    await page.addInitScript(() => localStorage.setItem('monendo-categories-bilan', JSON.stringify({ rapports: true })));
    await bilanPage.ouvrirSaisie();

    await expect(bloc(page, 'Rapports')).toBeVisible();
    await bloc(page, 'Rapports').click();
    await expect(page.getByText('Catégorie discrète')).toBeVisible();
    await choix(serie(page, 'Une douleur pendant ou après un rapport ?'), 'Pas de rapport').click();
    await expect(choix(serie(page, 'Une douleur pendant ou après un rapport ?'), 'Pas de rapport')).toHaveAttribute('aria-pressed', 'true');
  });

  test('en modification, les blocs déjà renseignés s\'ouvrent avec leurs réponses', async ({ bilanQuotidienPage: bilanPage, page, serveur }) => {
    const bilans = simulerBilans(serveur, {
      bilans: [{ jour: '2026-09-15', douleurMoyenne: 3, saignementsHorsRegles: true, abondanceSaignementsHorsRegles: 'Traces', nuit: 'Difficile' }],
    });
    await bilanPage.ouvrir();

    await bilanPage.boutonModifier.click();

    await expect(bloc(page, 'Saignements hors règles')).toHaveAttribute('aria-expanded', 'true');
    await expect(choix(serie(page, 'Des saignements en dehors de tes règles ?'), 'Oui')).toHaveAttribute('aria-pressed', 'true');
    await expect(choix(serie(page, 'Quelle abondance ?'), 'Traces')).toHaveAttribute('aria-pressed', 'true');
    await expect(bloc(page, 'Urinaire')).toHaveAttribute('aria-expanded', 'false');
    // Retirer la réponse efface aussi l'abondance, puis le serveur reçoit « non renseigné ».
    await choix(serie(page, 'Des saignements en dehors de tes règles ?'), 'Oui').click();
    await bilanPage.enregistrer();
    await expect(page.getByText('Bilan mis à jour', { exact: true })).toBeVisible();
    expect(bilans[0]).toMatchObject({ saignementsHorsRegles: null, abondanceSaignementsHorsRegles: null, nuit: 'Difficile' });
  });

  test('le détail d\'un jour résume les réponses données, sans « Rapports » quand la catégorie est désactivée', async ({ bilanQuotidienPage: bilanPage, page, serveur }) => {
    simulerBilans(serveur, {
      bilans: [{
        jour: '2026-09-15', douleurMoyenne: 3, douleurUriner: true, intensiteDouleurUriner: 'Modérée', sangUrines: false,
        limitationJournee: 'TresLimitee', absenceTravail: true, douleurRapport: 'Oui', nausees: true,
      }],
    });
    await bilanPage.ouvrir();

    const detail = page.getByRole('heading', { name: "Aujourd'hui", exact: true }).locator('xpath=ancestor::*[contains(@class,"rounded-carte")][1]');
    await expect(detail).toContainText(/Douleur en urinant.{0,4}Oui · modérée/);
    await expect(detail).toContainText(/Sang visible dans les urines.{0,4}Non/);
    await expect(detail).toContainText(/Journée.{0,4}Très limitée/);
    await expect(detail).toContainText(/Absence au travail ou en cours.{0,4}Oui/);
    await expect(detail).toContainText(/Nausées.{0,4}Oui/);
    await expect(detail).not.toContainText(/rapport/i);
  });
});
