import axios from 'axios';
import type { AxiosInstance, AxiosResponse } from 'axios';
import { useAuthStore } from '@/features/auth/store/auth';
import type { Pinia } from 'pinia';
import { tokenService } from '@/features/auth/services/tokenService';
import router from "@/router";
import type { DonneesDouleurModification } from '@/features/douleurs/types/donnees-douleur';
import type { AbonnementPush, Rappel, ReglageRappel, TypeRappel } from '@/features/parametres/types/notifications';
import type { BilanQuotidien, BilanQuotidienSaisie, EmotionBilan } from '@/features/bilan-quotidien/types/bilan-quotidien';
import type { HistoriqueBilans } from '@/features/bilan-quotidien/types/historique';
import { enTableau } from '@/shared/utils/json';

const API_URL = import.meta.env.VITE_DOCKER === 'true'
    ? '' 
    : (import.meta.env.MODE === 'production' ? import.meta.env.VITE_API_URL_PROD : import.meta.env.VITE_API_URL);

class ApiService {
    private axiosInstance: AxiosInstance;
    private authStore: ReturnType<typeof useAuthStore> | null = null;
    private pinia: Pinia | null = null;

    constructor() {
        this.axiosInstance = axios.create({
            baseURL: API_URL,
            withCredentials: true
        });
        // this.setupInterceptors();
    }

    public init(pinia: Pinia): void {
        this.pinia = pinia;
        this.authStore = useAuthStore(this.pinia);
        // this.setupInterceptors();
    }

    // private setupInterceptors(): void {
    //     this.axiosInstance.interceptors.response.use(
    //         (response) => response,
    //         async (error: AxiosError) => {
    //             const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
    //             if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
    //                 originalRequest._retry = true;
    //                 try {
    //                     await tokenService.refreshToken();
    //                     return this.axiosInstance(originalRequest);
    //                 } catch (refreshError) {
    //                     return Promise.reject(refreshError);
    //                 }
    //             }
    //             return Promise.reject(error);
    //         }
    //     );
    // }
    private async request<T>(method: string, url: string, data?: any): Promise<T> {
        try {
            const tokenExpired = this.isTokenExpired();
            if (tokenExpired) {
                try {
                    await tokenService.refreshToken();
                } catch (error) {
                    console.error('Error refreshing token:', error);
                    router.push({ name: 'login' });
                    throw error;
                }
            }

            const response: AxiosResponse<T> = await this.axiosInstance.request({
                method,
                url,
                data,
            });
            return response.data;
        } catch (error: any) {
            console.error(`Error in ${method} request to ${url}:`, error);
            // Re-throw the error so it can be handled by the caller and background sync
            throw error;
        }
    }

    private isTokenExpired(): boolean {
        if (!this.authStore || !this.authStore.user) {
            return true;
        }
        const tokenExpiryDate = new Date(this.authStore.user.tokenExpiry);
        const now = new Date();
        return tokenExpiryDate < now;
    }
    
    // GET
    async getDonneesCarnetSante(carnetSanteId: number): Promise<any> {
        return this.request('GET', `CarnetSante/${carnetSanteId}`);
    }

    async getLastDonneesCarnetSante(carnetSanteId: number): Promise<any> {
        return this.request('GET', `CarnetSante/last-entries/${carnetSanteId}`);
    }

    async getDonneesCarnetSanteByMonth(carnetSanteId: number, month: number, year: number): Promise<any> {
        return this.request('GET', `CarnetSante/${carnetSanteId}/${month}/${year}`);
    }

    async getDonneesDouleursByMonth(carnetSanteId: number, month: number, year: number): Promise<any> {
        return this.request('GET', `DonneesDouleurs/${carnetSanteId}/${month}/${year}`);
    }

    async getDonneesActivitePhysiqueByMonth(carnetSanteId: number, month: number, year: number): Promise<any> {
        return this.request('GET', `DonneesActivitePhysique/${carnetSanteId}/${month}/${year}`);
    }

    async getDonneesMedicamentByMonth(carnetSanteId: number, month: number, year: number): Promise<any> {
        return this.request('GET', `DonneesMedicament/${carnetSanteId}/${month}/${year}`);
    }

    async getDonneesTraitementNonMedicamenteuxByMonth(carnetSanteId: number, month: number, year: number): Promise<any> {
        return this.request('GET', `DonneesTraitementNonMedicamenteux/${carnetSanteId}/${month}/${year}`);
    }

    async getDonneesTransitByMonth(carnetSanteId: number, month: number, year: number): Promise<any> {
        return this.request('GET', `DonneesTransit/${carnetSanteId}/${month}/${year}`);
    }

    /** Bilans et jours de règles du `du` au `au` inclus (yyyy-MM-dd), carnet déduit de la session. */
    async getHistoriqueBilans(du: string, au: string): Promise<HistoriqueBilans> {
        type Liste<T> = T[] | { $values: T[] };
        type BilanRecu = Omit<BilanQuotidien, 'emotions'> & { emotions?: Liste<EmotionBilan> };
        const response = await this.request<{ bilans: Liste<BilanRecu>; joursRegles: Liste<string> }>(
            'GET', `BilanQuotidien/periode?du=${encodeURIComponent(du)}&au=${encodeURIComponent(au)}`);
        return {
            bilans: enTableau(response.bilans).map((bilan) => ({ ...bilan, emotions: enTableau(bilan.emotions) })),
            joursRegles: enTableau(response.joursRegles),
        };
    }

    async getAllMedicaments(carnetSanteId: number, ): Promise<any> {
        return this.request('GET', `Medicament/by-carnet-sante/${carnetSanteId}`);
    }
    
    async getJoursReglesByMonth(carnetSanteId: number, month: number, year: number): Promise<any> {
        return this.request('GET', `JourRegle/ByMonth/${carnetSanteId}/${month}/${year}`);
    }
    
    async getSymptomesByMonth(carnetSanteId: number, month: number, year: number): Promise<any> {
        return this.request('GET', `SymptomesCycle/${carnetSanteId}/${month}/${year}`);
    }
    
    // POST
    async postDonneesDouleurs(donneesDouleurs: any): Promise<any> {
        return this.request('POST', 'DonneesDouleurs', donneesDouleurs);
    }

    async postDonneesActivitePhysique(donneesActivitePhysique: any): Promise<any> {
        return this.request('POST', 'DonneesActivitePhysique', donneesActivitePhysique);
    }

    async postBilanQuotidien(bilanQuotidien: BilanQuotidienSaisie): Promise<{ id: number }> {
        return this.request('POST', 'BilanQuotidien', bilanQuotidien);
    }

    async putBilanQuotidien(bilanQuotidien: BilanQuotidienSaisie): Promise<void> {
        return this.request('PUT', `BilanQuotidien/${bilanQuotidien.id}`, bilanQuotidien);
    }

    async postMedicament(donneesMedicament: any): Promise<any> {
        return this.request('POST', 'Medicament', donneesMedicament);
    }

    async postDonneesPriseMedicament(donneesPriseMedicament: any): Promise<any> {
        return this.request('POST', 'DonneesMedicament', donneesPriseMedicament);
    }

    async postDonneesTraitementNonMedicamenteux(donneesTraitement: any): Promise<any> {
        return this.request('POST', 'DonneesTraitementNonMedicamenteux', donneesTraitement);
    }

    async postDonneesTransit(donneesTransit: any): Promise<any> {
        return this.request('POST', 'DonneesTransit', donneesTransit);
    }

    async postJourRegle(jourRegle: any): Promise<any> {
        return this.request('POST', 'JourRegle', jourRegle);
    }
    
    async postDonneesSymptomesCycle(symptomesCycle: any): Promise<any> {
        return this.request('POST', 'SymptomesCycle', symptomesCycle);
    }

    
    // DELETE

    async deleteDonneesActivitePhysique(donneesActivitePhysiqueId: number): Promise<any> {
        return this.request('DELETE', `DonneesActivitePhysique/${donneesActivitePhysiqueId}`);
    }

    async deleteDonneesDouleurs(donneesDouleursId: number): Promise<any> {
        return this.request('DELETE', `DonneesDouleurs/${donneesDouleursId}`);
    }

    async deleteDonneesMedicament(donneesMedicamentId: number): Promise<any> {
        return this.request('DELETE', `DonneesMedicament/${donneesMedicamentId}`);
    }

    async deleteDonneesTraitementNonMedicamenteux(donneesTraitementId: number): Promise<any> {
        return this.request('DELETE', `DonneesTraitementNonMedicamenteux/${donneesTraitementId}`);
    }

    async deleteDonneesTransit(donneesTransitId: number): Promise<any> {
        return this.request('DELETE', `DonneesTransit/${donneesTransitId}`);
    }
    
    async deleteSymptomeCycle(symptomeId: number): Promise<any> {
        return this.request('DELETE', `SymptomesCycle/${symptomeId}`);
    }

    async deleteJourRegle(jourRegleId: number): Promise<any> {
        return this.request('DELETE', `JourRegle/${jourRegleId}`);
    }

    async deleteMedicament(medicamentId: number): Promise<any> {
        return this.request('DELETE', `Medicament/${medicamentId}`);
    }

    // PUT

    async putDonneesMedicament(medicamentId: number, donneesMedicament: any): Promise<any> {
        return this.request('PUT', `Medicament/${medicamentId}`, donneesMedicament);
    }
    
    async editDonneesDouleurs(donneesDouleursId: number, donneesDouleurs: DonneesDouleurModification): Promise<void> {
        return this.request('PUT', `DonneesDouleurs/${donneesDouleursId}`, donneesDouleurs);
    }

    async editDonneesActivitePhysique(donneesActivitePhysiqueId: number, donneesActivitePhysique: any): Promise<any> {
        return this.request('PUT', `DonneesActivitePhysique/${donneesActivitePhysiqueId}`, donneesActivitePhysique);
    }

    async editSymptomeCycle(symptomeCycleId: number, symptomeCycle: any): Promise<any> {
        return this.request('PUT', `SymptomesCycle/${symptomeCycleId}`, symptomeCycle);
    }

    // NOTIFICATIONS WEB PUSH

    async getClePubliquePush(): Promise<string> {
        const response = await this.request<{ clePublique: string }>('GET', 'Notifications/cle-publique');
        return response.clePublique;
    }

    async abonnerAppareilPush(abonnement: AbonnementPush): Promise<void> {
        return this.request('POST', 'Notifications/abonnements', abonnement);
    }

    async desabonnerAppareilPush(endpoint: string): Promise<void> {
        return this.request('DELETE', 'Notifications/abonnements', { endpoint });
    }

    async getRappels(): Promise<Rappel[]> {
        const response = await this.request<Rappel[] | { $values: Rappel[] }>('GET', 'Notifications/rappels');
        return Array.isArray(response) ? response : response.$values;
    }

    async putRappel(type: TypeRappel, reglage: ReglageRappel): Promise<void> {
        return this.request('PUT', `Notifications/rappels/${type}`, reglage);
    }

    async envoyerNotificationTest(): Promise<void> {
        return this.request('POST', 'Notifications/test');
    }
}

export default new ApiService();