import axios from 'axios';
import { API_URL } from '@/shared/services/apiBase';

/** Résultat d'une demande liée au compte : réussie, ou un message à afficher tel quel (déjà en français). */
export type ResultatCompte = { ok: true } | { ok: false; message: string; erreurs: string[] };

const options = { headers: { 'Content-Type': 'application/json' }, withCredentials: true };

/** Message lisible pour une erreur d'appel : refus du serveur, trop de tentatives, service ou réseau indisponible. */
export function messageErreurCompte(erreur: unknown): { message: string; erreurs: string[] } {
    const reponse = (erreur as { response?: { status?: number; data?: { message?: unknown; erreurs?: unknown } } })?.response;
    if (!reponse) return { message: 'Pas de connexion pour le moment. Réessaie dans un instant.', erreurs: [] };
    if (reponse.status === 429) return { message: 'Trop de tentatives. Patiente une minute avant de réessayer.', erreurs: [] };
    const erreurs = Array.isArray(reponse.data?.erreurs) ? (reponse.data.erreurs as string[]) : [];
    if (reponse.status === 400 && typeof reponse.data?.message === 'string') return { message: reponse.data.message, erreurs };
    return { message: 'Ce service est momentanément indisponible. Réessaie un peu plus tard.', erreurs: [] };
}

async function poster(chemin: string, corps: object): Promise<ResultatCompte> {
    try {
        await axios.post(`${API_URL}Account/${chemin}`, corps, options);
        return { ok: true };
    } catch (erreur) {
        return { ok: false, ...messageErreurCompte(erreur) };
    }
}

/**
 * Demandes liées au compte sans session : le jeton d'un lien reçu par e-mail est lu dans le fragment par la page, puis envoyé
 * dans le corps de la requête, jamais dans une URL. Le serveur répond de la même façon que l'adresse soit inscrite ou non.
 */
export const comptesService = {
    confirmerAdresse: (email: string, jeton: string) => poster('confirmer-adresse', { email, jeton }),
    renvoyerConfirmation: (email: string) => poster('renvoyer-confirmation', { email }),
    motDePasseOublie: (email: string) => poster('mot-de-passe-oublie', { email }),
    reinitialiserMotDePasse: (email: string, jeton: string, nouveauMotDePasse: string) =>
        poster('reinitialiser-mot-de-passe', { email, jeton, nouveauMotDePasse }),
};
