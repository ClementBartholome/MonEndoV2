import axios from 'axios';
import { API_URL } from '@/shared/services/apiBase';
import {useAuthStore} from '@/features/auth/store/auth';
import type {User} from "@/features/auth/types/user";
import {useToast} from "@/shared/components/ui/toast";
import router from "@/router";


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

/** Réponses où le serveur refuse vraiment la session : il faut se reconnecter. Tout le reste (réseau, 5xx, 429) est passager. */
const REFUS_DE_SESSION = [400, 401, 403, 404];

/** Pauses avant chaque nouvel essai quand le serveur est momentanément injoignable (déploiement en cours : quelques secondes). */
const PAUSES_ENTRE_ESSAIS_MS = [1000, 3000, 6000];

const attendre = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

export const tokenService = {

    async refreshToken() {
        const lockKey = 'refreshToken';
        if (!lockService.acquireLock(lockKey)) {
            while (!lockService.acquireLock(lockKey)) {
                await new Promise(resolve => setTimeout(resolve, 100));
            }
        }

        try {
            // Plusieurs appels attendaient le renouvellement : le premier l'a fait, inutile d'en refaire un (chaque renouvellement
            // remplace le jeton côté serveur).
            const expiration = useAuthStore().user?.tokenExpiry;
            if (expiration && new Date(expiration) > new Date()) return;

            for (let essai = 0; ; essai++) {
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
                    return;
                } catch (error: any) {
                    if (REFUS_DE_SESSION.includes(error.response?.status)) {
                        useAuthStore().clearAuth();
                        toast({
                            title: 'Session expirée',
                            description: 'Veuillez-vous reconnecter',
                            variant: 'custom'
                        });
                        router.push({ name: 'login' });
                        return;
                    }
                    // Serveur momentanément injoignable : la session est conservée, l'appel qui en avait besoin échoue seul.
                    if (essai >= PAUSES_ENTRE_ESSAIS_MS.length) throw error;
                    await attendre(PAUSES_ENTRE_ESSAIS_MS[essai]);
                }
            }
        } finally {
            lockService.releaseLock(lockKey);
        }
    }
};