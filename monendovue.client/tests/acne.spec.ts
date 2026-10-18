import { test, expect } from './fixtures';
import { simulerAcne, simulerSymptomes } from './mocks/cycle';

const photo = (id: number, date: string) => ({
  id, date, intensite: 4, commentaire: null,
  // Chemin de l'API, comme le serveur : la photo est servie par Acne/photos/{id}, jamais par le stockage.
  photoUrl: `Acne/photos/${id}`,
});

test.describe('Acné', () => {
  test('épisode en cours, point de la semaine, avant / après et historique', async ({ serveur, page }) => {
    simulerAcne(serveur, [{ debut: '2026-06-03', fin: '2026-07-20' }, { debut: '2026-08-02' }],
      [photo(3, '2026-09-10T20:00:00'), photo(2, '2026-06-12T20:00:00'), photo(1, '2026-05-01T20:00:00')]);
    await page.goto('/acne');

    await expect(page.getByRole('heading', { level: 1 })).toHaveText('Acné');
    const episode = page.getByRole('region', { name: 'En ce moment' });
    await expect(episode).toContainText('Depuis le 2 août · 45 jours');
    await expect(page.getByRole('region', { name: 'Point de la semaine' })).toContainText('Dernière photo il y a 5 jours');
    // Trois mois avant le 10 septembre : la photo du 12 juin, la plus proche.
    await expect(page.getByRole('img', { name: 'Photo du 12 juin 2026' })).toBeVisible();
    await page.getByRole('radio', { name: '6 mois' }).click();
    await expect(page.getByRole('img', { name: 'Photo du 1 mai 2026' })).toBeVisible();
    await expect(page.getByRole('region', { name: 'Épisodes' })).toContainText('3 juin → 20 juillet');
  });

  test('« Ça s\'est calmé » termine l\'épisode, « L\'acné revient » en ouvre un autre', async ({ serveur, page }) => {
    const episodes = simulerAcne(serveur, [{ debut: '2026-08-02' }]);
    await page.goto('/acne');
    const panneau = page.getByRole('dialog');

    await page.getByRole('button', { name: 'Ça s\'est calmé' }).click();
    await expect(panneau.getByLabel('Dernier jour')).toHaveValue('2026-09-15');
    await panneau.getByRole('button', { name: 'Enregistrer' }).click();
    await expect(page.getByRole('region', { name: 'Pas en ce moment' })).toContainText('Dernier épisode terminé le 15 septembre');
    expect(episodes[0].fin).toBe('2026-09-15');

    await page.getByRole('button', { name: 'L\'acné revient' }).click();
    await panneau.getByRole('button', { name: 'Enregistrer' }).click();
    await expect(page.getByRole('region', { name: 'En ce moment' })).toBeVisible();
    expect(serveur.appelsVers('POST', /^Acne\/episodes$/)[0].corps).toEqual({ debut: '2026-09-15', fin: null });
  });

  test('un refus du serveur s\'affiche dans la saisie', async ({ serveur, page }) => {
    simulerAcne(serveur, [{ debut: '2026-08-02', fin: '2026-08-20' }]);
    serveur.on('PUT', /^Acne\/episodes\/\d+$/, () => ({ status: 400, body: { message: 'Ces dates chevauchent un autre épisode.' } }));
    await page.goto('/acne');

    await page.getByRole('region', { name: 'Épisodes' }).getByRole('button', { name: /2 août/ }).click();
    await page.getByRole('dialog').getByRole('button', { name: 'Enregistrer' }).click();

    await expect(page.getByRole('dialog').getByRole('alert')).toHaveText('Ces dates chevauchent un autre épisode.');
  });

  test('ajouter une photo ouvre la saisie de l\'acné, avec photo et sans type', async ({ serveur, page }) => {
    simulerAcne(serveur);
    await page.goto('/acne');

    await expect(page.getByRole('region', { name: 'Point de la semaine' })).toContainText('Aucune photo pour l\'instant');
    await page.getByRole('button', { name: 'Ajouter une photo' }).click();

    const panneau = page.getByRole('dialog');
    await expect(panneau.getByRole('heading', { name: 'Noter mon acné' })).toBeVisible();
    await expect(panneau.getByRole('button', { name: 'Fatigue' })).toHaveCount(0);
    await expect(panneau.getByText('Galerie')).toBeVisible();
  });

  test('l\'ancienne adresse de l\'onglet (notification déjà reçue) mène à la page Acné', async ({ serveur, page }) => {
    simulerAcne(serveur, [{ debut: '2026-08-02' }]);
    await page.goto('/cycle?onglet=acne');

    await expect(page).toHaveURL(/\/acne$/);
    await expect(page.getByRole('region', { name: 'En ce moment' })).toBeVisible();
  });

  test('se retrouve depuis le menu « Plus » et depuis l\'onglet Symptômes du cycle', async ({ navigationPage, serveur, page }) => {
    simulerAcne(serveur);
    simulerSymptomes(serveur);
    await page.goto('/cycle?onglet=symptomes');

    await page.getByRole('link', { name: /Suivi de l'acné/ }).click();
    await expect(page).toHaveURL(/\/acne$/);

    await page.goto('/');
    await navigationPage.ouvrirRubrique('Acné');
    await expect(page).toHaveURL(/\/acne$/);
    await expect(page.getByRole('heading', { level: 1 })).toHaveText('Acné');
  });
});
