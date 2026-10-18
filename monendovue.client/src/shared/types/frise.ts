/** Miroir de `JourFriseViewModel` : un jour de la frise (accueil, page Cycle). */
export interface JourFrise {
    /** AAAA-MM-JJ. */
    jour: string;
    /** Douleur la plus forte notée ce jour-là (0 à 10) ; null si rien n'est noté. */
    douleur: number | null;
    regles: boolean;
}
