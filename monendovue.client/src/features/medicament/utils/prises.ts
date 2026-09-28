import { format } from 'date-fns';
import type { FrequencePrise, JourSemaine, PrisePrevue, PriseSaisie, StatutPrise, Traitement } from '../types/traitements';
import { JOURS_SEMAINE } from '../types/traitements';

/** « 8 h 00 » à partir de « 08:00 » ou d'une date locale « AAAA-MM-JJTHH:mm:ss ». */
export function heureAffichee(valeur: string): string {
    const texte = valeur.includes('T') ? valeur.slice(11, 16) : valeur.slice(0, 5);
    const [heures, minutes] = texte.split(':');
    return `${Number(heures)} h ${minutes}`;
}

/** Réponse à une prise prévue (ou prise au besoin si `heurePrevue` est absente), à l'heure locale. */
export function reponse(statut: StatutPrise, heurePrevue: string | null, maintenant: Date): PriseSaisie {
    return {
        statut,
        heurePrevue: heurePrevue ? `${heurePrevue}:00` : null,
        date: format(maintenant, "yyyy-MM-dd'T'HH:mm:ss"),
    };
}

/** Moment de la journée d'une prise prévue, pour les regrouper comme dans l'app Santé. */
export function momentDe(heurePrevue: string): 'Matin' | 'Après-midi' | 'Soir' {
    const heure = Number(heurePrevue.slice(0, 2));
    if (heure < 12) return 'Matin';
    return heure < 18 ? 'Après-midi' : 'Soir';
}

export interface GroupePrises {
    moment: 'Matin' | 'Après-midi' | 'Soir';
    /** Horaire de la première prise du groupe (« 8 h 00 »). */
    heure: string;
    prises: PrisePrevue[];
}

/** Prises regroupées par moment, dans l'ordre de la journée (elles arrivent triées par horaire). */
export function grouperParMoment(prises: PrisePrevue[]): GroupePrises[] {
    const groupes: GroupePrises[] = [];
    prises.forEach((prise) => {
        const moment = momentDe(prise.heurePrevue);
        const dernier = groupes[groupes.length - 1];
        if (dernier?.moment === moment) dernier.prises.push(prise);
        else groupes.push({ moment, heure: heureAffichee(prise.heurePrevue), prises: [prise] });
    });
    return groupes;
}

const ABREVIATIONS: Record<JourSemaine, string> = {
    Lundi: 'lun', Mardi: 'mar', Mercredi: 'mer', Jeudi: 'jeu', Vendredi: 'ven', Samedi: 'sam', Dimanche: 'dim',
};

/** Fréquence en clair : « Tous les jours · 8 h 00 », « Lun, mer, ven · 21 h 00 », « Au besoin »… */
export function resumeFrequence(traitement: Pick<Traitement, 'frequence' | 'joursSemaine' | 'intervalleJours' | 'horaires'>): string {
    const heures = traitement.horaires.map(heureAffichee).join(', ');
    const libelles: Record<FrequencePrise, string> = {
        AuBesoin: 'Au besoin',
        ChaqueJour: 'Tous les jours',
        CertainsJours: JOURS_SEMAINE.filter((j) => traitement.joursSemaine.includes(j)).map((j) => ABREVIATIONS[j]).join(', '),
        TousLesNJours: `Tous les ${traitement.intervalleJours} jours`,
    };
    const libelle = libelles[traitement.frequence];
    const texte = libelle.charAt(0).toUpperCase() + libelle.slice(1);
    return traitement.frequence === 'AuBesoin' || !heures ? texte : `${texte} · ${heures}`;
}
