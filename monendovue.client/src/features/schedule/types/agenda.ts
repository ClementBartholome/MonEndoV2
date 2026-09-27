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
