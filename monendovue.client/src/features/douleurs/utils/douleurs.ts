import { format, getDaysInMonth } from 'date-fns';
import { cleJour } from '@/shared/utils/jours';
import type { DonneesDouleur } from '../types/donnees-douleur';

/** Types de douleur proposés (valeurs enregistrées) et leur libellé court à l'écran. */
export const TYPES_DOULEUR: { valeur: string; libelle: string }[] = [
    { valeur: 'Douleur pelvienne', libelle: 'Pelvienne' },
    { valeur: 'Douleur abdominale', libelle: 'Abdominale' },
    { valeur: 'Douleur lombaire', libelle: 'Lombaire' },
    { valeur: 'Douleur thoracique', libelle: 'Thoracique' },
    { valeur: 'Douleur projetée', libelle: 'Projetée' },
    { valeur: 'Douleur neuropathique', libelle: 'Neuropathique' },
    { valeur: 'Dyspareunie', libelle: 'Dyspareunie' },
    { valeur: 'Autre', libelle: 'Autre' },
];

/** Ancien texte de remplacement enregistré quand aucun commentaire n'était saisi : jamais affiché. */
const COMMENTAIRE_VIDE = 'Pas de commentaire';

export function libelleCourt(type: string): string {
    return TYPES_DOULEUR.find((t) => t.valeur === type)?.libelle ?? type;
}

export function commentaireAffiche(commentaire?: string | null): string | null {
    const texte = commentaire?.trim();
    return texte && texte !== COMMENTAIRE_VIDE ? texte : null;
}

export interface ChiffresDuMois {
    joursAvecDouleur: number;
    /** Arrondie au dixième ; null sans entrée. */
    intensiteMoyenne: number | null;
    /** Libellé court du type le plus noté ; null sans entrée. */
    typeLePlusNote: string | null;
}

export function chiffresDuMois(entrees: DonneesDouleur[]): ChiffresDuMois {
    if (entrees.length === 0) return { joursAvecDouleur: 0, intensiteMoyenne: null, typeLePlusNote: null };

    const parType = new Map<string, number>();
    entrees.forEach((e) => parType.set(e.typeDouleur, (parType.get(e.typeDouleur) ?? 0) + 1));
    const [typeLePlusNote] = [...parType.entries()].sort((a, b) => b[1] - a[1])[0];
    const moyenne = entrees.reduce((total, e) => total + e.intensite, 0) / entrees.length;

    return {
        joursAvecDouleur: new Set(entrees.map((e) => cleJour(e.date))).size,
        intensiteMoyenne: Math.round(moyenne * 10) / 10,
        typeLePlusNote: libelleCourt(typeLePlusNote),
    };
}

export interface JourDuGraphique {
    jour: number;
    /** Intensité la plus forte notée ce jour (0 = rien noté). */
    intensiteMax: number;
    regles: boolean;
}

/** Un élément par jour du mois : intensité la plus forte notée et jours de règles. */
export function joursDuMois(mois: Date, entrees: DonneesDouleur[], joursDeRegles: string[]): JourDuGraphique[] {
    const reglesDuMois = new Set(joursDeRegles);
    return Array.from({ length: getDaysInMonth(mois) }, (_, index) => {
        const cle = format(new Date(mois.getFullYear(), mois.getMonth(), index + 1), 'yyyy-MM-dd');
        const duJour = entrees.filter((e) => cleJour(e.date) === cle);
        return {
            jour: index + 1,
            intensiteMax: duJour.reduce((max, e) => Math.max(max, e.intensite), 0),
            regles: reglesDuMois.has(cle),
        };
    });
}
