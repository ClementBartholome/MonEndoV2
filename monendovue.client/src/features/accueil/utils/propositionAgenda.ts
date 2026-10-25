import { addDays } from 'date-fns';

/**
 * « Plus tard » sur la proposition de lier son agenda (accueil) : elle ne revient pas avant 30 jours sur cet appareil. Rien de
 * sensible ni d'envoyé au serveur ; sans stockage, elle reste simplement proposée.
 */
const CLE = 'monendo-agenda-plus-tard';
export const JOURS_AVANT_DE_REPROPOSER = 30;

export function propositionAgendaReportee(maintenant: Date): boolean {
    try {
        const jusquau = localStorage.getItem(CLE);
        return jusquau !== null && maintenant < new Date(jusquau);
    } catch {
        return false;
    }
}

export function reporterPropositionAgenda(maintenant: Date) {
    try {
        localStorage.setItem(CLE, addDays(maintenant, JOURS_AVANT_DE_REPROPOSER).toISOString());
    } catch {
        // Sans stockage, la proposition reviendra au prochain affichage.
    }
}
