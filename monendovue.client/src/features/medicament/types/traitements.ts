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

/** `HistoriqueTraitementViewModel` : un traitement sur un mois (GET Traitements/{id}/historique). */
export interface HistoriqueTraitement {
    traitement: Traitement;
    /** Prises prévues du mois jusqu'à aujourd'hui (0 pour un traitement au besoin ou un soin). */
    prevues: number;
    /** Prises faites ou séances. */
    faites: number;
    ignorees: number;
    faitesMoisPrecedent: number;
    jours: JourHistoriqueTraitement[];
}

export interface JourHistoriqueTraitement {
    /** AAAA-MM-JJ */
    jour: string;
    entrees: EntreeHistoriqueTraitement[];
}

export interface EntreeHistoriqueTraitement {
    /** Identifiant de la prise ou de la séance. */
    id: number;
    nature: 'Pris' | 'Ignore' | 'Seance';
    /** Date locale sans fuseau. */
    date: string;
    /** « HH:mm » de la prise prévue ; null pour une prise au besoin ou une séance. */
    heurePrevue: string | null;
}

/** Corps de POST Traitements/{id}/seances (`SeanceDto`). */
export interface SeanceSaisie {
    date: string;
    duree: number | null;
    commentaire: string | null;
}
