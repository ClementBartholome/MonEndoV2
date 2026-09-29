import axios from 'axios';
import type { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { useAuthStore } from '@/features/auth/store/auth';
import type { Pinia } from 'pinia';
import { tokenService } from '@/features/auth/services/tokenService';
import router from "@/router";
import type { DonneesDouleurModification } from '@/features/douleurs/types/donnees-douleur';
import type { AbonnementPush, Rappel, ReglageRappel, TypeRappel } from '@/features/parametres/types/notifications';
import type { BilanQuotidien, BilanQuotidienSaisie, EmotionBilan } from '@/features/bilan-quotidien/types/bilan-quotidien';
import type { HistoriqueBilans } from '@/features/bilan-quotidien/types/historique';
import type { EvenementAgenda } from '@/features/schedule/types/agenda';
import type { Aujourdhui, BilanAujourdhui } from '@/features/accueil/types/aujourdhui';
import type { PrisePrevue, PriseSaisie, SeanceSaisie, Soin, Traitement, TraitementAuBesoin, TraitementsDuJour, TraitementSaisie } from '@/features/medicament/types/traitements';

/** Liste C# sérialisée avec ReferenceHandler.Preserve. */
type Liste<T> = T[] | { $values: T[] };
import { enTableau } from '@/shared/utils/json';
import type { ReponseConsentement } from '@/features/auth/types/user';
import type { CycleDuMois, CycleTermine } from '@/features/cycle/types/cycle';
import type { Activite, ActiviteSaisie } from '@/features/activite/types/activite';
import type { Acne, EpisodeAcne, EpisodeAcneSaisie, SuiviAcne } from '@/features/cycle/types/acne';
import type { SymptomeCycle, SymptomeSaisie } from '@/features/cycle/types/symptome-cycle';
import { nomDeFichierPhoto } from '@/features/cycle/utils/photo';

/** Formulaire multipart d'un symptôme (le carnet est vérifié par le serveur ; l'adresse de la photo n'est jamais envoyée). */
function formulaireSymptome(carnetSanteId: number, saisie: SymptomeSaisie): FormData {
    const formulaire = new FormData();
    formulaire.append('typeSymptome', saisie.typeSymptome);
    formulaire.append('carnetSanteId', String(carnetSanteId));
    formulaire.append('date', saisie.date);
    formulaire.append('intensite', String(saisie.intensite));
    formulaire.append('commentaire', saisie.commentaire ?? '');
    if (saisie.photo) formulaire.append('photo', saisie.photo, nomDeFichierPhoto(saisie.photo));
    if (saisie.photoSource) formulaire.append('photoSource', saisie.photoSource);
    return formulaire;
}

/** Code du 403 renvoyé par l'API quand le consentement aux données de santé manque (ExigeConsentementFilter). */
const CONSENTEMENT_REQUIS = 'consentement-requis';

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
    private async request<T>(method: string, url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
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
                ...config,
                method,
                url,
                data,
            });
            return response.data;
        } catch (error: any) {
            if (error?.response?.status === 403 && error.response.data?.code === CONSENTEMENT_REQUIS) {
                this.authStore?.setConsentement(false);
                router.push({ name: 'consentement' });
            }
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

    async getDonneesCarnetSanteByMonth(carnetSanteId: number, month: number, year: number): Promise<any> {
        return this.request('GET', `CarnetSante/${carnetSanteId}/${month}/${year}`);
    }

    async getDonneesDouleursByMonth(carnetSanteId: number, month: number, year: number): Promise<any> {
        return this.request('GET', `DonneesDouleurs/${carnetSanteId}/${month}/${year}`);
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

    
    async getJoursReglesByMonth(carnetSanteId: number, month: number, year: number): Promise<any> {
        return this.request('GET', `JourRegle/ByMonth/${carnetSanteId}/${month}/${year}`);
    }
    

    /** Symptômes d'un mois (1-12) du carnet, dates locales sans fuseau. */
    async getSymptomesDuMois(carnetSanteId: number, mois: number, annee: number): Promise<SymptomeCycle[]> {
        return enTableau(await this.request<Liste<SymptomeCycle>>('GET', `SymptomesCycle/${carnetSanteId}/${mois}/${annee}`));
    }

    async postSymptome(carnetSanteId: number, saisie: SymptomeSaisie): Promise<void> {
        await this.request('POST', 'SymptomesCycle', formulaireSymptome(carnetSanteId, saisie));
    }

    async putSymptome(id: number, carnetSanteId: number, saisie: SymptomeSaisie): Promise<void> {
        const formulaire = formulaireSymptome(carnetSanteId, saisie);
        formulaire.append('id', String(id));
        await this.request('PUT', `SymptomesCycle/${id}`, formulaire);
    }

    /** Onglet Règles : jours de règles du mois (1er du mois), cycle en cours au jour local et historique des cycles. */
    async getCycle(jour: string, mois: string, cycles = 6): Promise<CycleDuMois> {
        type CycleRecu = Omit<CycleDuMois, 'joursDeRegles' | 'cycles'> & { joursDeRegles: Liste<string>; cycles: Liste<CycleTermine> };
        const recu = await this.request<CycleRecu>('GET', `Cycle?jour=${jour}&mois=${mois}&cycles=${cycles}`);
        return { ...recu, joursDeRegles: enTableau(recu.joursDeRegles), cycles: enTableau(recu.cycles) };
    }

    /** Activités d'un mois (1er du mois, AAAA-MM-JJ), carnet déduit de la session. */
    async getActivites(mois: string): Promise<Activite[]> {
        return enTableau(await this.request<Liste<Activite>>('GET', `Activite?mois=${mois}`));
    }

    async postActivite(saisie: ActiviteSaisie): Promise<{ id: number }> {
        return this.request('POST', 'Activite', saisie);
    }

    async putActivite(id: number, saisie: ActiviteSaisie): Promise<void> {
        await this.request('PUT', `Activite/${id}`, saisie);
    }

    async deleteActivite(id: number): Promise<void> {
        await this.request('DELETE', `Activite/${id}`);
    }

    /** Onglet Acné : épisodes et suivis photo, durée de l'épisode en cours comptée au jour local. */
    /** `mois` : fenêtre des photos de suivi renvoyées (7 derniers mois par défaut). */
    async getAcne(jour: string, mois = 7): Promise<Acne> {
        const recu = await this.request<{ episodes: Liste<EpisodeAcne>; suivis: Liste<SuiviAcne>; suivisPlusAnciens: number }>(
            'GET', `Acne?jour=${jour}&mois=${mois}`);
        return { episodes: enTableau(recu.episodes), suivis: enTableau(recu.suivis), suivisPlusAnciens: recu.suivisPlusAnciens };
    }

    async postEpisodeAcne(saisie: EpisodeAcneSaisie): Promise<{ id: number }> {
        return this.request('POST', 'Acne/episodes', saisie);
    }

    async putEpisodeAcne(id: number, saisie: EpisodeAcneSaisie): Promise<void> {
        await this.request('PUT', `Acne/episodes/${id}`, saisie);
    }

    /** « Ça s'est calmé » : dernier jour (AAAA-MM-JJ) de l'épisode en cours. */
    async postFinEpisodeAcne(id: number, fin: string): Promise<void> {
        await this.request('POST', `Acne/episodes/${id}/fin`, { fin });
    }

    async deleteEpisodeAcne(id: number): Promise<void> {
        await this.request('DELETE', `Acne/episodes/${id}`);
    }

    /** Note un jour de règles (AAAA-MM-JJ) ; sans effet s'il l'est déjà. */
    async putJourDeRegles(jour: string): Promise<void> {
        await this.request('PUT', `Cycle/regles/${jour}`);
    }

    async deleteJourDeRegles(jour: string): Promise<void> {
        await this.request('DELETE', `Cycle/regles/${jour}`);
    }
    
    // POST
    async postDonneesDouleurs(donneesDouleurs: any): Promise<any> {
        return this.request('POST', 'DonneesDouleurs', donneesDouleurs);
    }

    async postBilanQuotidien(bilanQuotidien: BilanQuotidienSaisie): Promise<{ id: number }> {
        return this.request('POST', 'BilanQuotidien', bilanQuotidien);
    }

    async putBilanQuotidien(bilanQuotidien: BilanQuotidienSaisie): Promise<void> {
        return this.request('PUT', `BilanQuotidien/${bilanQuotidien.id}`, bilanQuotidien);
    }

    async postDonneesTransit(donneesTransit: any): Promise<any> {
        return this.request('POST', 'DonneesTransit', donneesTransit);
    }

    

    
    // DELETE

    async deleteDonneesDouleurs(donneesDouleursId: number): Promise<any> {
        return this.request('DELETE', `DonneesDouleurs/${donneesDouleursId}`);
    }

    async deleteDonneesTransit(donneesTransitId: number): Promise<any> {
        return this.request('DELETE', `DonneesTransit/${donneesTransitId}`);
    }
    
    async deleteSymptomeCycle(symptomeId: number): Promise<any> {
        return this.request('DELETE', `SymptomesCycle/${symptomeId}`);
    }

    // PUT

    
    async editDonneesDouleurs(donneesDouleursId: number, donneesDouleurs: DonneesDouleurModification): Promise<void> {
        return this.request('PUT', `DonneesDouleurs/${donneesDouleursId}`, donneesDouleurs);
    }

    // NOTIFICATIONS WEB PUSH

    async postConsentement(): Promise<ReponseConsentement> {
        return this.request<ReponseConsentement>('POST', 'Account/consentement');
    }

    /** Archive ZIP de toutes les données de l'utilisatrice connectée (JSON + photos). */
    async getExportDonnees(): Promise<Blob> {
        return this.request<Blob>('GET', 'DonneesPersonnelles/export', undefined, { responseType: 'blob' });
    }

    /** Suppression définitive du compte et de toutes ses données, confirmée par le mot de passe (corps de la requête). */
    async postSuppressionCompte(password: string): Promise<void> {
        await this.request<void>('POST', 'DonneesPersonnelles/suppression-compte', { password });
    }

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

    // AGENDA

    /** Événements de l'agenda sur la période, ou null si l'utilisatrice n'a pas d'agenda (404). */
    async getEvenementsAgenda(debut: Date, fin: Date): Promise<EvenementAgenda[] | null> {
        const periode = `debut=${encodeURIComponent(debut.toISOString())}&fin=${encodeURIComponent(fin.toISOString())}`;
        return this.agendaOuNull(`Agenda/evenements?${periode}`);
    }

    /** Accueil « Aujourd'hui » : le jour local est envoyé (le serveur ne connaît pas le fuseau de l'utilisatrice). */
    async getAujourdhui(jour: string): Promise<Aujourdhui> {
        type Brut = Omit<Aujourdhui, 'prisesPrevues' | 'auBesoin' | 'bilan'> & {
            prisesPrevues: Liste<PrisePrevue>;
            auBesoin: Liste<TraitementAuBesoin>;
            bilan: (Omit<BilanAujourdhui, 'emotions'> & { emotions: Liste<BilanAujourdhui['emotions'][number]> }) | null;
        };
        const brut = await this.request<Brut>('GET', `Accueil/aujourdhui?jour=${jour}`);
        return {
            ...brut,
            prisesPrevues: enTableau(brut.prisesPrevues),
            auBesoin: enTableau(brut.auBesoin),
            bilan: brut.bilan ? { ...brut.bilan, emotions: enTableau(brut.bilan.emotions) } : null,
        };
    }

    /** Planning du jour local et liste des traitements (carnet de la session). */
    async getTraitementsDuJour(jour: string): Promise<TraitementsDuJour> {
        type TraitementBrut = Omit<Traitement, 'joursSemaine' | 'horaires'> & { joursSemaine: Liste<Traitement['joursSemaine'][number]>; horaires: Liste<string> };
        type Brut = {
            prisesPrevues: Liste<PrisePrevue>; auBesoin: Liste<TraitementAuBesoin>; soins: Liste<Soin>;
            enCours: Liste<TraitementBrut>; termines: Liste<TraitementBrut>;
        };
        const brut = await this.request<Brut>('GET', `Traitements/jour?jour=${jour}`);
        const traitement = (t: TraitementBrut): Traitement => ({ ...t, joursSemaine: enTableau(t.joursSemaine), horaires: enTableau(t.horaires) });
        return {
            prisesPrevues: enTableau(brut.prisesPrevues),
            auBesoin: enTableau(brut.auBesoin),
            soins: enTableau(brut.soins),
            enCours: enTableau(brut.enCours).map(traitement),
            termines: enTableau(brut.termines).map(traitement),
        };
    }

    async postTraitement(saisie: TraitementSaisie): Promise<{ id: number }> {
        return this.request('POST', 'Traitements', saisie);
    }

    async putTraitement(id: number, saisie: TraitementSaisie): Promise<void> {
        await this.request('PUT', `Traitements/${id}`, saisie);
    }

    async postArretTraitement(id: number, jour: string): Promise<void> {
        await this.request('POST', `Traitements/${id}/arret?jour=${jour}`);
    }

    async postPrise(traitementId: number, saisie: PriseSaisie): Promise<{ id: number }> {
        return this.request('POST', `Traitements/${traitementId}/prises`, saisie);
    }

    async deletePrise(priseId: number): Promise<void> {
        await this.request('DELETE', `Traitements/prises/${priseId}`);
    }

    async postSeance(traitementId: number, saisie: SeanceSaisie): Promise<{ id: number }> {
        return this.request('POST', `Traitements/${traitementId}/seances`, saisie);
    }

    /** Trois prochains rendez-vous à heure fixe, ou null si l'utilisatrice n'a pas d'agenda (404). */
    async getProchainsRendezVous(): Promise<EvenementAgenda[] | null> {
        return this.agendaOuNull('Agenda/prochains');
    }

    private async agendaOuNull(url: string): Promise<EvenementAgenda[] | null> {
        try {
            return enTableau(await this.request<EvenementAgenda[] | { $values: EvenementAgenda[] }>('GET', url));
        } catch (error) {
            if (axios.isAxiosError(error) && error.response?.status === 404) return null;
            throw error;
        }
    }
}

export default new ApiService();