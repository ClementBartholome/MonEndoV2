import { test, expect } from './fixtures';

/** Lit l'objet et le corps d'un lien mailto:, décodés. */
function lireMailto(href: string | null) {
  const url = new URL(href ?? '');
  return { adresse: url.pathname, sujet: url.searchParams.get('subject'), corps: url.searchParams.get('body') ?? '' };
}

test.describe('Une suggestion ? Un bug ?', () => {
  test('la rubrique se trouve dans la navigation et ouvre la page', async ({ navigationPage, page }) => {
    await page.goto('/');

    await navigationPage.ouvrirRubrique('Suggestion ou bug');

    await expect(page).toHaveURL(/\/suggestions$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Une suggestion ? Un bug ?' })).toBeVisible();
  });

  test('chaque lien prépare un e-mail à l\'éditeur, avec le contexte utile et rien de plus', async ({ navigationPage, page }) => {
    await page.goto('/');
    await navigationPage.ouvrirRubrique('Suggestion ou bug');
    const nav = page.getByRole('navigation', { name: "Écrire à l'éditeur" });

    const idee = lireMailto(await nav.getByRole('link', { name: /J'ai une idée/ }).getAttribute('href'));
    const bug = lireMailto(await nav.getByRole('link', { name: /Je signale un problème/ }).getAttribute('href'));

    expect(idee.adresse).toBe('clementoss@gmail.com');
    expect(idee.sujet).toBe('MonEndo — suggestion');
    expect(bug.sujet).toBe('MonEndo — bug');
    expect(idee.corps).toContain('Mon idée :');
    expect(bug.corps).toContain('Ce que je faisais :');
    expect(bug.corps).toContain("Ce qui s'est passé :");
    // Page d'où l'on vient (chemin seulement), version de l'application et appareil.
    expect(bug.corps).toContain('Page : /');
    expect(bug.corps).toMatch(/Version de MonEndo : \d+\.\d+\.\d+/);
    expect(bug.corps).toContain('Appareil : ');
    // Rien du compte ni du suivi.
    expect(bug.corps).not.toContain('utilisatrice@test.local');
  });

  test('ouverte directement, la page l\'indique', async ({ page }) => {
    await page.goto('/suggestions');

    const bug = lireMailto(await page.getByRole('link', { name: /Je signale un problème/ }).getAttribute('href'));

    expect(bug.corps).toContain('Page : ouverte directement');
  });

  test("l'adresse est affichée en clair et se copie", async ({ page, context }) => {
    await context.grantPermissions(['clipboard-read', 'clipboard-write']);
    await page.goto('/suggestions');

    await expect(page.getByText('clementoss@gmail.com')).toBeVisible();
    await page.getByRole('button', { name: 'Copier' }).click();

    await expect(page.locator('.text-sm', { hasText: 'Adresse copiée' })).toBeVisible();
    expect(await page.evaluate(() => navigator.clipboard.readText())).toBe('clementoss@gmail.com');
  });

  test('prévient que les captures peuvent montrer des données de santé', async ({ page }) => {
    await page.goto('/suggestions');

    await expect(page.getByText(/Tu choisis ce que tu m'envoies/)).toBeVisible();
  });
});
