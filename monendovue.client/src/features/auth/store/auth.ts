import { defineStore } from 'pinia';
import authService from '@/features/auth/services/authService';
import type { User } from '@/features/auth/types/user';
import { effacerQuestions } from '@/features/export/services/questionsRendezVous';

export const useAuthStore = defineStore({
    id: 'auth',
    state: (): { user: User | null } => ({
        user: null
    }),
    actions: {
        async login(email: string, password: string) {
            const response = await authService.login(email, password);
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
        async register(email: string, password: string, consentementDonneesSante: boolean) {
            try {
                const response = await authService.register(email, password, consentementDonneesSante);
                if (response) {
                    this.user = {
                        email: response.userName,
                        carnetSanteId: response.carnetSanteId,
                        tokenExpiry: response.tokenExpiry,
                        consentementAJour: response.consentementAJour,
                    };
                    this.setAuth(this.user);
                    return response;
                }
            } catch (error: any) {
                console.error(error);
                throw error;
            }
        },
        async logout() {
            await authService.logout();
            this.clearAuth();
        },
        checkAuth() {
            const userItem = localStorage.getItem('user');

            if (userItem) {
         
                this.user = JSON.parse(userItem);

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