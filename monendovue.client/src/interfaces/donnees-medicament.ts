/**
 * This is a TypeGen auto-generated file.
 * Any changes made to this file can be lost when this file is regenerated.
 */

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
