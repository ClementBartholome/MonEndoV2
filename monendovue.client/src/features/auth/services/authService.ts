import axios from 'axios';
import { API_URL } from '@/shared/services/apiBase';

/** Réponse de connexion d'un compte dont l'adresse n'est pas encore confirmée (code renvoyé par `AccountController`). */
export const ADRESSE_NON_CONFIRMEE = 'adresse-non-confirmee';


const authService = {
    async login(email: string, password: string) {
        try {
            const response = await axios.post(`${API_URL}Account/login`, { email, password }, {
                headers: {
                    'Content-Type': 'application/json'
                },
                withCredentials: true
            });

            if (response.status === 200) {
                return response.data;
            }
        } catch (error: any) {
            console.error(error);
            if (error.response) {
                console.error(error.response.data);
            }
            // Identifiants refusés (ou compte verrouillé) : null. Adresse pas encore confirmée (bon mot de passe) : un repère dédié.
            // Serveur injoignable, 429, 5xx : l'erreur est remontée.
            if (error.response?.status === 401) return null;
            if (error.response?.status === 403 && error.response.data?.code === ADRESSE_NON_CONFIRMEE) return ADRESSE_NON_CONFIRMEE;
            throw error;
        }
    },

    /** Ce que demande l'inscription : un code d'invitation tant que l'ouverture est progressive. En cas de doute, on le demande. */
    async inscriptionSurInvitation(): Promise<boolean> {
        try {
            const response = await axios.get(`${API_URL}Account/inscription`, { withCredentials: true });
            return response.data?.surInvitation !== false;
        } catch {
            return true;
        }
    },

    async register(email: string, password: string, consentementDonneesSante: boolean, codeInvitation: string | null = null) {
        try {
            const response = await axios.post(`${API_URL}Account/register`, { email, password, consentementDonneesSante, codeInvitation }, {
                headers: {
                    'Content-Type': 'application/json'
                },
                withCredentials: true
            });

            // 202 : compte créé (ou adresse déjà inscrite : même réponse), en attente de la confirmation de l'adresse.
            return response.data;
        } catch (error: any) {
            if (error.response && error.response.status === 400) {
                throw new Error(error.response.data?.$values?.[0] ?? "Requête invalide.");
            }
            throw error;
        }
    },

    async logout() {
        try {
            await axios.post(`${API_URL}Account/logout`, {}, {
                headers: {
                    'Content-Type': 'application/json'
                },
                withCredentials: true
            });
        } catch (error) {
            console.error(error);
        }
    },
    
    async changePassword(currentPassword: string, newPassword: string) {
        try {
            const response = await axios.post(`${API_URL}Account/change-password`, { currentPassword, newPassword }, {
                headers: {
                    'Content-Type': 'application/json'
                },
                withCredentials: true
            });

            if (response.status === 200) {
                return response.data;
            } else {
                throw new Error(response.data.$values[0].description);
            }
        } catch (error: any) {
            if (error.response && error.response.status === 400) {
                throw new Error(error.response.data?.$values?.[0] ?? "Requête invalide.");
            }
            if (error.response && error.response.status === 429) {
                throw new Error(error.response.data?.message ?? "Trop d'essais. Réessaie dans quelques minutes.");
            }
            throw error;
        }
    }
};

export default authService;