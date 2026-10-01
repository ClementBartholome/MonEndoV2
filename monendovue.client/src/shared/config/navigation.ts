/** Rubriques de l'application : une seule liste pour la barre du bas, le menu « Plus » et la barre latérale. */

/** Teintes de rubrique (tokens teinte-*) : classes complètes, lisibles par Tailwind. */
export const teintesRubrique = {
    bilan: 'bg-teinte-bilan-fond text-teinte-bilan',
    symptome: 'bg-teinte-symptome-fond text-teinte-symptome',
    traitement: 'bg-teinte-traitement-fond text-teinte-traitement',
    douleur: 'bg-teinte-douleur-fond text-teinte-douleur',
    regles: 'bg-teinte-regles-fond text-teinte-regles',
    neutre: 'bg-teinte-neutre-fond text-teinte-neutre',
} as const;

export interface EntreeNavigation {
    libelle: string;
    icone: string;
    vers: string;
    /** Sous-titre affiché dans le menu « Plus ». */
    detail?: string;
    /** Tuile de l'icône dans le menu « Plus ». */
    teinte?: keyof typeof teintesRubrique;
}

/** Barre du bas (mobile) et haut de la barre latérale : les suivis du quotidien. */
export const navigationPrincipale: EntreeNavigation[] = [
    { libelle: 'Accueil', icone: 'clinical_notes', vers: '/' },
    { libelle: 'Bilan', icone: 'event_note', vers: '/bilan-quotidien' },
    { libelle: 'Douleurs', icone: 'sick', vers: '/douleurs' },
    { libelle: 'Cycle', icone: 'menstrual_health', vers: '/cycle' },
];

/** Menu « Plus » (mobile) et suite de la barre latérale. */
export const navigationSecondaire: EntreeNavigation[] = [
    { libelle: 'Traitements', icone: 'pill', vers: '/medicaments', detail: 'Prises et séances', teinte: 'traitement' },
    { libelle: 'Activité', icone: 'directions_run', vers: '/activite', detail: 'Séances et types d\'activité', teinte: 'bilan' },
    { libelle: 'Préparer un rendez-vous', icone: 'picture_as_pdf', vers: '/export', detail: 'Synthèse PDF pour la consultation', teinte: 'neutre' },
    { libelle: 'Transit', icone: 'gastroenterology', vers: '/transit', detail: 'Ancien suivi, en lecture', teinte: 'neutre' },
    { libelle: 'Suggestion ou bug', icone: 'feedback', vers: '/suggestions', detail: 'Écrire à l\'éditeur', teinte: 'symptome' },
];

export const navigationCompte: EntreeNavigation = { libelle: 'Paramètres', icone: 'settings', vers: '/parametres' };

/** Une entrée est active sur sa page et ses sous-pages ; l'accueil seulement sur « / ». */
export function estActive(entree: EntreeNavigation, chemin: string): boolean {
    return entree.vers === '/' ? chemin === '/' : chemin === entree.vers || chemin.startsWith(`${entree.vers}/`);
}
