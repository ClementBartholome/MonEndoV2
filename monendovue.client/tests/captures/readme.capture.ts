import { format } from 'date-fns';
import type { Page, TestInfo } from '@playwright/test';
import { expect, test } from '../fixtures';
import { MAINTENANT } from '../mocks/session';
import { prisePrevue, simulerAccueil } from '../mocks/accueil';
import { simulerBilans } from '../mocks/bilan-quotidien';
import { periodeRealiste } from '../mocks/donnees-realistes';
import { simulerDouleurs } from '../mocks/douleurs';
import { simulerAcne, simulerRegles, simulerSymptomes } from '../mocks/cycle';
import { simulerTraitements } from '../mocks/traitements';
import { simulerActivites } from '../mocks/activite';
import { ECHO, GYNECO, KINE, PRECEDENT, simulerRendezVous } from '../mocks/agenda';
import { simulerSynthese } from '../mocks/synthese';

/**
 * Captures du README : `npm run captures` (depuis monendovue.client/) écrit les images dans docs/images/.
 * Données génériques et déterministes (mêmes jeux que les tests d'affichage) : aucune donnée personnelle.
 * Les noms de fichiers portent le format (`-mobile`, `-desktop`) ; chaque projet Playwright produit les siens.
 */
const SORTIE = '../docs/images';
const AUJOURDHUI = format(MAINTENANT, 'yyyy-MM-dd');
const DEBUT_REGLES = '2026-09-14';

/** Un cycle de 29 jours, jusqu'à aujourd'hui (les jours de règles à venir ne sont pas notés). */
const periode = periodeRealiste('2026-06-01', AUJOURDHUI, DEBUT_REGLES);
const joursRegles = periode.joursRegles.filter((j) => j <= AUJOURDHUI);

const CYCLES = [
  { debut: '2026-08-16', joursDeRegles: 5, duree: 29, joursDouleurForte: [1, 2, 3] },
  { debut: '2026-07-18', joursDeRegles: 5, duree: 29, joursDouleurForte: [1, 2] },
  { debut: '2026-06-19', joursDeRegles: 4, duree: 29, joursDouleurForte: [2] },
  { debut: '2026-05-21', joursDeRegles: 5, duree: 29, joursDouleurForte: [1, 2, 4] },
];

const DOULEURS = periode.bilans
  .filter((b) => b.jour >= '2026-09-01' && (b.douleurMoyenne ?? 0) >= 3)
  .map((b, i) => ({
    typeDouleur: i % 3 === 0 ? 'Douleur lombaire' : 'Douleur pelvienne',
    intensite: b.douleurMoyenne ?? 3,
    date: `${b.jour}T${i % 2 ? '08:30' : '19:00'}:00`,
    commentaire: null,
  }));

const formatDe = (info: TestInfo) => info.project.name;

/** Le README montre surtout le mobile (l'application est pensée mobile d'abord) ; deux écrans seulement en desktop. */
const EN_DESKTOP = new Set(['accueil', 'bilan-tendances']);

async function capturer(page: Page, nom: string, info: TestInfo) {
  if (formatDe(info) === 'desktop' && !EN_DESKTOP.has(nom)) return;
  // Fin des animations et des chargements avant la capture.
  await page.waitForLoadState('networkidle');
  // Sans le focus d'un champ ni le défilement qu'il provoque.
  await page.evaluate(() => { (document.activeElement as HTMLElement | null)?.blur(); window.scrollTo(0, 0); });
  await page.waitForTimeout(600);
  await page.screenshot({ path: `${SORTIE}/${nom}-${formatDe(info)}.png` });
}

test.describe('Captures du README', () => {
  test.beforeEach(async ({ serveur }) => {
    simulerRendezVous(serveur, [KINE, ECHO, GYNECO], { precedent: PRECEDENT });
    simulerBilans(serveur, { bilans: periode.bilans, joursRegles });
    simulerRegles(serveur, joursRegles, {
      enCours: { debut: DEBUT_REGLES, jourDuCycle: 2, jourDeRegles: 2 },
      cycles: CYCLES,
      dureeMoyenne: 29, reglesMoyenne: 5, dureeMinimale: 29, dureeMaximale: 29,
    });
    simulerDouleurs(serveur, DOULEURS, joursRegles);
    simulerSymptomes(serveur, [
      { typeSymptome: 'Fatigue', date: '2026-09-14T10:00:00', intensite: 6 },
      { typeSymptome: 'Nausée', date: '2026-09-14T12:00:00', intensite: 4 },
      { typeSymptome: 'Ballonnements', date: '2026-09-13T20:00:00', intensite: 5 },
    ]);
    simulerAcne(serveur, []);
    simulerActivites(serveur, [
      { type: 'Yoga', date: '2026-09-14T18:30:00', duree: 40, niveau: 'Douce', effet: 'Soulagee' },
      { type: 'Marche', date: '2026-09-12T11:00:00', duree: 45, niveau: 'Moderee', effet: 'Pareille' },
      { type: 'Natation', date: '2026-09-09T19:00:00', duree: 30, niveau: 'Soutenue', effet: 'NonRenseigne' },
    ]);
    simulerTraitements(serveur, [
      { nom: 'Diénogest 2 mg', dose: '1 comprimé', horaires: ['08:00'] },
      { nom: 'Vitamine D', dose: '1 ampoule', frequence: 'TousLesNJours', intervalleJours: 14, horaires: ['20:00'] },
      { nom: 'Ibuprofène', dose: '400 mg', frequence: 'AuBesoin', horaires: [] },
      { nom: 'Kinésithérapie', type: 'NonMedicamenteux', frequence: 'AuBesoin', horaires: [], dose: '' },
    ]);
    simulerSynthese(serveur);
    simulerAccueil(serveur, {
      cycle: { enRegles: true, jourDeRegles: 2, jourDuCycle: 2 },
      bilan: { douleurMoyenne: 4, emotions: ['Calme', 'Soulagement'], fatigue: 3 },
      prisesPrevues: [
        prisePrevue({ traitementId: 1, nom: 'Diénogest 2 mg', heurePrevue: '08:00', reponse: { priseId: 3, statut: 'Pris', date: `${AUJOURDHUI}T08:05:00` } }),
        prisePrevue({ traitementId: 2, nom: 'Vitamine D', dose: '1 ampoule', heurePrevue: '20:00' }),
      ],
      auBesoin: [{ id: 3, nom: 'Ibuprofène', dose: '400 mg', dernierePrise: null }],
      semaine: { joursAvecDouleur: 5, joursAvecDouleurPendantRegles: 2, fatigueMoyenne: 2.5, fatigueMoyennePrecedente: 3.5 },
    });
  });

  test('accueil', async ({ page }, info) => {
    await page.goto('/');
    await expect(page.getByRole('heading', { level: 1 })).toHaveText('Mardi 15 septembre');
    await capturer(page, 'accueil', info);
  });

  test('bilan : historique et tendances', async ({ page, bilanQuotidienPage: bilan }, info) => {
    await bilan.ouvrir();
    await expect(bilan.calendrier).toBeVisible();
    await capturer(page, 'bilan-historique', info);

    await bilan.afficherOnglet('Tendances');
    await capturer(page, 'bilan-tendances', info);
  });

  test('bilan : saisie', async ({ page, bilanQuotidienPage: bilan, serveur }, info) => {
    // Bilan du jour pas encore fait : la saisie s'ouvre.
    simulerBilans(serveur, { bilans: periode.bilans.filter((b) => b.jour !== AUJOURDHUI), joursRegles });
    await bilan.ouvrirSaisie();
    await bilan.remplirEssentiel(5, 'Calme', 'Soulagement');
    await bilan.choisirNiveau('Fatigue', 3);
    await capturer(page, 'bilan-saisie', info);
  });

  test('douleurs', async ({ page }, info) => {
    await page.goto('/douleurs');
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    await capturer(page, 'douleurs', info);
  });

  test('cycle', async ({ page }, info) => {
    await page.goto('/cycle');
    await expect(page.getByText('Règles · jour 2').first()).toBeVisible();
    await capturer(page, 'cycle', info);
  });

  test('traitements', async ({ page }, info) => {
    await page.goto('/medicaments');
    await expect(page.getByText('Diénogest 2 mg').first()).toBeVisible();
    await capturer(page, 'traitements', info);
  });

  test('activité', async ({ page }, info) => {
    await page.goto('/activite');
    await expect(page.getByText('Yoga').first()).toBeVisible();
    await capturer(page, 'activite', info);
  });

  test('agenda', async ({ page }, info) => {
    await page.goto('/agenda');
    await expect(page.getByText('Kinésithérapie').first()).toBeVisible();
    await capturer(page, 'agenda', info);
  });

  test('préparer un rendez-vous', async ({ page, rendezVousPage }, info) => {
    await rendezVousPage.ouvrir();
    await rendezVousPage.questions.fill('Les douleurs pendant les règles augmentent-elles ?');
    await capturer(page, 'rendez-vous', info);
  });

  test.describe('sans session', () => {
    test.use({ connectee: false });

    test('bienvenue', async ({ page }, info) => {
      await page.goto('/bienvenue');
      await capturer(page, 'bienvenue', info);
    });
  });
});
