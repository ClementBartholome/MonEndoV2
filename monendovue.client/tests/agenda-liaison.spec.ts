import { test, expect } from './fixtures';
import { CALENDRIERS, simulerLiaisonAgenda, URL_GOOGLE } from './mocks/agenda';

test.describe("Liaison de l'agenda Google", () => {
  test("sans configuration Google, la ligne n'est pas proposée", async ({ parametresPage, serveur }) => {
    simulerLiaisonAgenda(serveur, { disponible: false });

    await parametresPage.ouvrir();

    await expect(parametresPage.page.getByRole('heading', { name: 'Notifications', exact: false })).toHaveCount(0);
    await expect(parametresPage.ligne('Agenda Google')).toHaveCount(0);
  });

  test("lier : prévient de l'écran Google, puis envoie vers Google", async ({ parametresPage, serveur, page }) => {
    simulerLiaisonAgenda(serveur);
    await page.route('https://accounts.google.com/**', (route) => route.fulfill({ body: 'Google' }));
    await parametresPage.ouvrir();

    await expect(parametresPage.ligne('Agenda Google')).toContainText('Non lié');
    await parametresPage.ligne('Agenda Google').click();
    const panneau = parametresPage.panneau('Agenda Google');
    await expect(panneau.getByText(/Cette application n'a pas été validée/)).toBeVisible();
    await panneau.getByRole('button', { name: 'Lier mon agenda Google' }).click();

    await page.waitForURL(/accounts\.google\.com/);
    expect(page.url()).toBe(URL_GOOGLE);
    expect(serveur.appelsVers('POST', /^Agenda\/liaison\/demarrer$/)).toHaveLength(1);
  });

  test("n'envoie jamais le navigateur ailleurs que chez Google", async ({ parametresPage, serveur, page }) => {
    simulerLiaisonAgenda(serveur, { urlDemarrage: 'https://autre-site.example/auth' });
    await parametresPage.ouvrir();

    await parametresPage.ligne('Agenda Google').click();
    await parametresPage.panneau('Agenda Google').getByRole('button', { name: 'Lier mon agenda Google' }).click();

    await expect(parametresPage.panneau('Agenda Google').getByRole('alert')).toContainText("La liaison n'a pas pu démarrer");
    await expect(page).toHaveURL(/\/parametres$/);
  });

  test("retour de Google réussi : on choisit le calendrier lu, et rien n'est lu avant", async ({ parametresPage, serveur, page }) => {
    simulerLiaisonAgenda(serveur, { liee: true });

    await page.goto('/parametres?agenda=lie');

    await expect(page).toHaveURL(/\/parametres$/);
    const panneau = parametresPage.panneau('Agenda Google');
    await expect(panneau.getByText(/rien n'est lu/)).toBeVisible();
    for (const calendrier of CALENDRIERS) {
      await expect(panneau.getByRole('radio', { name: new RegExp(calendrier.nom) })).not.toBeChecked();
    }
    await expect(panneau.getByRole('link', { name: 'Voir mon agenda' })).toHaveCount(0);

    await panneau.getByRole('radio', { name: /Rendez-vous médicaux/ }).check();

    await expect(panneau.getByRole('radio', { name: /Rendez-vous médicaux/ })).toBeChecked();
    await expect(panneau.getByRole('link', { name: 'Voir mon agenda' })).toBeVisible();
    expect(serveur.appelsVers('PUT', /^Agenda\/calendrier$/)[0].corps).toEqual({ id: 'rdv@example.com' });
  });

  test('retour de Google en échec : message rassurant et nouvelle tentative possible', async ({ parametresPage, serveur, page }) => {
    simulerLiaisonAgenda(serveur);

    await page.goto('/parametres?agenda=echec');

    await expect(page.locator('.text-sm', { hasText: 'Tu peux réessayer' })).toBeVisible();
    await expect(page).toHaveURL(/\/parametres$/);
    await expect(parametresPage.ligne('Agenda Google')).toContainText('Non lié');
  });

  test("délier : l'agenda n'est plus lié", async ({ parametresPage, serveur }) => {
    simulerLiaisonAgenda(serveur, { liee: true, calendrierId: 'rdv@example.com' });
    await parametresPage.ouvrir();

    await expect(parametresPage.ligne('Agenda Google')).toContainText('prochains rendez-vous');
    await parametresPage.ligne('Agenda Google').click();
    const panneau = parametresPage.panneau('Agenda Google');
    await expect(panneau.getByText(/lié depuis le 20 septembre 2026/)).toBeVisible();
    await expect(panneau.getByRole('radio', { name: /Rendez-vous médicaux/ })).toBeChecked();
    await panneau.getByRole('button', { name: 'Délier mon agenda' }).click();

    await expect(panneau.getByRole('button', { name: 'Lier mon agenda Google' })).toBeVisible();
    expect(serveur.appelsVers('DELETE', /^Agenda\/liaison$/)).toHaveLength(1);
  });
});
