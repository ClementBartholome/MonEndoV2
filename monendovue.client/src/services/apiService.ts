import axios, { AxiosError } from 'axios';
import type { AxiosInstance, AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/store/auth';
import type { Pinia } from 'pinia';
import { tokenService } from '@/services/tokenService';
import router from "@/router";

const API_URL = import.meta.env.MODE === 'production' ? import.meta.env.VITE_API_URL_PROD : import.meta.env.VITE_API_URL;

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
    private async request<T>(method: string, url: string, data?: any): Promise<T | null> {
        try {
            const tokenExpired = this.isTokenExpired();
            if (tokenExpired) {
                try {
                    await tokenService.refreshToken();
                } catch (error) {
                    console.error('Error refreshing token:', error);
                    router.push({ name: 'login' });
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
            return null;
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

    async getDonneesTransitByMonth(carnetSanteId: number, month: number, year: number): Promise<any> {
        return this.request('GET', `DonneesTransit/${carnetSanteId}/${month}/${year}`);
    }

    async getBilanQuotidienByWeek(carnetSanteId: number, week: string, year: string): Promise<any> {
        return this.request('GET', `BilanQuotidien/by-week/${carnetSanteId}/${week}/${year}`);
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

    async postBilanQuotidien(bilanQuotidien: any): Promise<any> {
        return this.request('POST', 'BilanQuotidien', bilanQuotidien);
    }

    async postMedicament(donneesMedicament: any): Promise<any> {
        return this.request('POST', 'Medicament', donneesMedicament);
    }

    async postDonneesPriseMedicament(donneesPriseMedicament: any): Promise<any> {
        return this.request('POST', 'DonneesMedicament', donneesPriseMedicament);
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

    async saveDeviceToken(deviceToken: string): Promise<any> {
        return this.request('POST', 'Account/device-token', { deviceToken });
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

    async deleteDonneesTransit(donneesTransitId: number): Promise<any> {
        return this.request('DELETE', `DonneesTransit/${donneesTransitId}`);
    }
    
    async deleteSymptomeCycle(symptomeId: number): Promise<any> {
        return this.request('DELETE', `SymptomesCycle/${symptomeId}`);
    }
    
    // PUT

    async putDonneesMedicament(medicamentId: number, donneesMedicament: any): Promise<any> {
        return this.request('PUT', `Medicament/${medicamentId}`, donneesMedicament);
    }
    
    async editDonneesDouleurs(donneesDouleursId: number, donneesDouleurs: any): Promise<any> {
        return this.request('PUT', `DonneesDouleurs/${donneesDouleursId}`, donneesDouleurs);
    }

}

export default new ApiService();