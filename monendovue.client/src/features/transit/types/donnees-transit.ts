/** Miroir de `DonneesTransit` (ancien suivi du transit par événements, en lecture depuis les bilans quotidiens). */
export interface DonneesTransit {
    id: number;
    /** Date locale sans fuseau (« AAAA-MM-JJTHH:mm:ss »). */
    date: string;
    typeEvenement: string;
    /** Légère, Modérée ou Sévère. */
    intensite: string;
    saignement: boolean;
    douleur: boolean;
    commentaires?: string | null;
}
