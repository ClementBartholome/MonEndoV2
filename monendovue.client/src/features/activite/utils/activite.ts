import { cleJour } from '@/shared/utils/jours';
import type { Activite, EffetActivite, NiveauActivite } from '../types/activite';

/** Types proposés en un tap ; « Autre » ouvre un champ libre (les anciens types libres restent affichés tels quels). */
export const TYPES_ACTIVITE: { valeur: string; icone: string }[] = [
    { valeur: 'Marche', icone: 'directions_walk' },
    { valeur: 'Yoga', icone: 'self_improvement' },
    { valeur: 'Natation', icone: 'pool' },
    { valeur: 'Vélo', icone: 'directions_bike' },
    { valeur: 'Étirements', icone: 'accessibility_new' },
];

export const DUREES = [15, 30, 45, 60];

export const NIVEAUX: { valeur: NiveauActivite; libelle: string }[] = [
    { valeur: 'Douce', libelle: 'Douce' },
    { valeur: 'Moderee', libelle: 'Modérée' },
    { valeur: 'Soutenue', libelle: 'Soutenue' },
];

export const EFFETS: { valeur: Exclude<EffetActivite, 'NonRenseigne'>; libelle: string }[] = [
    { valeur: 'Soulagee', libelle: 'Soulagée' },
    { valeur: 'Pareille', libelle: 'Pareille' },
    { valeur: 'PlusForte', libelle: 'Plus forte' },
];

export function iconeActivite(type: string): string {
    return TYPES_ACTIVITE.find((t) => t.valeur === type)?.icone ?? 'directions_run';
}

export const libelleNiveau = (niveau: NiveauActivite) => NIVEAUX.find((n) => n.valeur === niveau)?.libelle ?? niveau;

/** Effet sur la douleur en toutes lettres ; null s'il n'a pas été renseigné. */
export function libelleEffet(effet: EffetActivite): string | null {
    const libelles: Record<EffetActivite, string | null> = {
        NonRenseigne: null, Soulagee: 'douleur soulagée', Pareille: 'douleur inchangée', PlusForte: 'douleur plus forte',
    };
    return libelles[effet];
}

export function duree(minutes: number): string {
    if (minutes < 60) return `${minutes} min`;
    const heures = Math.floor(minutes / 60);
    const reste = minutes % 60;
    return reste ? `${heures} h ${String(reste).padStart(2, '0')}` : `${heures} h`;
}

export interface ChiffresActivite {
    seances: number;
    minutes: number;
    /** Séances après lesquelles la douleur a été notée soulagée. */
    soulagee: number;
    /** Séances dont l'effet sur la douleur a été renseigné. */
    effetRenseigne: number;
    jours: number;
}

export function chiffresDuMois(activites: Activite[]): ChiffresActivite {
    return {
        seances: activites.length,
        minutes: activites.reduce((total, a) => total + a.duree, 0),
        soulagee: activites.filter((a) => a.effet === 'Soulagee').length,
        effetRenseigne: activites.filter((a) => a.effet !== 'NonRenseigne').length,
        jours: new Set(activites.map((a) => cleJour(a.date))).size,
    };
}
