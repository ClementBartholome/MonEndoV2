import type { Semaine } from '../types/aujourdhui';

/** Écart de fatigue moyenne (échelle 1 à 5) en dessous duquel on parle d'une fatigue stable. */
const ECART_FATIGUE_SIGNIFICATIF = 0.5;

/**
 * Faits de la semaine en phrases simples, descriptives et non anxiogènes : aucune interprétation médicale,
 * rien n'est dit quand il n'y a rien à dire.
 */
export function phrasesSemaine(semaine: Semaine): string[] {
    const phrases: string[] = [];

    if (semaine.joursAvecDouleur > 0) {
        const jours = semaine.joursAvecDouleur === 1 ? '1 jour' : `${semaine.joursAvecDouleur} jours`;
        const pendantRegles = semaine.joursAvecDouleurPendantRegles;
        const precision = pendantRegles === 0 ? ''
            : pendantRegles === semaine.joursAvecDouleur ? ', pendant les règles'
                : `, dont ${pendantRegles} pendant les règles`;
        phrases.push(`Douleur notée ${jours} sur 7${precision}.`);
    }

    const { fatigueMoyenne: actuelle, fatigueMoyennePrecedente: precedente } = semaine;
    if (actuelle !== null && precedente !== null) {
        const ecart = actuelle - precedente;
        phrases.push(Math.abs(ecart) < ECART_FATIGUE_SIGNIFICATIF
            ? 'Fatigue moyenne semblable à la semaine précédente.'
            : `Fatigue moyenne plus ${ecart < 0 ? 'basse' : 'haute'} que la semaine précédente.`);
    }

    return phrases;
}
