import type { TypeTraitement } from "./type-traitement";

export interface Medicament {
    id: number;
    carnetSanteId: number;
    nom: string;
    type: TypeTraitement;
    posologie?: string;
    traitementEnCours: boolean;
    dateDebutTraitement?: Date;
    dateFinTraitement?: Date;
}
