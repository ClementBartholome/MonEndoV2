export interface SymptomeCycle {
    id: number;
    carnetSanteId: number;
    typeSymptome: string;
    date: Date;
    intensite: number;
    commentaire?: string;
    photoUrl?: string
}
