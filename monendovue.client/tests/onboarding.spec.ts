import { test, expect } from './fixtures';
import { simulerConsentement } from './mocks/consentement';
import { simulerDouleurs } from './mocks/douleurs';
import { simulerIphoneDansSafari, simulerNavigateurPush, simulerNotifications } from './mocks/onboarding';

test.describe('Onboarding', () => {
  test.use({ connectee: false });

  test.describe('arrivée', () => {
    test('une première visite commence par la bienvenue', async ({ page }) => {
      await page.goto('/');

      await expect(page).toHaveURL(/\/bienvenue$/);
      await expect(page.getByRole('heading', { level: 1, name: 'Ta douleur, noir sur blanc.' })).toBeVisible();
      await expect(page.getByText('Gratuit · Sans publicité · Hébergé en Europe')).toBeVisible();
      await page.getByRole('link', { name: "J'ai déjà un compte" }).click();
      await expect(page).toHaveURL(/\/login$/);
    });

    test('« Créer mon compte » mène à l\'inscription', async ({ page }) => {
      await page.goto('/bienvenue');

      await page.getByRole('link', { name: 'Créer mon compte' }).click();

      await expect(page).toHaveURL(/\/register$/);
      await expect(page.getByRole('heading', { level: 1, name: 'Crée ton compte' })).toBeVisible();
    });

    test('une personne déjà venue arrive directement sur la connexion', async ({ page }) => {
      await page.addInitScript(() => localStorage.setItem('monendo-deja-venue', '1'));

      await page.goto('/');

      await expect(page).toHaveURL(/\/login$/);
    });
  });

  test.describe('inscription', () => {
    test.beforeEach(({ serveur }) => {
      simulerConsentement(serveur);
    });

    test('les engagements précèdent le consentement, et les règles se cochent pendant la saisie', async ({ page }) => {
      await page.goto('/register');
      const regles = page.getByRole('list', { name: 'Règles du mot de passe' });

      await expect(page.getByRole('heading', { name: "Ce que MonEndo s'engage à faire de tes données" })).toBeVisible();
      await expect(page.getByText("Ton suivi n'est visible que depuis ton compte.")).toBeVisible();
      await expect(regles).not.toContainText(/: respectée/);
      await page.locator('input[name="password"]').fill('MotDePasse1');
      await expect(regles.getByText('8 caractères au moins')).toContainText(': respectée');
      await expect(regles.getByText('Un chiffre')).toContainText(': respectée');
      await expect(regles.getByText('Un caractère spécial')).toContainText('pas encore respectée');
      await page.locator('input[name="password"]').fill('MotDePasse1!');
      await expect(regles.getByText('Un caractère spécial')).toContainText(': respectée');
    });

    test("le mot de passe peut être affiché", async ({ page }) => {
      await page.goto('/register');
      const champ = page.locator('input[name="password"]');
      await champ.fill('MotDePasse1!');

      await expect(champ).toHaveAttribute('type', 'password');
      await page.getByRole('button', { name: 'Afficher le mot de passe' }).click();
      await expect(champ).toHaveAttribute('type', 'text');
    });

    test("rien n'est envoyé tant que l'adresse, le mot de passe et la case ne sont pas bons ; chaque erreur est au bon endroit", async ({ page, serveur }) => {
      await page.goto('/register');

      await page.getByRole('button', { name: 'Créer mon compte' }).click();

      await expect(page.getByText('Saisis une adresse e-mail valide.')).toBeVisible();
      await expect(page.getByText('Ton mot de passe ne respecte pas encore toutes les règles.')).toBeVisible();
      await expect(page.getByText('Coche cette case pour créer ton compte.')).toBeVisible();
      expect(serveur.appelsVers('POST', /^Account\/register$/)).toHaveLength(0);
    });

    test('adresse déjà utilisée : message doux et lien pour se connecter', async ({ page, serveur }) => {
      serveur.on('POST', /^Account\/register$/, () => ({ status: 400, body: { $values: ['Cette adresse e-mail est déjà utilisée.'] } }));
      await page.goto('/register');
      await page.locator('input[name="email"]').fill('prenom@exemple.fr');
      await page.locator('input[name="password"]').fill('MotDePasse1!');
      await page.getByRole('checkbox', { name: /J'accepte que MonEndo enregistre/ }).check();

      await page.getByRole('button', { name: 'Créer mon compte' }).click();

      await expect(page.getByText(/Impossible de créer un compte avec cette adresse/)).toBeVisible();
      await page.getByRole('alert').getByRole('link', { name: 'Se connecter' }).click();
      await expect(page).toHaveURL(/\/login$/);
    });

    test('pas de connexion : la saisie est conservée et un message le dit', async ({ page, serveur }) => {
      serveur.on('POST', /^Account\/register$/, () => ({ status: 503 }));
      await page.goto('/register');
      await page.locator('input[name="email"]').fill('prenom@exemple.fr');
      await page.locator('input[name="password"]').fill('MotDePasse1!');
      await page.getByRole('checkbox', { name: /J'accepte que MonEndo enregistre/ }).check();

      await page.getByRole('button', { name: 'Créer mon compte' }).click();

      await expect(page.getByRole('alert')).toContainText('momentanément indisponible');
      await expect(page.locator('input[name="email"]')).toHaveValue('prenom@exemple.fr');
      await expect(page.getByRole('button', { name: 'Créer mon compte' })).toBeEnabled();
    });

    test("la politique s'ouvre dans un nouvel onglet pour ne rien perdre de la saisie", async ({ page }) => {
      await page.goto('/register');

      await expect(page.getByRole('link', { name: 'politique de confidentialité' })).toHaveAttribute('target', '_blank');
    });
  });

  test.describe('après l\'inscription', () => {
    async function sInscrire(page: import('@playwright/test').Page) {
      await page.goto('/register');
      await page.locator('input[name="email"]').fill('prenom@exemple.fr');
      await page.locator('input[name="password"]').fill('MotDePasse1!');
      await page.getByRole('checkbox', { name: /J'accepte que MonEndo enregistre/ }).check();
      await page.getByRole('button', { name: 'Créer mon compte' }).click();
    }

    test("un appareil sans notification passe l'étape rappel et arrive sur l'accueil avec « Pour bien démarrer »", async ({ page, serveur }) => {
      simulerConsentement(serveur);

      await sInscrire(page);

      await expect(page).toHaveURL(/\/$/);
      const carte = page.getByRole('region', { name: 'Pour bien démarrer' });
      await expect(carte).toBeVisible();
      await expect(carte.getByRole('link', { name: 'Noter une douleur' })).toBeVisible();
      await expect(carte.getByRole('link', { name: 'Noter mes règles' })).toBeVisible();
      await expect(carte.getByRole('link', { name: "Des sources fiables sur l'endométriose" })).toBeVisible();
      // Sans progression ni tâche à terminer.
      await expect(carte.getByRole('progressbar')).toHaveCount(0);
    });

    test('la carte se ferme et ne revient pas', async ({ page, serveur }) => {
      simulerConsentement(serveur);
      await sInscrire(page);
      const carte = page.getByRole('region', { name: 'Pour bien démarrer' });

      await carte.getByRole('button', { name: 'Masquer cette carte' }).click();
      await expect(carte).toHaveCount(0);

      await page.reload();
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
      await expect(carte).toHaveCount(0);
    });

    test("utiliser un raccourci de la carte la retire", async ({ page, serveur }) => {
      simulerConsentement(serveur);
      simulerDouleurs(serveur);
      await sInscrire(page);

      await page.getByRole('region', { name: 'Pour bien démarrer' }).getByRole('link', { name: 'Noter une douleur' }).click();

      await expect(page).toHaveURL(/\/douleurs/);
      expect(await page.evaluate(() => localStorage.getItem('monendo-premiers-pas'))).toBeNull();
    });

    test('rappel : on choisit l\'heure, le rappel du bilan est activé à cette heure', async ({ page, serveur }) => {
      simulerConsentement(serveur);
      simulerNotifications(serveur);
      await simulerNavigateurPush(page, 'granted');
      await sInscrire(page);

      await expect(page).toHaveURL(/\/bienvenue\/rappel$/);
      await expect(page.getByRole('heading', { level: 1 })).toContainText('Un rappel, si tu veux');
      await expect(page.getByText('La notification ne contient jamais de donnée de santé.')).toBeVisible();
      await expect(page.getByRole('button', { name: '20 h' })).toHaveAttribute('aria-pressed', 'true');
      await page.getByRole('button', { name: '21 h' }).click();
      await page.getByRole('button', { name: 'Activer le rappel' }).click();

      await expect(page).toHaveURL(/\/$/);
      const envois = serveur.appelsVers('PUT', /^Notifications\/rappels\/BilanQuotidien$/);
      expect(envois.at(-1)?.corps).toMatchObject({ actif: true, heure: '21:00' });
      await expect(page.locator('.text-sm', { hasText: 'Tu recevras un rappel chaque soir à 21 h.' })).toBeVisible();
    });

    test('rappel : « Autre » permet une heure libre', async ({ page, serveur }) => {
      simulerConsentement(serveur);
      simulerNotifications(serveur);
      await simulerNavigateurPush(page, 'granted');
      await sInscrire(page);

      await page.getByRole('button', { name: 'Autre' }).click();
      await page.locator('input[type="time"]').fill('18:30');
      await page.getByRole('button', { name: 'Activer le rappel' }).click();

      await expect(page).toHaveURL(/\/$/);
      expect(serveur.appelsVers('PUT', /^Notifications\/rappels\/BilanQuotidien$/).at(-1)?.corps).toMatchObject({ heure: '18:30' });
    });

    test('rappel : « Pas maintenant » passe sans rien activer', async ({ page, serveur }) => {
      simulerConsentement(serveur);
      simulerNotifications(serveur);
      await simulerNavigateurPush(page, 'granted');
      await sInscrire(page);

      await page.getByRole('button', { name: 'Pas maintenant' }).click();

      await expect(page).toHaveURL(/\/$/);
      expect(serveur.appelsVers('PUT', /^Notifications\/rappels\//)).toHaveLength(0);
      expect(serveur.appelsVers('POST', /^Notifications\/abonnements$/)).toHaveLength(0);
    });

    test('rappel : une permission refusée est expliquée, sans bloquer', async ({ page, serveur }) => {
      simulerConsentement(serveur);
      simulerNotifications(serveur);
      await simulerNavigateurPush(page, 'denied');
      await sInscrire(page);

      await page.getByRole('button', { name: 'Activer le rappel' }).click();

      await expect(page.getByRole('status')).toContainText('Les notifications sont bloquées par ton navigateur.');
      await page.getByRole('button', { name: 'Continuer' }).click();
      await expect(page).toHaveURL(/\/$/);
    });

    test("iPhone dans Safari : l'installation est expliquée au lieu de demander une permission", async ({ page, serveur }) => {
      simulerConsentement(serveur);
      simulerNotifications(serveur);
      await simulerIphoneDansSafari(page);
      await sInscrire(page);

      await expect(page.getByText("Sur iPhone, les rappels n'arrivent que si MonEndo est installé sur ton écran d'accueil.")).toBeVisible();
      await expect(page.getByText('Touche Partager')).toBeVisible();
      await expect(page.getByText("« Sur l'écran d'accueil »")).toBeVisible();
      await expect(page.getByRole('button', { name: 'Activer le rappel' })).toHaveCount(0);
      await page.getByRole('button', { name: "J'ai compris" }).click();
      await expect(page).toHaveURL(/\/$/);
    });
  });
});
