import { expect, test } from '../fixtures';
import { simulerBilans, simulerDouleursDuJour } from '../mocks/bilan-quotidien';

// Aujourd'hui (MAINTENANT) : mardi 15 septembre 2026.
const AUJOURDHUI = '2026-09-15';

test.describe('Bilan quotidien — « Où as-tu eu mal ? »', () => {
  test('une zone touchée reprend la douleur du jour, s\'ajuste et rejoint les douleurs du jour', async ({ bilanQuotidienPage: bilanPage, page, serveur }) => {
    simulerBilans(serveur);
    const zones = simulerDouleursDuJour(serveur);
    await bilanPage.ouvrirSaisie();

    await bilanPage.remplirEssentiel(6, 'Calme');
    await page.getByRole('button', { name: 'Lombaire', exact: true }).click();
    const lignes = page.getByRole('list', { name: 'Douleurs du jour' });
    await expect(lignes).toContainText('6/10');
    await page.getByRole('button', { name: 'Plus fort : Lombaire' }).click();
    await expect(lignes).toContainText('7/10');
    // Touchée puis retirée : elle n'est pas envoyée.
    await page.getByRole('button', { name: 'Abdominale', exact: true }).click();
    await page.getByRole('button', { name: 'Abdominale', exact: true }).click();
    await bilanPage.enregistrer();

    await expect(page.getByText('Bilan enregistré', { exact: true })).toBeVisible();
    expect(zones.get(AUJOURDHUI)).toEqual([{ typeDouleur: 'Douleur lombaire', intensite: 7 }]);
  });

  test('une crise déjà notée ce jour-là est montrée, sans pouvoir la noter une seconde fois', async ({ bilanQuotidienPage: bilanPage, page, serveur }) => {
    simulerBilans(serveur);
    simulerDouleursDuJour(serveur, { crises: { [AUJOURDHUI]: [{ typeDouleur: 'Douleur pelvienne', intensite: 7, heure: '14:20' }] } });
    await bilanPage.ouvrirSaisie();

    const pelvienne = page.getByRole('button', { name: /^Pelvienne/ });
    await expect(pelvienne).toHaveAttribute('aria-pressed', 'true');
    await expect(pelvienne).toBeDisabled();
    await expect(pelvienne).toContainText('crise à 14 h 20');
    await expect(page.getByRole('list', { name: 'Douleurs du jour' })).toContainText('Crise notée à 14 h 20');
  });

  test('sans zone touchée, rien n\'est envoyé en plus du bilan', async ({ bilanQuotidienPage: bilanPage, page, serveur }) => {
    simulerBilans(serveur);
    simulerDouleursDuJour(serveur);
    await bilanPage.ouvrirSaisie();

    await bilanPage.remplirEssentiel(3, 'Calme');
    await bilanPage.enregistrer();

    await expect(page.getByText('Bilan enregistré', { exact: true })).toBeVisible();
    expect(serveur.appelsVers('PUT', /^DouleursDuJour\//)).toHaveLength(0);
  });

  test('en modification, les zones déjà notées sont reprises et peuvent être retirées', async ({ bilanQuotidienPage: bilanPage, page, serveur }) => {
    simulerBilans(serveur, { bilans: [{ jour: AUJOURDHUI, douleurMoyenne: 5 }] });
    const zones = simulerDouleursDuJour(serveur, { zones: { [AUJOURDHUI]: [{ typeDouleur: 'Douleur lombaire', intensite: 5 }] } });
    await bilanPage.ouvrir();
    await bilanPage.boutonModifier.click();

    const lombaire = page.getByRole('button', { name: 'Lombaire', exact: true });
    await expect(lombaire).toHaveAttribute('aria-pressed', 'true');
    await lombaire.click();
    await bilanPage.enregistrer();

    await expect(page.getByText('Bilan mis à jour', { exact: true })).toBeVisible();
    expect(zones.get(AUJOURDHUI)).toEqual([]);
  });
});
