import { test, expect } from './fixtures';
import { UTILISATRICE } from './mocks/session';

/** Texte d'une notification (le conteneur aria-live en double le texte pour les lecteurs d'écran). */
const notification = (page: import('@playwright/test').Page, texte: string | RegExp) => page.locator('.text-sm', { hasText: texte });

const JETON_LOINTAIN = new Date(2099, 0, 1).toISOString();

/** Jeton d'accès déjà expiré au chargement : le premier appel de l'API doit d'abord renouveler la session. */
async function ouvrirAvecJetonExpire(page: import('@playwright/test').Page) {
  await page.addInitScript((u) => localStorage.setItem('user', JSON.stringify(u)), {
    ...UTILISATRICE,
    tokenExpiry: new Date(2020, 0, 1).toISOString(),
  });
}

test.describe('Session : renouvellement et connexion', () => {
  test("un serveur momentanément injoignable au renouvellement ne déconnecte pas", async ({ serveur, page }) => {
    await ouvrirAvecJetonExpire(page);
    let appels = 0;
    serveur.on('POST', /^Account\/refresh-token$/, () => (++appels === 1
      ? { status: 502, body: 'Bad Gateway' }
      : { body: { tokenExpiry: JETON_LOINTAIN } }));

    await page.goto('/');

    await expect(page.getByRole('link', { name: 'Douleurs' })).toBeVisible();
    await expect(page).not.toHaveURL(/login/);
    // Nouvel essai après une courte pause : la session est renouvelée au second appel.
    await expect.poll(() => appels).toBe(2);
    expect(await page.evaluate(() => localStorage.getItem('user'))).not.toBeNull();
  });

  test("un refus du serveur au renouvellement renvoie vers la connexion", async ({ serveur, page }) => {
    await ouvrirAvecJetonExpire(page);
    serveur.on('POST', /^Account\/refresh-token$/, () => ({ status: 400, body: 'Invalid refresh token' }));

    await page.goto('/');

    await expect(page).toHaveURL(/login/);
    await expect(notification(page, 'Veuillez-vous reconnecter')).toBeVisible();
    expect(await page.evaluate(() => localStorage.getItem('user'))).toBeNull();
  });

  test("plusieurs appels simultanés ne renouvellent la session qu'une fois", async ({ serveur, page }) => {
    await ouvrirAvecJetonExpire(page);
    serveur.on('POST', /^Account\/refresh-token$/, () => ({ body: { tokenExpiry: JETON_LOINTAIN } }));

    await page.goto('/');
    await expect(page.getByRole('link', { name: 'Douleurs' })).toBeVisible();

    expect(serveur.appelsVers('POST', /^Account\/refresh-token$/)).toHaveLength(1);
  });

  test.describe('connexion', () => {
    test.use({ connectee: false });

    const seConnecter = async (page: import('@playwright/test').Page) => {
      await page.goto('/login');
      await page.locator('input[name="email"]').fill('utilisatrice@test.local');
      await page.locator('input[name="password"]').fill('MotDePasse1!');
      await page.getByRole('button', { name: 'Connexion' }).click();
    };

    test("des identifiants refusés invitent à les vérifier", async ({ serveur, page }) => {
      serveur.on('POST', /^Account\/login$/, () => ({ status: 401 }));

      await seConnecter(page);

      await expect(notification(page, 'Veuillez vérifier votre email et votre mot de passe.')).toBeVisible();
    });

    test("un serveur injoignable ne fait pas accuser le mot de passe", async ({ serveur, page }) => {
      serveur.on('POST', /^Account\/login$/, () => ({ status: 502, body: 'Bad Gateway' }));

      await seConnecter(page);

      await expect(notification(page, 'Le service est momentanément indisponible.')).toBeVisible();
      await expect(notification(page, 'vérifier votre email')).toHaveCount(0);
    });

    test("trop de tentatives : on demande de patienter", async ({ serveur, page }) => {
      serveur.on('POST', /^Account\/login$/, () => ({ status: 429 }));

      await seConnecter(page);

      await expect(notification(page, /Trop de tentatives/)).toBeVisible();
    });
  });
});
