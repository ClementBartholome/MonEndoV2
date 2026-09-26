/** Abonnement Web Push d'un appareil, tel que transmis à l'API. */
export interface AbonnementPush {
    endpoint: string;
    p256dh: string;
    auth: string;
}

/** Rappel quotidien du bilan : heure locale HH:mm dans un fuseau IANA. */
export interface PreferenceRappel {
    rappelActif: boolean;
    heureRappel: string;
    fuseauHoraire: string;
}

export type EtatNotifications =
    | 'chargement'
    | 'non-supporte'
    | 'ios-a-installer'
    | 'non-disponible'
    | 'refuse'
    | 'inactif'
    | 'actif';
