import axios from 'axios';
import {useAuthStore} from '@/store/auth';
import type {User} from "@/models/user";
import {useToast} from "@/components/ui/toast";
import router from "@/router";

const API_URL = import.meta.env.MODE === 'production' ? import.meta.env.VITE_API_URL_PROD : import.meta.env.VITE_API_URL;

const {toast} = useToast();

class LockService {
    private locks: { [key: string]: boolean } = {};

    acquireLock(key: string): boolean {
        if (this.locks[key]) {
            return false;
        }
        this.locks[key] = true;
        return true;
    }

    releaseLock(key: string): void {
        delete this.locks[key];
    }
}

export const lockService = new LockService();

export const tokenService = {

    async refreshToken() {
        const lockKey = 'refreshToken';
        if (!lockService.acquireLock(lockKey)) {
            while (!lockService.acquireLock(lockKey)) {
                await new Promise(resolve => setTimeout(resolve, 100));
            }
        }

        try {
            const response = await axios.post(`${API_URL}Account/refresh-token`, {}, {
                withCredentials: true
            });

            if (response.status === 200) {
                const authStore = useAuthStore();
                const tokenExpiry = response.data.tokenExpiry;
                authStore.setTokenExpiry(tokenExpiry);
                return response.data;
            }
        } catch (error: any) {
            const authStore = useAuthStore();
            authStore.clearAuth();

            if (error.response) {
                if (error.response.status === 401 || error.response.status === 403 || error.response.status === 404 || error.response.status === 400) {
                    toast({
                        title: 'Session expirée',
                        description: 'Veuillez-vous reconnecter',
                        variant: 'custom'
                    });
                } else {
                    toast({
                        title: 'Erreur',
                        description: 'Une erreur est survenue. Veuillez vous reconnecter.',
                        variant: 'custom'
                    });
                }
            } else {
                toast({
                    title: 'Erreur',
                    description: 'Une erreur est survenue. Veuillez vous reconnecter.',
                    variant: 'custom'
                });
            }

            router.push({ name: 'login' });
        } finally {
            lockService.releaseLock(lockKey);
        }
    }
};