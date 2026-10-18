import { differenceInCalendarDays, parseISO } from 'date-fns';
import type { SyntheseRendezVous } from '../types/synthese';

/**
 * « En un coup d'œil » : les chiffres clés d'une synthèse, chacun avec ce sur quoi il porte (jours de la période, journées
 * renseignées, prises prévues). Uniquement des comptes de ce qui a été noté, aucune interprétation. Sert à l'aperçu de la
 * page et à la première page du PDF.
 */
export interface CoupDOeil {
    /** Jours de la période, du premier au dernier compris. */
    jours: number;
    joursDouleur: number;
    /** Jours avec une douleur à 6/10 ou plus, et ceux d'entre eux pendant les règles. */
    joursForts: number;
    fortsPendantRegles: number;
    joursRegles: number;
    debutsRegles: number;
    /** Cycle moyen en jours (cycles commencés sur la période), null s'il n'y en a pas assez. */
    cycleMoyen: number | null;
    /** Journées où « Nuit et journée » a été renseigné, dont limitées (un peu ou très) et avec une absence. */
    journeesNotees: number;
    journeesLimitees: number;
    absences: number;
    /** Prises faites et prévues des traitements à horaires (les traitements au besoin n'ont pas de prises prévues). */
    prisesFaites: number;
    prisesPrevues: number;
}

export function coupDOeil(synthese: SyntheseRendezVous): CoupDOeil {
    const journee = synthese.bilans.categories.nuitEtJournee;
    const planifies = synthese.traitements.filter((t) => t.prevues > 0);
    return {
        jours: differenceInCalendarDays(parseISO(synthese.au), parseISO(synthese.du)) + 1,
        joursDouleur: synthese.douleurs.jours,
        joursForts: synthese.douleurs.joursDouleurForte,
        fortsPendantRegles: synthese.douleurs.joursDouleurFortePendantRegles,
        joursRegles: synthese.regles.jours.length,
        debutsRegles: synthese.regles.debuts.length,
        cycleMoyen: synthese.regles.cycleMoyen,
        journeesNotees: journee.journeesNotees,
        journeesLimitees: journee.journeesPeuLimitees + journee.journeesTresLimitees,
        absences: journee.joursAbsence,
        prisesFaites: planifies.reduce((total, t) => total + t.faites, 0),
        prisesPrevues: planifies.reduce((total, t) => total + t.prevues, 0),
    };
}
