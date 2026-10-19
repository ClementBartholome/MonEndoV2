export type IntensiteTransit = 'Légère' | 'Modérée' | 'Forte';

/** Catégorie « Transit » du bilan : chaque champ est facultatif (null = non renseigné). */
export interface TransitBilan {
    selles: boolean | null;
    typeBristol: number | null;
    crampesEstomac: boolean | null;
    intensiteCrampes: IntensiteTransit | null;
    ballonnements: boolean | null;
    intensiteBallonnements: IntensiteTransit | null;
    /** Suite du transit (1.5.0) : douleur en allant à la selle, nausées, traces de sang dans les selles. */
    douleurSelle: boolean | null;
    intensiteDouleurSelle: IntensiteTransit | null;
    nausees: boolean | null;
    sangSelles: boolean | null;
}

/** Codes des choix (les mêmes que côté serveur, `BilanCategoriesValidator`) ; libellés dans config/questions.ts. */
export type AbondanceSaignements = 'Traces' | 'Legers' | 'Abondants';
export type QualiteNuit = 'Bonne' | 'Moyenne' | 'Difficile';
export type LimitationJournee = 'PasLimitee' | 'PeuLimitee' | 'TresLimitee';
export type ReponseRapport = 'Oui' | 'Non' | 'PasDeRapport';

/** Catégories facultatives du bilan (1.5.0) : chaque champ vaut null tant qu'il n'est pas renseigné (jamais « non » ni 0 par défaut). */
export interface CategoriesBilan {
    /** Urinaire */
    douleurUriner: boolean | null;
    intensiteDouleurUriner: IntensiteTransit | null;
    enviesUrinaires: boolean | null;
    difficulteVider: boolean | null;
    sangUrines: boolean | null;
    /** Saignements hors règles (les jours de règles se notent dans l'onglet Règles) */
    saignementsHorsRegles: boolean | null;
    abondanceSaignementsHorsRegles: AbondanceSaignements | null;
    /** Nuit et journée */
    nuit: QualiteNuit | null;
    reveilsDouleur: boolean | null;
    limitationJournee: LimitationJournee | null;
    absenceTravail: boolean | null;
    activiteAnnulee: boolean | null;
    /** Rapports : catégorie discrète */
    douleurRapport: ReponseRapport | null;
}

/** Toutes les réponses facultatives du bilan qui se posent par questions (transit complété compris). */
export type ReponsesBilan = TransitBilan & CategoriesBilan;

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

export interface BilanQuotidien extends Partial<TransitBilan>, Partial<CategoriesBilan>, MesuresBilan {
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
export interface BilanQuotidienSaisie extends TransitBilan, CategoriesBilan, MesuresBilan {
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
