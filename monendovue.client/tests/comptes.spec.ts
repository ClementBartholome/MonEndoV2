import { test, expect } from './fixtures';
import { simulerConsentement } from './mocks/consentement';

// Liens reçus par e-mail : l'adresse et le jeton sont dans le fragment (#), jamais dans l'URL envoyée au serveur.
const LIEN_CONFIRMATION = '/confirmer-adresse#email=prenom%40exemple.fr&jeton=jeton%2Bde%2Ftest';
const LIEN_REINITIALISATION = '/reinitialiser-mot-de-passe#email=prenom%40exemple.fr&jeton=jeton%2Bde%2Ftest';

test.describe('Comptes sans session', () => {
  test.use({ connectee: false });

  test.beforeEach(({ serveur }) => {
    simulerConsentement(serveur);
  });

  test.describe('confirmation de l\'adresse', () => {
    test('le jeton du lien est posté dans le corps, puis retiré de la barre d\'adresse', async ({ page, serveur }) => {
      await page.goto(LIEN_CONFIRMATION);

      await expect(page.getByRole('heading', { level: 1, name: 'Adresse confirmée' })).toBeVisible();
      expect(serveur.appelsVers('POST', /^Account\/confirmer-adresse$/).at(-1)!.corps).toEqual({ email: 'prenom@exemple.fr', jeton: 'jeton+de/test' });
      expect(page.url()).not.toContain('#');
      expect(page.url()).not.toContain('jeton');
    });

    test('un lien périmé le dit et propose un nouveau lien', async ({ page, serveur }) => {
      serveur.on('POST', /^Account\/confirmer-adresse$/, () => ({ status: 400, body: { message: "Ce lien n'est plus valable. Demande un nouveau lien." } }));
      await page.goto(LIEN_CONFIRMATION);

      await expect(page.getByRole('alert')).toContainText("Ce lien n'est plus valable");
      await page.getByRole('button', { name: 'Recevoir un nouveau lien' }).click();

      await expect(page.getByRole('status')).toContainText('un nouveau lien vient de partir');
      expect(serveur.appelsVers('POST', /^Account\/renvoyer-confirmation$/).at(-1)!.corps).toEqual({ email: 'prenom@exemple.fr' });
    });

    test('un lien incomplet ne part pas vers le serveur', async ({ page, serveur }) => {
      await page.goto('/confirmer-adresse');

      await expect(page.getByRole('alert')).toContainText('Ce lien est incomplet');
      expect(serveur.appelsVers('POST', /^Account\/confirmer-adresse$/)).toHaveLength(0);
    });
  });

  test.describe('connexion', () => {
    test('adresse pas encore confirmée : message et renvoi du lien', async ({ page, serveur }) => {
      serveur.on('POST', /^Account\/login$/, () => ({ status: 403, body: { code: 'adresse-non-confirmee' } }));
      await page.goto('/login');
      await page.locator('input[name="email"]').fill('prenom@exemple.fr');
      await page.locator('input[name="password"]').fill('MotDePasse1!');

      await page.getByRole('button', { name: 'Connexion' }).click();

      await expect(page.getByRole('alert')).toContainText("Ton adresse n'est pas encore confirmée");
      await page.getByRole('button', { name: 'Renvoyer le lien' }).click();
      await expect(page.getByRole('status')).toContainText('Un nouveau lien vient de partir');
      await expect(page).toHaveURL(/\/login$/);
    });

    test('après l\'inscription sur cet appareil, la première connexion passe par l\'étape du rappel', async ({ page }) => {
      await page.addInitScript(() => localStorage.setItem('monendo-accueil-a-faire', '1'));
      await page.goto('/login');
      await page.locator('input[name="email"]').fill('prenom@exemple.fr');
      await page.locator('input[name="password"]').fill('MotDePasse1!');

      await page.getByRole('button', { name: 'Connexion' }).click();

      // Sans notification possible, l'étape passe d'elle-même jusqu'à l'accueil ; le drapeau ne sert qu'une fois.
      await expect(page).toHaveURL(/\/$/);
      expect(await page.evaluate(() => localStorage.getItem('monendo-accueil-a-faire'))).toBeNull();
    });
  });

  test.describe('mot de passe oublié', () => {
    test('depuis la connexion, la réponse ne dit pas si l\'adresse a un compte', async ({ page, serveur }) => {
      serveur.on('POST', /^Account\/mot-de-passe-oublie$/, () => ({ status: 202 }));
      await page.goto('/login');
      await page.locator('input[name="email"]').fill('prenom@exemple.fr');

      await page.getByRole('link', { name: 'Mot de passe oublié ?' }).click();
      await expect(page.locator('input[name="email"]')).toHaveValue('prenom@exemple.fr');
      await page.getByRole('button', { name: 'Recevoir le lien' }).click();

      await expect(page.getByRole('status')).toContainText('Si un compte existe pour prenom@exemple.fr, un lien vient de partir');
      expect(serveur.appelsVers('POST', /^Account\/mot-de-passe-oublie$/).at(-1)!.corps).toEqual({ email: 'prenom@exemple.fr' });
    });

    test('une adresse mal saisie n\'est pas envoyée', async ({ page, serveur }) => {
      await page.goto('/mot-de-passe-oublie');
      await page.locator('input[name="email"]').fill('pas-une-adresse');

      await page.getByRole('button', { name: 'Recevoir le lien' }).click();

      await expect(page.getByText('Saisis une adresse e-mail valide.')).toBeVisible();
      expect(serveur.appelsVers('POST', /^Account\/mot-de-passe-oublie$/)).toHaveLength(0);
    });

    test('service d\'e-mails indisponible : un message le dit', async ({ page, serveur }) => {
      serveur.on('POST', /^Account\/mot-de-passe-oublie$/, () => ({ status: 503, body: { message: 'indisponible' } }));
      await page.goto('/mot-de-passe-oublie');
      await page.locator('input[name="email"]').fill('prenom@exemple.fr');

      await page.getByRole('button', { name: 'Recevoir le lien' }).click();

      await expect(page.getByRole('alert')).toContainText('momentanément indisponible');
    });
  });

  test.describe('inscription sur invitation', () => {
    test.beforeEach(({ serveur }) => {
      simulerConsentement(serveur, { surInvitation: true });
    });

    test('le code du lien partagé est prérempli, retiré de l\'adresse et envoyé avec l\'inscription', async ({ page, serveur }) => {
      await page.goto('/register#code=ABCD-EFGH-JKLM');

      await expect(page.locator('input[name="code"]')).toHaveValue('ABCD-EFGH-JKLM');
      expect(page.url()).not.toContain('ABCD');
      await page.locator('input[name="email"]').fill('invitee@exemple.fr');
      await page.locator('input[name="password"]').fill('MotDePasse1!');
      await page.getByRole('checkbox', { name: /J'accepte que MonEndo enregistre/ }).check();
      await page.getByRole('button', { name: 'Créer mon compte' }).click();

      await expect(page).toHaveURL(/\/inscription\/envoyee$/);
      expect(serveur.appelsVers('POST', /^Account\/register$/).at(-1)!.corps).toMatchObject({ codeInvitation: 'ABCD-EFGH-JKLM' });
    });

    test('sans code, rien n\'est envoyé ; un code refusé est expliqué sous le champ', async ({ page, serveur }) => {
      await page.goto('/register');
      await page.locator('input[name="email"]').fill('invitee@exemple.fr');
      await page.locator('input[name="password"]').fill('MotDePasse1!');
      await page.getByRole('checkbox', { name: /J'accepte que MonEndo enregistre/ }).check();

      await page.getByRole('button', { name: 'Créer mon compte' }).click();
      await expect(page.getByText("Saisis le code d'invitation que tu as reçu.")).toBeVisible();
      expect(serveur.appelsVers('POST', /^Account\/register$/)).toHaveLength(0);

      serveur.on('POST', /^Account\/register$/, () => ({
        status: 400, body: { $values: ["Ce code d'invitation n'est pas valable (inconnu, expiré ou déjà utilisé). Demande un nouveau code à la personne qui t'a invitée."] },
      }));
      await page.locator('input[name="code"]').fill('ZZZZ-ZZZZ-ZZZZ');
      await page.getByRole('button', { name: 'Créer mon compte' }).click();

      await expect(page.getByText("Ce code d'invitation n'est pas valable")).toBeVisible();
      await expect(page).toHaveURL(/\/register$/);
    });

    test('inscriptions ouvertes : pas de champ de code', async ({ page, serveur }) => {
      serveur.on('GET', /^Account\/inscription$/, () => ({ body: { surInvitation: false } }));
      await page.goto('/register');

      await expect(page.locator('input[name="email"]')).toBeVisible();
      await expect(page.locator('input[name="code"]')).toHaveCount(0);
    });
  });

  test.describe('nouveau mot de passe', () => {
    test('les règles sont vérifiées avant l\'envoi, puis le jeton part dans le corps', async ({ page, serveur }) => {
      serveur.on('POST', /^Account\/reinitialiser-mot-de-passe$/, () => ({ status: 204 }));
      await page.goto(LIEN_REINITIALISATION);
      expect(page.url()).not.toContain('jeton');
      await expect(page.getByText('prenom@exemple.fr')).toBeVisible();

      await page.locator('input[name="password"]').fill('court');
      await page.getByRole('button', { name: 'Enregistrer le mot de passe' }).click();
      await expect(page.getByText('Ton mot de passe ne respecte pas encore toutes les règles.')).toBeVisible();
      expect(serveur.appelsVers('POST', /^Account\/reinitialiser-mot-de-passe$/)).toHaveLength(0);

      await page.locator('input[name="password"]').fill('NouveauMotDePasse2!');
      await page.getByRole('button', { name: 'Enregistrer le mot de passe' }).click();

      await expect(page.getByRole('heading', { level: 1, name: 'Mot de passe changé' })).toBeVisible();
      await expect(page.getByText('toutes tes sessions ouvertes ont été fermées')).toBeVisible();
      expect(serveur.appelsVers('POST', /^Account\/reinitialiser-mot-de-passe$/).at(-1)!.corps)
        .toEqual({ email: 'prenom@exemple.fr', jeton: 'jeton+de/test', nouveauMotDePasse: 'NouveauMotDePasse2!' });
      await page.getByRole('link', { name: 'Me connecter' }).click();
      await expect(page.locator('input[name="email"]')).toHaveValue('prenom@exemple.fr');
    });

    test('un lien périmé le dit et renvoie vers une nouvelle demande', async ({ page, serveur }) => {
      serveur.on('POST', /^Account\/reinitialiser-mot-de-passe$/, () => ({
        status: 400, body: { message: "Ce lien n'est plus valable. Demande un nouveau lien.", erreurs: ["Ce lien n'est plus valable. Demande un nouveau lien."] },
      }));
      await page.goto(LIEN_REINITIALISATION);
      await page.locator('input[name="password"]').fill('NouveauMotDePasse2!');

      await page.getByRole('button', { name: 'Enregistrer le mot de passe' }).click();

      await expect(page.getByRole('alert')).toContainText("Ce lien n'est plus valable");
      await page.getByRole('alert').getByRole('link', { name: 'Demander un nouveau lien' }).click();
      await expect(page).toHaveURL(/\/mot-de-passe-oublie$/);
    });
  });
});
