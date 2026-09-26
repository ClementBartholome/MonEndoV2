/** Abonnement Web Push d'un appareil, tel que transmis à l'API. */
export interface AbonnementPush {
    endpoint: string;
    p256dh: string;
    auth: string;
}

export type TypeRappel = 'BilanQuotidien' | 'SuiviAcne';

/**
 * Réglage d'un rappel : heure locale HH:mm dans un fuseau IANA ; jour (0 = dimanche … 6 = samedi) pour un rappel hebdomadaire.
 * `type` et `estHebdomadaire` sont fournis par l'API.
 */
export interface Rappel {
    type: TypeRappel;
    estHebdomadaire: boolean;
    actif: boolean;
    heure: string;
    jourSemaine: number | null;
    fuseauHoraire: string;
}

export type ReglageRappel = Pick<Rappel, 'actif' | 'heure' | 'jourSemaine' | 'fuseauHoraire'>;

export type EtatNotifications =
    | 'chargement'
    | 'non-supporte'
    | 'ios-a-installer'
    | 'non-disponible'
    | 'refuse'
    | 'inactif'
    | 'actif';
