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

export interface BilanQuotidien extends Partial<TransitBilan> {
    id: number;
    carnetSanteId: number;
    date: Date;
    /** Ancienne humeur (Heureuse, Neutre, Triste) des bilans saisis avant les émotions ; null ensuite. */
    mood?: string | null;
    /** Une à trois émotions (bilans récents). */
    emotions: EmotionBilan[];
    stressPro: number;
    stressPerso: number;
    fatigue: number;
    pas: number;
    douleurMoyenne: number;
    hydratation: number;
    gluten: boolean;
    lactose: boolean;
    grignotage: boolean;
    commentaire?: string;
}
