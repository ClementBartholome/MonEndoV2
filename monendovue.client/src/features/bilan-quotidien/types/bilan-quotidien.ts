/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

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
