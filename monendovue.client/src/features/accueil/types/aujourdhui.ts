import type { CodeEmotion } from '@/features/bilan-quotidien/types/bilan-quotidien';

/** Miroir de `AujourdhuiViewModel` (GET Accueil/aujourdhui) ; les listes arrivent sous `$values`, voir `enTableau`. */
export interface Aujourdhui {
    cycle: CycleAujourdhui;
    /** Null tant que le bilan du jour n'est pas rempli. */
    bilan: BilanAujourdhui | null;
    traitements: TraitementAujourdhui[];
    semaine: Semaine;
}

export interface CycleAujourdhui {
    enRegles: boolean;
    jourDeRegles: number | null;
    jourDuCycle: number | null;
}

export interface BilanAujourdhui {
    douleurMoyenne: number;
    emotions: CodeEmotion[];
    fatigue: number | null;
}

export interface TraitementAujourdhui {
    id: number;
    nom: string;
    posologie: string | null;
    prisesDuJour: number;
    /** Date locale sans fuseau (AAAA-MM-JJTHH:mm:ss). */
    dernierePrise: string | null;
}

export interface Semaine {
    joursAvecDouleur: number;
    joursAvecDouleurPendantRegles: number;
    fatigueMoyenne: number | null;
    fatigueMoyennePrecedente: number | null;
}
