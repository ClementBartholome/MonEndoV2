import type { CodeEmotion } from '@/features/bilan-quotidien/types/bilan-quotidien';
import type { PrisePrevue, TraitementAuBesoin } from '@/features/medicament/types/traitements';
import type { JourFrise } from '@/shared/types/frise';

/** Miroir de `AujourdhuiViewModel` (GET Accueil/aujourdhui) ; les listes arrivent sous `$values`, voir `enTableau`. */
export interface Aujourdhui {
    cycle: CycleAujourdhui;
    /** Null tant que le bilan du jour n'est pas rempli. */
    bilan: BilanAujourdhui | null;
    /** Prises prévues aujourd'hui, dans l'ordre des horaires. */
    prisesPrevues: PrisePrevue[];
    /** Traitements « au besoin » en cours. */
    auBesoin: TraitementAuBesoin[];
    semaine: Semaine;
    /** Épisode d'acné en cours ; null sans épisode en cours. */
    acne: AcneAujourdhui | null;
    /** Frise des 7 derniers jours, aujourd'hui compris. */
    frise: JourFrise[];
}

/** Miroir de `AcneAujourdhuiViewModel`. */
export interface AcneAujourdhui {
    /** Début de l'épisode (AAAA-MM-JJ). */
    debut: string;
    /** Durée jusqu'à aujourd'hui, jour de début compris. */
    jours: number;
    /** Jour de la dernière photo de suivi (AAAA-MM-JJ), null sans photo. */
    dernierePhoto: string | null;
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

export interface Semaine {
    joursAvecDouleur: number;
    joursAvecDouleurPendantRegles: number;
    fatigueMoyenne: number | null;
    fatigueMoyennePrecedente: number | null;
}
