export type IntensiteTransit = 'Légère' | 'Modérée' | 'Forte';

/** Catégorie « Transit » du bilan : chaque champ est facultatif (null = non renseigné). */
export interface TransitBilan {
    selles: boolean | null;
    typeBristol: number | null;
    crampesEstomac: boolean | null;
    intensiteCrampes: IntensiteTransit | null;
    ballonnements: boolean | null;
    intensiteBallonnements: IntensiteTransit | null;
}

/** Codes des émotions (enum Emotion côté serveur) ; libellés dans config/emotions.ts. */
export type CodeEmotion =
    | 'Joie' | 'Calme' | 'Soulagement' | 'Motivation' | 'Fierte'
    | 'Tristesse' | 'Anxiete' | 'Irritabilite' | 'Frustration' | 'Decouragement';

export interface EmotionBilan {
    id?: number;
    emotion: CodeEmotion;
}

/** Mesures chiffrées du bilan : seule la douleur est obligatoire, null = non renseigné. */
export interface MesuresBilan {
    /** Douleur du jour, de 0 à 10. */
    douleurMoyenne: number;
    /** Stress (vie pro, vie perso) et fatigue, de 0 à 5. */
    stressPro: number | null;
    stressPerso: number | null;
    fatigue: number | null;
    pas: number | null;
    /** En litres. */
    hydratation: number | null;
}

export interface BilanQuotidien extends Partial<TransitBilan>, MesuresBilan {
    id: number;
    carnetSanteId: number;
    date: Date | string;
    /** Ancienne humeur (Heureuse, Neutre, Triste) des bilans saisis avant les émotions ; null ensuite. */
    mood?: string | null;
    /** Une à trois émotions (bilans récents). */
    emotions: EmotionBilan[];
    gluten: boolean;
    lactose: boolean;
    grignotage: boolean;
    commentaire?: string | null;
}

/** Corps envoyé en création (POST, id = 0) ou en modification (PUT) ; date au format yyyy-MM-ddTHH:mm:ss, heure locale. */
export interface BilanQuotidienSaisie extends TransitBilan, MesuresBilan {
    id: number;
    carnetSanteId: number;
    date: string;
    mood: string | null;
    emotions: EmotionBilan[];
    gluten: boolean;
    lactose: boolean;
    grignotage: boolean;
    commentaire: string | null;
}
