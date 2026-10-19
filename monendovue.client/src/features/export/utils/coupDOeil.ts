import { addDays, differenceInCalendarDays, format, parseISO } from 'date-fns';
import type { SyntheseRendezVous } from '../types/synthese';

/**
 * « En un coup d'œil » : les chiffres clés d'une synthèse, chacun avec ce sur quoi il porte (jours de la période, journées
 * renseignées, prises prévues). Uniquement des comptes de ce qui a été noté, aucune interprétation. Sert à l'aperçu de la
 * page et à la première page du PDF.
 */
export interface CoupDOeil {
    /** Jours de la période, du premier au dernier compris. */
    jours: number;
    /** Jours avec une douleur notée au-dessus de 0 (bilan ou entrée de douleur). */
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

/** Un jour de la frise du PDF : douleur la plus forte notée (bilan ou entrée de douleur), null si rien ; règles. */
export interface JourFrisePdf {
    jour: string;
    douleur: number | null;
    regles: boolean;
}

/** Frise de la période, jour par jour, calculée depuis la synthèse (même règle que la frise de l'application). */
export function friseDeLaSynthese(synthese: Pick<SyntheseRendezVous, 'du' | 'au' | 'regles' | 'douleurs' | 'bilans'>): JourFrisePdf[] {
    const douleurs = new Map<string, number>();
    const noter = (jour: string, intensite: number) => douleurs.set(jour, Math.max(douleurs.get(jour) ?? 0, intensite));
    synthese.bilans.jours.forEach((bilan) => noter(bilan.jour, bilan.douleur));
    synthese.douleurs.entrees.forEach((entree) => noter(entree.jour, entree.intensite));
    const regles = new Set(synthese.regles.jours);
    const jours: JourFrisePdf[] = [];
    for (let date = parseISO(synthese.du); format(date, 'yyyy-MM-dd') <= synthese.au; date = addDays(date, 1)) {
        const jour = format(date, 'yyyy-MM-dd');
        jours.push({ jour, douleur: douleurs.get(jour) ?? null, regles: regles.has(jour) });
    }
    return jours;
}

/** Douleur à partir de laquelle un jour compte comme « douleur forte » (même seuil que le serveur). */
const DOULEUR_FORTE = 6;

export function coupDOeil(synthese: SyntheseRendezVous): CoupDOeil {
    // Douleur de la frise : bilans et entrées de douleur, comme ce que la frise montre juste au-dessus.
    const frise = friseDeLaSynthese(synthese);
    const avecDouleur = frise.filter((j) => j.douleur !== null && j.douleur > 0);
    const forts = avecDouleur.filter((j) => j.douleur! >= DOULEUR_FORTE);
    const journee = synthese.bilans.categories.nuitEtJournee;
    const planifies = synthese.traitements.filter((t) => t.prevues > 0);
    return {
        jours: differenceInCalendarDays(parseISO(synthese.au), parseISO(synthese.du)) + 1,
        joursDouleur: avecDouleur.length,
        joursForts: forts.length,
        fortsPendantRegles: forts.filter((j) => j.regles).length,
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
