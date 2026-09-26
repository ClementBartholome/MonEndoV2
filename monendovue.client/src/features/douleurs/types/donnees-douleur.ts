export interface DonneesDouleur {
    id: number;
    carnetSanteId: number;
    intensite: number;
    typeDouleur: string;
    date: Date;
    commentaire?: string;
}
