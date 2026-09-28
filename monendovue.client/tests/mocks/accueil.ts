import type { Aujourdhui, TraitementAujourdhui } from '../../src/features/accueil/types/aujourdhui';
import type { FauxServeur } from './faux-serveur';

/** Accueil sans aucune donnée : ni règles, ni bilan, ni traitement, rien à dire sur la semaine. */
export const ACCUEIL_VIDE: Aujourdhui = {
  cycle: { enRegles: false, jourDeRegles: null, jourDuCycle: null },
  bilan: null,
  traitements: [],
  semaine: { joursAvecDouleur: 0, joursAvecDouleurPendantRegles: 0, fatigueMoyenne: null, fatigueMoyennePrecedente: null },
};

/**
 * Routes simulées de l'accueil (`AccueilController`) et de la prise d'un traitement (`DonneesMedicamentController`) :
 * les traitements arrivent sous `$values`, comme une liste C#.
 */
export function simulerAccueil(serveur: FauxServeur, accueil: Partial<Aujourdhui>) {
  const { traitements = [], ...reste } = accueil;
  serveur
    .on('GET', /^Accueil\/aujourdhui$/, () => ({ body: { ...ACCUEIL_VIDE, ...reste, traitements: { $values: traitements } } }))
    .on('POST', /^DonneesMedicament$/, ({ corps }) => ({ status: 201, body: { id: 99, ...corps } }));
}

export function traitement(t: Partial<TraitementAujourdhui>): TraitementAujourdhui {
  return { id: 1, nom: 'Diénogest 2 mg', posologie: '1 comprimé le matin', prisesDuJour: 0, dernierePrise: null, ...t };
}
