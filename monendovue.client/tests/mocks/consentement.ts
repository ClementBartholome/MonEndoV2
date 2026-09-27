import type { Page } from '@playwright/test';
import { CARNET_ID, UTILISATRICE } from './session';
import type { FauxServeur } from './faux-serveur';

const JETON_LOINTAIN = new Date(2099, 0, 1).toISOString();

/** Routes simulées du consentement (`AccountController`) : inscription et accord d'un compte existant. */
export function simulerConsentement(serveur: FauxServeur) {
  serveur
    .on('POST', /^Account\/register$/, ({ corps }) => (corps?.consentementDonneesSante
      ? { body: { userName: corps.email, carnetSanteId: CARNET_ID, tokenExpiry: JETON_LOINTAIN, consentementAJour: true } }
      : { status: 400, body: { $values: ['Ton accord est nécessaire pour créer un compte.'] } }))
    .on('POST', /^Account\/consentement$/, () => ({ body: { tokenExpiry: JETON_LOINTAIN, consentementAJour: true } }));
}

/** Refus de l'API pour un compte sans consentement à jour (`ExigeConsentementFilter`). */
export const REFUS_CONSENTEMENT = { status: 403, body: { code: 'consentement-requis', message: 'Accord nécessaire.' } };

/** Session ouverte par un compte existant : sans consentement enregistré, ou d'avant la 1.3.0 (champ absent). */
export async function ouvrirSessionSansConsentement(page: Page, champ: 'refuse' | 'absent') {
  const utilisatrice = champ === 'refuse' ? { ...UTILISATRICE, consentementAJour: false } : UTILISATRICE;
  await page.addInitScript((u) => localStorage.setItem('user', JSON.stringify(u)), utilisatrice);
}
