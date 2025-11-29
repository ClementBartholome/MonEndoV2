import type { Medicament } from "./medicament";

export interface DonneesTraitementNonMedicamenteux {
    id: number;
    carnetSanteId: number;
    medicamentId: number;
    duree?: number;
    date: Date;
    commentaire?: string;
    medicament?: Medicament;
}
