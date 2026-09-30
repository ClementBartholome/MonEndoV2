import { expect, test } from './fixtures';
import { simulerTransit } from './mocks/transit';

// Aujourd'hui (MAINTENANT) : mardi 15 septembre 2026.
test.describe('Transit (ancien suivi)', () => {
  test('affiche le mois par jour, avec le détail de chaque entrée, et renvoie vers le bilan', async ({ page, serveur }) => {
    simulerTransit(serveur, [
      { typeEvenement: 'Ballonnements', date: '2026-09-10T08:40:00', intensite: 'Modérée', douleur: true },
      { typeEvenement: 'Diarrhée', date: '2026-09-10T19:05:00', intensite: 'Légère', saignement: true, commentaires: 'Après le repas' },
    ]);
    await page.goto('/transit');

    await expect(page.getByRole('heading', { level: 1, name: 'Transit' })).toBeVisible();
    await expect(page.getByRole('heading', { level: 2, name: 'Jeudi 10 septembre' })).toBeVisible();
    await expect(page.getByText('8 h 40 · modérée · douleur', { exact: true })).toBeVisible();
    await expect(page.getByText('19 h 05 · légère · saignement · Après le repas', { exact: true })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Aller au bilan' })).toBeVisible();
    // Ancien suivi : plus aucune saisie.
    await expect(page.getByRole('button', { name: /Ajouter/ })).toHaveCount(0);
  });

  test('supprime une entrée en deux temps', async ({ page, serveur }) => {
    const entrees = simulerTransit(serveur, [{ typeEvenement: 'Ballonnements' }]);
    await page.goto('/transit');

    await page.getByRole('button', { name: /^Supprimer : Ballonnements/ }).click();
    await page.getByRole('button', { name: 'Confirmer' }).click();

    await expect(page.getByText('Aucune entrée ce mois-ci.')).toBeVisible();
    expect(entrees).toHaveLength(0);
  });

  test('change de mois ; un mois sans entrée le dit', async ({ page, serveur }) => {
    simulerTransit(serveur, [{ date: '2026-09-10T08:40:00' }]);
    await page.goto('/transit');

    await page.getByRole('button', { name: 'Mois précédent' }).click();

    await expect(page.getByText('Aucune entrée ce mois-ci.')).toBeVisible();
    expect(serveur.appelsVers('GET', /^DonneesTransit\/1\/8\/2026$/)).toHaveLength(1);
  });
});
