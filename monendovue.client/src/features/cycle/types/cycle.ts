/** Miroir de `CycleViewModel` (GET Cycle?jour=…&mois=…) : dates « AAAA-MM-JJ ». */
/** Miroir de l'énumération C# `FluxRegles`. */
export type FluxRegles = 'Traces' | 'Leger' | 'Moyen' | 'Abondant';

/** Détails facultatifs d'un jour de règles : null = non précisé (jamais compté comme un niveau ni comme « non »). */
export interface DetailJourRegles {
    /** AAAA-MM-JJ */
    jour: string;
    flux: FluxRegles | null;
    caillots: boolean | null;
}

export interface CycleDuMois {
    joursDeRegles: string[];
    /** Jours du mois dont le flux ou les caillots sont précisés. */
    detailsJours: DetailJourRegles[];
    enCours: CycleEnCours | null;
    cycles: CycleTermine[];
    /** Moyenne des 6 derniers cycles ; null avant deux cycles terminés. */
    dureeMoyenne: number | null;
    /** Durée moyenne des règles des 6 derniers cycles ; null avant deux cycles. */
    reglesMoyenne: number | null;
    /** Cycle le plus court et le plus long des 6 derniers ; null avant deux cycles. */
    dureeMinimale: number | null;
    dureeMaximale: number | null;
    /** Cycles plus anciens que ceux listés (« Voir plus »). */
    cyclesPlusAnciens: number;
}

export interface CycleEnCours {
    debut: string;
    jourDuCycle: number;
    /** Jour de règles si aujourd'hui est noté, sinon null. */
    jourDeRegles: number | null;
}

export interface CycleTermine {
    debut: string;
    joursDeRegles: number;
    duree: number;
    /** Jours du cycle (1 = premier jour des règles) avec une douleur de 6/10 ou plus (Douleurs ou bilan). */
    joursDouleurForte: number[];
}
