/** Miroir de l'entité `SymptomeCycle` renvoyée par `SymptomesCycleController` (date locale sans fuseau). */
export interface SymptomeCycle {
    id: number;
    carnetSanteId: number;
    typeSymptome: string;
    /** « AAAA-MM-JJTHH:mm:ss » */
    date: string;
    intensite: number;
    commentaire?: string | null;
    photoUrl?: string | null;
}

/** Saisie d'un symptôme (formulaire multipart de POST et PUT SymptomesCycle, avec la photo éventuelle). */
export interface SymptomeSaisie {
    typeSymptome: string;
    /** « AAAA-MM-JJTHH:mm:ss » */
    date: string;
    intensite: number;
    commentaire: string | null;
    photo: File | null;
    photoSource: 'camera' | 'gallery' | null;
}
