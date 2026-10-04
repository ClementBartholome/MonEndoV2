import type { FluxRegles } from '../types/cycle';

export interface PresentationFlux {
    valeur: FluxRegles;
    libelle: string;
    /** Points dans le calendrier : 0 = traces (un point vide), 1 léger, 2 moyen, 3 abondant. */
    points: 0 | 1 | 2 | 3;
}

/** Du plus léger au plus abondant ; les valeurs sont celles de l'énumération C# `FluxRegles`. */
export const niveauxDeFlux: PresentationFlux[] = [
    { valeur: 'Traces', libelle: 'Traces', points: 0 },
    { valeur: 'Leger', libelle: 'Léger', points: 1 },
    { valeur: 'Moyen', libelle: 'Moyen', points: 2 },
    { valeur: 'Abondant', libelle: 'Abondant', points: 3 },
];

export const libelleDeFlux = (flux: FluxRegles): string => niveauxDeFlux.find((n) => n.valeur === flux)?.libelle.toLowerCase() ?? '';

/**
 * Repère de protections par jour du carnet de suivi des HUG (Genève) ; chaque personne en reste juge.
 * Source dans `.claude/skills/endometriose/sources.md`.
 */
export const reperesDeProtections = 'Repère du carnet de suivi des HUG : léger, 1 à 3 protections par jour ; moyen, 4 à 6 ; abondant, plus de 6.';
