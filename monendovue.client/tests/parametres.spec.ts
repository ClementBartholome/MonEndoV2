import { expect, test } from './fixtures';
import { simulerParametresSansNotifications } from './mocks/parametres';

test.describe('Paramètres', () => {
  test.beforeEach(({ serveur }) => {
    simulerParametresSansNotifications(serveur);
  });

  test('regroupe les réglages en lignes : rappels, suivi, compte, mes données', async ({ parametresPage, page }) => {
    await parametresPage.ouvrir();

    await expect(page.getByRole('heading', { level: 1, name: 'Paramètres' })).toBeVisible();
    for (const groupe of ['Rappels', 'Suivi', 'Compte', 'Mes données']) {
      await expect(page.getByRole('heading', { level: 2, name: groupe })).toBeVisible();
    }
    for (const ligne of ['Notifications', 'Repères personnels', 'Mot de passe', 'Se déconnecter', 'Télécharger mes données', 'Supprimer mon compte']) {
      await expect(parametresPage.ligne(ligne)).toBeVisible();
    }
    await expect(page.getByText(/^MonEndo v\d/)).toBeVisible();
  });

  test('les notifications non disponibles sont expliquées dans leur panneau', async ({ parametresPage }) => {
    await parametresPage.ouvrir();

    await parametresPage.ligne('Notifications').click();

    await expect(parametresPage.panneau('Notifications')).toBeVisible();
    await expect(parametresPage.panneau('Notifications')).toContainText(/ne sont pas disponibles|ne permet pas/);
  });

  test('enregistre des repères personnels sur cet appareil', async ({ parametresPage, page }) => {
    await parametresPage.ouvrir();

    await parametresPage.ligne('Repères personnels').click();
    await parametresPage.panneau('Repères personnels').getByLabel('Douleur (sur 10, au plus)').fill('4');
    await parametresPage.panneau('Repères personnels').getByRole('button', { name: 'Enregistrer' }).click();

    await expect(page.getByText('Repères enregistrés', { exact: true })).toBeVisible();
    await expect(parametresPage.panneau('Repères personnels')).toBeHidden();
    expect(await page.evaluate(() => JSON.parse(localStorage.getItem('monendo.wellbeing-goals') ?? '{}').painMaxGoal)).toBe(4);
  });

  test('change le mot de passe ; deux saisies différentes sont refusées', async ({ parametresPage, serveur, page }) => {
    serveur.on('POST', /^Account\/change-password$/, () => ({ status: 200, body: {} }));
    await parametresPage.ouvrir();
    await parametresPage.ligne('Mot de passe').click();
    const panneau = parametresPage.panneau('Mot de passe');

    await panneau.getByLabel('Mot de passe actuel').fill('Faux');
    await panneau.getByLabel('Nouveau mot de passe', { exact: true }).fill('MotDePasse1!');
    await panneau.getByLabel('Confirmer le nouveau mot de passe').fill('Autre');
    await panneau.getByRole('button', { name: 'Changer le mot de passe' }).click();
    await expect(panneau.getByRole('alert')).toHaveText('Les mots de passe ne correspondent pas.');

    await panneau.getByLabel('Confirmer le nouveau mot de passe').fill('MotDePasse1!');
    await panneau.getByRole('button', { name: 'Changer le mot de passe' }).click();

    await expect(page.getByText('Mot de passe changé', { exact: true })).toBeVisible();
    expect(serveur.appelsVers('POST', /^Account\/change-password$/)[0].corps).toEqual({ currentPassword: 'Faux', newPassword: 'MotDePasse1!' });
  });

  test('se déconnecter ramène à la connexion', async ({ parametresPage, serveur, page }) => {
    serveur.on('POST', /^Account\/logout$/, () => ({ status: 200, body: {} }));
    await parametresPage.ouvrir();

    await parametresPage.ligne('Se déconnecter').click();

    await expect(page).toHaveURL(/\/login$/);
    expect(await page.evaluate(() => localStorage.getItem('user'))).toBeNull();
  });
});
