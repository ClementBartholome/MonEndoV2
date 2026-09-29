import type { Aujourdhui } from '../../src/features/accueil/types/aujourdhui';
import type { PrisePrevue } from '../../src/features/medicament/types/traitements';
import { liste, type FauxServeur } from './faux-serveur';

/** Accueil sans aucune donnée : ni règles, ni bilan, ni traitement, rien à dire sur la semaine. */
export const ACCUEIL_VIDE: Aujourdhui = {
  cycle: { enRegles: false, jourDeRegles: null, jourDuCycle: null },
  bilan: null,
  prisesPrevues: [],
  auBesoin: [],
  semaine: { joursAvecDouleur: 0, joursAvecDouleurPendantRegles: 0, fatigueMoyenne: null, fatigueMoyennePrecedente: null },
};

/**
 * Routes simulées de l'accueil (`AccueilController`) et de la réponse à une prise (`TraitementsController`) :
 * les listes arrivent sous `$values`, comme une liste C#.
 */
export function simulerAccueil(serveur: FauxServeur, accueil: Partial<Aujourdhui>) {
  const { prisesPrevues = [], auBesoin = [], ...reste } = accueil;
  serveur
    .on('GET', /^Accueil\/aujourdhui$/, () => ({
      body: { ...ACCUEIL_VIDE, ...reste, prisesPrevues: liste(prisesPrevues), auBesoin: liste(auBesoin) },
    }))
    .on('POST', /^Traitements\/\d+\/prises$/, () => ({ body: { id: 99 } }));
}

export function prisePrevue(p: Partial<PrisePrevue>): PrisePrevue {
  return { traitementId: 1, nom: 'Diénogest 2 mg', dose: '1 comprimé', heurePrevue: '08:00', reponse: null, ...p };
}
