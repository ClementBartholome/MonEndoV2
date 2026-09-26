export interface DonneesTransit {
    id: number;
    carnetSanteId: number;
    date: Date;
    typeEvenement: string;
    intensite: string;
    saignement: boolean;
    douleur: boolean;
    commentaires?: string;
}
