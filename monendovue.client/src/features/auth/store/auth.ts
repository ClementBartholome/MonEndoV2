import { defineStore } from 'pinia';
import authService, { ADRESSE_NON_CONFIRMEE } from '@/features/auth/services/authService';
import type { User } from '@/features/auth/types/user';
import { effacerQuestions } from '@/features/export/services/questionsRendezVous';
import { marquerDejaVenue } from '@/features/auth/utils/parcours';

export const useAuthStore = defineStore({
    id: 'auth',
    state: (): { user: User | null } => ({
        user: null
    }),
    actions: {
        async login(email: string, password: string) {
            const response = await authService.login(email, password);
            if (response === ADRESSE_NON_CONFIRMEE) return response;
            if (response) {
                this.user = {
                    email: response.userName,
                    carnetSanteId: response.carnetSanteId,
                    tokenExpiry: response.tokenExpiry,
                    consentementAJour: response.consentementAJour,
                };
                this.setAuth(this.user);
            }
            return response;
        },
        /** Crée le compte : aucune session n'est ouverte, l'adresse doit d'abord être confirmée par le lien reçu par e-mail. */
        async register(email: string, password: string, consentementDonneesSante: boolean) {
            return authService.register(email, password, consentementDonneesSante);
        },
        async logout() {
            await authService.logout();
            this.clearAuth();
        },
        checkAuth() {
            const userItem = localStorage.getItem('user');

            if (userItem) {
         
                this.user = JSON.parse(userItem);
                // Une session ouverte avant l'accueil guidé : elle n'a pas à revoir la page de bienvenue.
                marquerDejaVenue();

            }
        },
        getUser() {
            return this.user;
        },
        clearAuth() {
            this.user = null;
            localStorage.removeItem('user');
            effacerQuestions();
        },
        setAuth(user: User | null) {
            this.user = user;
            if (user) marquerDejaVenue();
            localStorage.setItem('user', JSON.stringify(user));
        },
        /** Consentement enregistré par le serveur (nouveau jeton) ou refusé par l'API (403 consentement-requis). */
        setConsentement(consentementAJour: boolean, tokenExpiry?: Date) {
            if (this.user) {
                this.user.consentementAJour = consentementAJour;
                if (tokenExpiry) this.user.tokenExpiry = tokenExpiry;
                this.setAuth(this.user);
            }
        },
        setTokenExpiry(tokenExpiry: Date) {
            if (this.user) {
                this.user.tokenExpiry = tokenExpiry;
                this.setAuth(this.user);
            }
        }
    },
});