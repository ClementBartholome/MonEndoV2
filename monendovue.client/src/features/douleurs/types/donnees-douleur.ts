export interface DonneesDouleur {
    id: number;
    carnetSanteId: number;
    intensite: number;
    typeDouleur: string;
    date: Date;
    commentaire?: string;
}

/** Corps de `PUT DonneesDouleurs/{id}` (miroir de `DonneesDouleurDto` côté serveur). */
export interface DonneesDouleurModification {
    typeDouleur: string;
    intensite: number;
    date: Date;
    commentaire?: string | null;
}
