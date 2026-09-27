import { test, expect } from './fixtures';
import { ouvrirSessionSansConsentement, REFUS_CONSENTEMENT, simulerConsentement } from './mocks/consentement';

test.describe('Consentement aux données de santé', () => {
  test.use({ connectee: false });

  test("l'inscription exige de cocher l'accord, envoyé au serveur", async ({ consentementPage, serveur, page }) => {
    simulerConsentement(serveur);
    await consentementPage.ouvrirInscription();
    await consentementPage.remplirIdentifiants('nouvelle@test.local', 'MotDePasse1!');

    await expect(consentementPage.caseAccord).not.toBeChecked();
    await expect(consentementPage.boutonInscription).toBeDisabled();

    await consentementPage.caseAccord.check();
    await consentementPage.boutonInscription.click();

    await expect(page).toHaveURL(/\/$/);
    expect(serveur.appelsVers('POST', /^Account\/register$/)[0].corps).toMatchObject({ consentementDonneesSante: true });
  });

  test("un compte sans accord ne voit que la page d'accord, puis retrouve l'application", async ({ consentementPage, serveur, page }) => {
    simulerConsentement(serveur);
    await ouvrirSessionSansConsentement(page, 'refuse');

    await page.goto('/douleurs');

    await expect(consentementPage.titre).toBeVisible();
    await expect(consentementPage.navigation).toHaveCount(0);
    await expect(consentementPage.boutonAccord).toBeDisabled();

    await consentementPage.caseAccord.check();
    await consentementPage.boutonAccord.click();

    await expect(page).toHaveURL(/\/$/);
    expect(serveur.appelsVers('POST', /^Account\/consentement$/)).toHaveLength(1);
  });

  test("sans donner son accord, on peut se déconnecter", async ({ consentementPage, page }) => {
    await ouvrirSessionSansConsentement(page, 'refuse');
    await page.goto('/');

    await consentementPage.boutonDeconnexion.click();

    await expect(page).toHaveURL(/\/login$/);
  });

  test("une session d'avant l'accord est renvoyée vers la page d'accord quand l'API le demande", async ({ consentementPage, serveur, page }) => {
    serveur.on('GET', /^CarnetSante\/last-entries\/\d+$/, () => REFUS_CONSENTEMENT);
    await ouvrirSessionSansConsentement(page, 'absent');

    await page.goto('/');

    await expect(consentementPage.titre).toBeVisible();
  });
});
