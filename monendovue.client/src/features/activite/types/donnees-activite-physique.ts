export interface DonneesActivitePhysique {
    id: number;
    carnetSanteId: number;
    typeActivite: string;
    date: Date | string;
    duree: number;
    intensite: number;
    commentaire?: string;
}
