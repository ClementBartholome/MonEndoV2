/** Douleur telle que renvoyée par l'API (`DonneesDouleur`). */
export interface DonneesDouleur {
    id: number;
    carnetSanteId: number;
    intensite: number;
    typeDouleur: string;
    /** Date locale sans fuseau : AAAA-MM-JJTHH:mm:ss. */
    date: string;
    commentaire?: string | null;
}

/** Corps de `POST DonneesDouleurs` (avec `carnetSanteId`) et de `PUT DonneesDouleurs/{id}` (`DonneesDouleurDto`). */
export interface DonneesDouleurModification {
    typeDouleur: string;
    /** 0 à 10 (le serveur refuse au-delà). */
    intensite: number;
    /** Date locale sans fuseau : AAAA-MM-JJTHH:mm:ss (jamais une Date, qui glisserait en UTC). */
    date: string;
    commentaire?: string | null;
}
