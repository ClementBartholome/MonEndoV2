/**
 * Teinte de chaque indicateur du bilan (tokens `--teinte-*`) : fond clair et couleur du texte ou de l'icône, pour que la
 * même mesure garde la même couleur partout (résumé du jour, tendances, repères). Classes écrites en entier pour Tailwind.
 */
export interface Teinte {
    fond: string;
    texte: string;
}

const TEINTES = {
    douleur: { fond: 'bg-teinte-douleur-fond', texte: 'text-teinte-douleur' },
    bilan: { fond: 'bg-teinte-bilan-fond', texte: 'text-teinte-bilan' },
    symptome: { fond: 'bg-teinte-symptome-fond', texte: 'text-teinte-symptome' },
    traitement: { fond: 'bg-teinte-traitement-fond', texte: 'text-teinte-traitement' },
    regles: { fond: 'bg-teinte-regles-fond', texte: 'text-teinte-regles' },
    neutre: { fond: 'bg-teinte-neutre-fond', texte: 'text-teinte-neutre' },
} satisfies Record<string, Teinte>;

export const TEINTE_INDICATEUR: Record<string, Teinte> = {
    douleur: TEINTES.douleur,
    fatigue: TEINTES.bilan,
    stress: TEINTES.neutre,
    emotions: TEINTES.symptome,
    pas: TEINTES.traitement,
    hydratation: TEINTES.bilan,
};

/** Teintes des blocs de l'onglet Tendances. */
export const TEINTE_BLOC = {
    tendances: TEINTES.bilan,
    reperes: TEINTES.traitement,
    cycle: TEINTES.regles,
};

export const teinteDe = (cle: string): Teinte => TEINTE_INDICATEUR[cle] ?? TEINTES.neutre;
