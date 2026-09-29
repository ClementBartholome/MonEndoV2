/** Miroir de `AcneViewModel` (GET Acne?jour=…). */
export interface Acne {
    /** Du plus récent au plus ancien ; le premier est en cours si sa fin est nulle. */
    episodes: EpisodeAcne[];
    /** Points de suivi avec photo, du plus récent au plus ancien. */
    suivis: SuiviAcne[];
}

export interface EpisodeAcne {
    id: number;
    /** AAAA-MM-JJ */
    debut: string;
    fin: string | null;
    jours: number;
}

export interface SuiviAcne {
    id: number;
    /** Date locale sans fuseau. */
    date: string;
    intensite: number;
    commentaire: string | null;
    photoUrl: string;
}

/** Corps de POST et PUT Acne/episodes (`EpisodeAcneDto`). */
export interface EpisodeAcneSaisie {
    debut: string;
    fin: string | null;
}
