/**
 * Événement de l'agenda, tel que renvoyé par l'API (`EvenementAgendaViewModel` côté serveur).
 * `debut` et `fin` gardent le format de Google : date et heure avec fuseau, ou date seule (AAAA-MM-JJ)
 * quand `journeeEntiere` est vrai.
 */
export interface EvenementAgenda {
    id: string;
    titre: string;
    debut: string;
    fin: string | null;
    journeeEntiere: boolean;
    lieu: string | null;
    lien: string | null;
}

/** Statut de la liaison de l'agenda Google (`StatutLiaison` côté serveur) : `disponible` faux tant que Google n'est pas configuré. */
export interface StatutLiaisonAgenda {
    disponible: boolean;
    liee: boolean;
    lieeLe: string | null;
    /** Calendrier choisi (le seul qui est lu), ou null tant qu'aucun n'est choisi. */
    calendrierId: string | null;
}

/** Calendrier Google proposé au choix (`CalendrierViewModel` côté serveur). */
export interface CalendrierAgenda {
    id: string;
    nom: string;
    principal: boolean;
}
