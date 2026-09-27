import type { Page, TestInfo } from '@playwright/test';
import { expect, test } from './support/fixtures';
import { simulerActivite } from './support/activite';

/** Texte visible : la page contient à la fois les cartes (mobile) et le tableau (desktop), l'un des deux est masqué. */
const visible = (page: Page, texte: string) => page.getByText(texte, { exact: true }).and(page.locator(':visible'));

/** Bouton « Modifier » ou « Supprimer » d'une séance : carte sur mobile, ligne du tableau sur desktop. */
const actionSur = (page: Page, testInfo: TestInfo, titre: string, action: 'Modifier' | 'Supprimer') => {
  if (testInfo.project.name === 'mobile') {
    return page.locator('div', { has: visible(page, titre) })
      .getByRole('button', { name: action }).last();
  }
  return page.getByRole('row', { name: new RegExp(titre) })
    .locator(action === 'Modifier' ? '.edit-btn' : '.delete-btn');
};

test.describe('Activité physique', () => {
  test('affiche les séances du mois', async ({ page, serveur }) => {
    simulerActivite(serveur, [{ typeActivite: 'Natation', date: '2026-09-10T08:00:00', duree: 45 }]);

    await page.goto('/activite');

    await expect(visible(page, 'Natation')).toBeVisible();
  });

  test('ajoute une séance', async ({ page, serveur }) => {
    const seances = simulerActivite(serveur);
    await page.goto('/activite');

    await page.locator('.form-modal button').first().click();
    const formulaire = page.getByRole('dialog');
    await formulaire.getByPlaceholder('Course à pied').fill('Yoga');
    await formulaire.locator('input[type="date"]').fill('2026-09-14');
    await formulaire.locator('input[type="time"]').fill('18:30');
    await formulaire.getByPlaceholder('Durée en minutes').fill('40');
    await formulaire.getByRole('button', { name: 'Enregistrer' }).click();

    await expect(visible(page, 'Yoga')).toBeVisible();
    expect(seances).toHaveLength(1);
    expect(seances[0]).toMatchObject({ typeActivite: 'Yoga', duree: 40 });
  });

  test('modifie une séance', async ({ page, serveur }, testInfo) => {
    const seances = simulerActivite(serveur, [{ typeActivite: 'Marche', duree: 30 }]);
    await page.goto('/activite');

    await actionSur(page, testInfo, 'Marche', 'Modifier').click();
    const formulaire = page.getByRole('dialog');
    await formulaire.getByPlaceholder('Durée en minutes').fill('50');
    await formulaire.getByRole('button', { name: 'Mettre à jour' }).click();

    await expect(formulaire).toBeHidden();
    expect(seances[0]).toMatchObject({ typeActivite: 'Marche', duree: 50 });
  });

  test('supprime une séance', async ({ page, serveur }, testInfo) => {
    const seances = simulerActivite(serveur, [{ typeActivite: 'Vélo' }]);
    await page.goto('/activite');

    await actionSur(page, testInfo, 'Vélo', 'Supprimer').click();

    await expect(page.getByText('La session a été supprimée avec succès', { exact: true })).toBeVisible();
    await expect(visible(page, 'Vélo')).toHaveCount(0);
    expect(seances).toHaveLength(0);
  });
});
