/** Miroir de `CycleViewModel` (GET Cycle?jour=…&mois=…) : dates « AAAA-MM-JJ ». */
export interface CycleDuMois {
    joursDeRegles: string[];
    enCours: CycleEnCours | null;
    cycles: CycleTermine[];
    /** Moyenne des 6 derniers cycles ; null avant deux cycles terminés. */
    dureeMoyenne: number | null;
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
}
