import AxeBuilder from '@axe-core/playwright';
import type { Page } from '@playwright/test';
import { test, expect } from './fixtures';
import type { FauxServeur } from './mocks/faux-serveur';
import { simulerAccueil, prisePrevue } from './mocks/accueil';
import { simulerBilans } from './mocks/bilan-quotidien';
import { periodeRealiste } from './mocks/donnees-realistes';
import { simulerAcne, simulerRegles, simulerSymptomes } from './mocks/cycle';
import { simulerDouleurs } from './mocks/douleurs';
import { simulerTraitements } from './mocks/traitements';
import { simulerActivites } from './mocks/activite';
import { simulerTransit } from './mocks/transit';
import { ECHO, ECHO_PASSEE, GYNECO, KINE, simulerRendezVous } from './mocks/agenda';
import { simulerSynthese } from './mocks/synthese';
import { simulerParametresSansNotifications } from './mocks/parametres';
import { simulerConsentement } from './mocks/consentement';

/**
 * Accessibilité (WCAG 2.2 A et AA, bonnes pratiques d'axe) de chaque écran, avec des données réalistes et dans ses états ouverts (panneaux de saisie,
 * menu « Plus ») : axe-core ne voit que ce qui est affiché. Une violation fait échouer le test avec la règle, l'élément et
 * l'aide d'axe, pour corriger sans deviner.
 */
// WCAG 2.2 AA (taille des cibles tactiles comprise) et bonnes pratiques d'axe (ordre des titres, zones de la page).
const NORMES = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa', 'best-practice'];

// Animations réduites (réglage d'accessibilité du téléphone, respecté par l'app) : le fondu d'ouverture des pages ferait
// lire à axe des couleurs à mi-transparence, donc des contrastes faux.
test.use({ contextOptions: { reducedMotion: 'reduce' } });

async function verifier(page: Page, ecran: string) {
  // Les animations d'ouverture (panneaux, toasts) font lire des couleurs à mi-transition : on attend qu'elles finissent.
  await page.waitForFunction(() => Array.from(document.querySelectorAll('main, [role=dialog]')).every((el) => getComputedStyle(el).opacity === '1'));
  await page.waitForTimeout(400);
  const { violations } = await new AxeBuilder({ page }).withTags(NORMES).analyze();
  const lisibles = violations.map((v) => ({
    regle: v.id,
    impact: v.impact,
    aide: v.help,
    elements: v.nodes.slice(0, 5).map((n) => `${n.target.join(' ')} — ${n.failureSummary?.split('\n').slice(1).join(' ').trim()}`),
  }));
  expect(lisibles, `violations d'accessibilité sur « ${ecran} »`).toEqual([]);
}

/** Un compte bien rempli : un mois et demi de bilans, des règles, des douleurs, des traitements, un agenda. */
function donneesRealistes(serveur: FauxServeur) {
  const realiste = periodeRealiste('2026-08-01', '2026-09-15', '2026-08-16');
  simulerAccueil(serveur, {
    cycle: { enRegles: true, jourDeRegles: 2, jourDuCycle: 2 },
    bilan: { douleurMoyenne: 4, emotions: ['Calme', 'Soulagement'], fatigue: 3 },
    prisesPrevues: [prisePrevue({ traitementId: 1, nom: 'Diénogest 2 mg', heurePrevue: '08:00' })],
    auBesoin: [{ id: 3, nom: 'Ibuprofène', dose: '400 mg', dernierePrise: null }],
    semaine: { joursAvecDouleur: 4, joursAvecDouleurPendantRegles: 2, fatigueMoyenne: 2.5, fatigueMoyennePrecedente: 3.5 },
    acne: { debut: '2026-08-30', jours: 17, dernierePhoto: '2026-09-07' },
    frise: [2, null, 5, 7, 4, 3, 6].map((douleur, i) => ({ jour: `2026-09-${String(9 + i).padStart(2, '0')}`, douleur, regles: i >= 5 })),
  });
  simulerBilans(serveur, { ...realiste, bilans: realiste.bilans.filter((b) => b.jour !== '2026-09-15') });
  simulerRegles(serveur, ['2026-09-14', '2026-09-15'], {
    enCours: { debut: '2026-09-14', jourDuCycle: 2, jourDeRegles: 2 },
    cycles: [
      { debut: '2026-08-14', joursDeRegles: 5, duree: 31, joursDouleurForte: [1, 2, 16] },
      { debut: '2026-07-14', joursDeRegles: 4, duree: 31, joursDouleurForte: [] },
    ],
    dureeMoyenne: 31, reglesMoyenne: 5, dureeMinimale: 31, dureeMaximale: 31,
  });
  simulerSymptomes(serveur, [{ typeSymptome: 'Nausée', date: '2026-09-12T09:00:00' }]);
  simulerAcne(serveur, [{ debut: '2026-06-03', fin: '2026-07-20' }, { debut: '2026-08-30' }]);
  simulerDouleurs(serveur, realiste.bilans.slice(-20).map((b, i) => ({
    typeDouleur: 'Douleur pelvienne', intensite: (i * 3) % 9 + 1, date: `${b.jour}T09:00:00`,
  })), realiste.joursRegles);
  simulerTraitements(serveur, [{ nom: 'Diénogest 2 mg', horaires: ['08:00'] }, { nom: 'Vitamine D', dose: null, horaires: ['20:30'] }]);
  simulerActivites(serveur, [{ date: '2026-09-14T18:00:00' }, { date: '2026-09-10T08:00:00' }]);
  simulerTransit(serveur);
  simulerRendezVous(serveur, [KINE, ECHO, GYNECO, ECHO_PASSEE]);
  simulerSynthese(serveur);
  simulerParametresSansNotifications(serveur);
}

test.describe('Accessibilité', () => {
  test.beforeEach(({ serveur }) => donneesRealistes(serveur));

  const pages: [string, string][] = [
    ['accueil', '/'],
    ['bilan (historique)', '/bilan-quotidien'],
    ['bilan (tendances)', '/bilan-quotidien?onglet=analyse'],
    ['douleurs', '/douleurs'],
    ['cycle (règles)', '/cycle'],
    ['cycle (symptômes)', '/cycle?onglet=symptomes'],
    ['acné', '/acne'],
    ['traitements', '/medicaments'],
    ['activité', '/activite'],
    ['transit', '/transit'],
    ['agenda', '/agenda'],
    ['préparer un rendez-vous', '/export'],
    ['s\'informer', '/s-informer'],
    ['suggestion ou bug', '/suggestions'],
    ['paramètres', '/parametres'],
  ];
  for (const [ecran, adresse] of pages) {
    test(ecran, async ({ page }) => {
      await page.goto(adresse);
      // Premier écran de la série : Vite compile encore, d'où un délai plus long.
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible({ timeout: 15_000 });
      await verifier(page, ecran);
    });
  }

  test('saisie du bilan', async ({ page }) => {
    await page.goto('/bilan-quotidien?ajouter');
    await expect(page.getByRole('heading', { name: 'Bilan d\x27aujourd\x27hui' })).toBeVisible({ timeout: 15_000 });
    await verifier(page, 'saisie du bilan');
  });

  test('saisie d\'une douleur', async ({ page }) => {
    await page.goto('/douleurs?ajouter');
    await expect(page.getByRole('dialog')).toBeVisible();
    await verifier(page, 'saisie d\'une douleur');
  });

  test('saisie d\'un symptôme', async ({ page }) => {
    await page.goto('/cycle?onglet=symptomes&ajouter');
    await expect(page.getByRole('dialog')).toBeVisible();
    await verifier(page, 'saisie d\'un symptôme');
  });

  test('menu « Plus » et vue Passés de l\'agenda', async ({ page, navigationPage }) => {
    await page.goto('/agenda');
    await page.getByRole('button', { name: 'Passés' }).click();
    await verifier(page, 'agenda (passés)');
    if (navigationPage.estMobile) {
      await navigationPage.principale.getByRole('button', { name: 'Plus' }).click();
      await expect(navigationPage.menuPlus).toBeVisible();
      await verifier(page, 'menu « Plus »');
    }
  });
});

test.describe('Accessibilité sans session', () => {
  test.use({ connectee: false });
  test.beforeEach(({ serveur }) => simulerConsentement(serveur));

  const pages: [string, string][] = [
    ['bienvenue', '/bienvenue'],
    ['connexion', '/login'],
    ['inscription', '/register'],
    ['mot de passe oublié', '/mot-de-passe-oublie'],
    ['politique de confidentialité', '/confidentialite'],
    ['mentions légales', '/mentions-legales'],
  ];
  for (const [ecran, adresse] of pages) {
    test(ecran, async ({ page }) => {
      await page.goto(adresse);
      // Premier écran de la série : Vite compile encore, d'où un délai plus long.
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible({ timeout: 15_000 });
      await verifier(page, ecran);
    });
  }
});

test.describe('Accessibilité · recadrage et clavier', () => {
  test.beforeEach(({ serveur }) => donneesRealistes(serveur));

  // WCAG 1.4.10 : à 320 px de large (zoom à 400 %), rien ne demande de défiler horizontalement.
  test('à 320 px, aucune page ne défile horizontalement', async ({ page }) => {
    test.skip(test.info().project.name !== 'mobile', 'vérifié une fois, en largeur de téléphone');
    await page.setViewportSize({ width: 320, height: 640 });
    const debordements: string[] = [];
    for (const adresse of ['/', '/bilan-quotidien', '/bilan-quotidien?onglet=analyse', '/douleurs', '/cycle', '/acne', '/medicaments', '/agenda', '/export', '/parametres']) {
      await page.goto(adresse);
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible({ timeout: 15_000 });
      await page.waitForTimeout(300);
      const largeur = await page.evaluate(() => document.documentElement.scrollWidth);
      if (largeur > 320) debordements.push(`${adresse} : ${largeur} px`);
    }
    expect(debordements).toEqual([]);
  });

  test('une saisie s\'ouvre et se ferme au clavier, et le focus revient au bouton', async ({ page }) => {
    await page.goto('/douleurs');
    const bouton = page.getByRole('button', { name: 'Noter une crise' }).and(page.locator(':visible')).first();
    await bouton.focus();
    await page.keyboard.press('Enter');

    const panneau = page.getByRole('dialog');
    await expect(panneau).toBeVisible();
    await expect.poll(() => panneau.evaluate((el) => el.contains(document.activeElement))).toBe(true);
    // Le focus reste dans le panneau tant qu'il est ouvert.
    for (let i = 0; i < 15; i++) await page.keyboard.press('Tab');
    expect(await panneau.evaluate((el) => el.contains(document.activeElement))).toBe(true);

    await page.keyboard.press('Escape');
    await expect(panneau).toBeHidden();
    await expect(bouton).toBeFocused();
  });
});
