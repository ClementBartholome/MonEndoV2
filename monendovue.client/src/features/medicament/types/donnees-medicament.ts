import type { Medicament } from "./medicament";

export interface DonneesMedicament {
    id: number;
    carnetSanteId: number;
    medicamentId: number;
    nombreComprimes: number;
    date: Date;
    commentaire?: string;
    medicament?: Medicament;
}
