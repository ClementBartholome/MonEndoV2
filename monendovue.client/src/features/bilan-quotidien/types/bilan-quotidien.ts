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

export interface BilanQuotidien extends Partial<TransitBilan> {
    id: number;
    carnetSanteId: number;
    date: Date;
    mood: string;
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
