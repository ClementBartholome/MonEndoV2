/** Miroirs des vues et DTO de `TraitementsController` (GET Traitements/jour, POST/PUT Traitements…). */

export type FrequencePrise = 'AuBesoin' | 'ChaqueJour' | 'CertainsJours' | 'TousLesNJours';
export type TypeTraitement = 'Medicamenteux' | 'NonMedicamenteux';
export type StatutPrise = 'Pris' | 'Ignore';
export type JourSemaine = 'Lundi' | 'Mardi' | 'Mercredi' | 'Jeudi' | 'Vendredi' | 'Samedi' | 'Dimanche';

export const JOURS_SEMAINE: JourSemaine[] = ['Lundi', 'Mardi', 'Mercredi', 'Jeudi', 'Vendredi', 'Samedi', 'Dimanche'];

/** `PrisePrevueViewModel` : une prise prévue ce jour-là et la réponse notée s'il y en a une. */
export interface PrisePrevue {
    traitementId: number;
    nom: string;
    dose: string | null;
    /** « HH:mm » */
    heurePrevue: string;
    reponse: ReponsePrise | null;
}

export interface ReponsePrise {
    priseId: number;
    statut: StatutPrise;
    /** Date locale sans fuseau. */
    date: string;
}

export interface TraitementAuBesoin {
    id: number;
    nom: string;
    dose: string | null;
    dernierePrise: string | null;
}

export interface Soin {
    id: number;
    nom: string;
    derniereSeance: string | null;
}

/** `TraitementViewModel` : liste et formulaire de modification. */
export interface Traitement {
    id: number;
    nom: string;
    type: TypeTraitement;
    dose: string | null;
    frequence: FrequencePrise;
    joursSemaine: JourSemaine[];
    intervalleJours: number | null;
    /** « HH:mm » */
    horaires: string[];
    /** AAAA-MM-JJ */
    dateDebut: string;
    dateFin: string | null;
}

export interface TraitementsDuJour {
    prisesPrevues: PrisePrevue[];
    auBesoin: TraitementAuBesoin[];
    soins: Soin[];
    enCours: Traitement[];
    termines: Traitement[];
}

/** Corps de POST Traitements et PUT Traitements/{id} (`TraitementDto`). */
export interface TraitementSaisie {
    nom: string;
    type: TypeTraitement;
    dose: string | null;
    frequence: FrequencePrise;
    joursSemaine: JourSemaine[];
    intervalleJours: number | null;
    /** « HH:mm:ss » */
    horaires: string[];
    dateDebut: string;
    dateFin: string | null;
}

/** Corps de POST Traitements/{id}/prises (`PriseDto`). */
export interface PriseSaisie {
    statut: StatutPrise;
    /** « HH:mm:ss », absent pour une prise « au besoin ». */
    heurePrevue: string | null;
    /** Date locale sans fuseau. */
    date: string;
}

/** Corps de POST Traitements/{id}/seances (`SeanceDto`). */
export interface SeanceSaisie {
    date: string;
    duree: number | null;
    commentaire: string | null;
}
