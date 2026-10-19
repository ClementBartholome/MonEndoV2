/**
 * Deux drapeaux du parcours d'arrivée, gardés dans le stockage local de l'appareil (rien de sensible, rien envoyé au serveur) :
 * - « déjà venue » : une personne qui s'est déjà connectée arrive sur la connexion, les autres sur la page de bienvenue ;
 * - « premiers pas » : la carte « Pour bien démarrer » de l'accueil, posée à l'inscription, retirée dès qu'elle est utilisée ou fermée ;
 * - « accueil à faire » : posé à l'inscription, il fait passer la première connexion (une fois l'adresse confirmée) par l'étape du rappel.
 */
const CLE_DEJA_VENUE = 'monendo-deja-venue';
const CLE_PREMIERS_PAS = 'monendo-premiers-pas';
const CLE_ACCUEIL_A_FAIRE = 'monendo-accueil-a-faire';

function lire(cle: string): boolean {
    try {
        return localStorage.getItem(cle) === '1';
    } catch {
        // Stockage indisponible (navigation privée…) : le parcours se comporte comme pour une première visite.
        return false;
    }
}

function ecrire(cle: string, actif: boolean) {
    try {
        if (actif) localStorage.setItem(cle, '1');
        else localStorage.removeItem(cle);
    } catch {
        // Sans stockage, le drapeau n'est simplement pas gardé.
    }
}

export const dejaVenue = (): boolean => lire(CLE_DEJA_VENUE);
export const marquerDejaVenue = () => ecrire(CLE_DEJA_VENUE, true);

export const premiersPasActifs = (): boolean => lire(CLE_PREMIERS_PAS);
export const lancerPremiersPas = () => ecrire(CLE_PREMIERS_PAS, true);
export const terminerPremiersPas = () => ecrire(CLE_PREMIERS_PAS, false);

export const marquerAccueilAFaire = () => ecrire(CLE_ACCUEIL_A_FAIRE, true);
/** Vrai une seule fois : la première connexion après l'inscription sur cet appareil. */
export function prendreAccueilAFaire(): boolean {
    const aFaire = lire(CLE_ACCUEIL_A_FAIRE);
    if (aFaire) ecrire(CLE_ACCUEIL_A_FAIRE, false);
    return aFaire;
}
