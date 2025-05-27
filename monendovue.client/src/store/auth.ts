import { defineStore } from 'pinia';
import authService from '@/services/authService';
import type { User } from '@/models/user';

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
                    deviceToken: '',
                };
                this.setAuth(this.user);
            }
            return response;
        },
        async register(email: string, password: string) {
            try {
                const response = await authService.register(email, password);
                if (response) {
                    this.user = {
                        email: response.userName,
                        carnetSanteId: response.carnetSanteId,
                        tokenExpiry: response.tokenExpiry,
                        deviceToken: '',
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
        },
        setAuth(user: User | null) {
            this.user = user;
            localStorage.setItem('user', JSON.stringify(user));
        },
        setTokenExpiry(tokenExpiry: Date) {
            if (this.user) {
                this.user.tokenExpiry = tokenExpiry;
                this.setAuth(this.user);
            }
        }
    },
});