/** Zone notée dans le bilan (« Où as-tu eu mal ? ») : miroir de `ZoneDouleurViewModel` et de `ZoneDouleurDto`. */
export interface ZoneDouleur {
    typeDouleur: string;
    /** 1 à 10. */
    intensite: number;
}

/** Crise notée sur la page Douleurs ce jour-là (`CriseDouleurViewModel`). */
export interface CriseDouleur {
    typeDouleur: string;
    intensite: number;
    /** Heure locale « HH:mm ». */
    heure: string;
}

/** Réponse de `GET DouleursDuJour/{jour}` (`DouleursDuJourViewModel`). */
export interface DouleursDuJour {
    zones: ZoneDouleur[];
    crises: CriseDouleur[];
}
