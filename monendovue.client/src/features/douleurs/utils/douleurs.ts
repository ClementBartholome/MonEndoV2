import { format, getDaysInMonth } from 'date-fns';
import { cleJour } from '@/shared/utils/jours';
import type { DonneesDouleur } from '../types/donnees-douleur';
import type { JourFrise } from '@/shared/types/frise';

/** Types de douleur proposés (valeurs enregistrées) et leur libellé court à l'écran. */
export const TYPES_DOULEUR: { valeur: string; libelle: string }[] = [
    { valeur: 'Douleur pelvienne', libelle: 'Pelvienne' },
    { valeur: 'Douleur abdominale', libelle: 'Abdominale' },
    { valeur: 'Douleur lombaire', libelle: 'Lombaire' },
    { valeur: 'Douleur thoracique', libelle: 'Thoracique' },
    { valeur: 'Douleur projetée', libelle: 'Projetée' },
    { valeur: 'Douleur neuropathique', libelle: 'Neuropathique' },
    // Valeur enregistrée inchangée ; libellé courant plutôt que le terme médical.
    { valeur: 'Dyspareunie', libelle: 'Pendant un rapport' },
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

export interface DouleurParType {
    libelle: string;
    /** Jours où ce type a été noté. */
    jours: number;
    /** Intensité moyenne, arrondie au dixième. */
    moyenne: number;
    max: number;
}

/** Un élément par type noté (bilans et crises), du plus fréquent au moins fréquent. */
export function douleursParType(entrees: DonneesDouleur[]): DouleurParType[] {
    const parType = new Map<string, DonneesDouleur[]>();
    entrees.forEach((e) => parType.set(e.typeDouleur, [...(parType.get(e.typeDouleur) ?? []), e]));
    return [...parType.entries()]
        .map(([type, duType]) => ({
            libelle: libelleCourt(type),
            jours: new Set(duType.map((e) => cleJour(e.date))).size,
            moyenne: Math.round((duType.reduce((total, e) => total + e.intensite, 0) / duType.length) * 10) / 10,
            max: Math.max(...duType.map((e) => e.intensite)),
        }))
        .sort((a, b) => b.jours - a.jours || b.max - a.max);
}

/** Frise du mois, un jour par trait : douleur la plus forte notée (null si rien) et jours de règles. */
export function friseDuMois(mois: Date, entrees: DonneesDouleur[], joursDeRegles: string[]): JourFrise[] {
    const reglesDuMois = new Set(joursDeRegles);
    return Array.from({ length: getDaysInMonth(mois) }, (_, index) => {
        const jour = format(new Date(mois.getFullYear(), mois.getMonth(), index + 1), 'yyyy-MM-dd');
        const duJour = entrees.filter((e) => cleJour(e.date) === jour);
        return {
            jour,
            douleur: duJour.length ? Math.max(...duJour.map((e) => e.intensite)) : null,
            regles: reglesDuMois.has(jour),
        };
    });
}
