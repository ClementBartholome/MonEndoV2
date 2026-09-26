export interface BilanQuotidien {
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
