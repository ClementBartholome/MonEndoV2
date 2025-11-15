/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

export interface Medicament {
    id: number;
    carnetSanteId: number;
    nom: string;
    posologie: string;
    traitementEnCours: boolean;
    dateDebutTraitement?: Date;
    dateFinTraitement?: Date;
}
