import { ref } from 'vue';
import apiService from '@/shared/services/apiService';
import type { EtatNotifications, Rappel, ReglageRappel, TypeRappel } from '@/features/parametres/types/notifications';

/** Service worker dédié au push (aucun cache) ; voir public/push-sw.js. */
const CHEMIN_SERVICE_WORKER = '/push-sw.js';

const MESSAGE_ECHEC_ACTIVATION =
    "L'activation des notifications a échoué. Sur iPhone, ouvre MonEndo depuis l'icône de l'écran d'accueil.";

export const estIos = (): boolean =>
    /iPad|iPhone|iPod/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);

export const estAppInstallee = (): boolean =>
    window.matchMedia('(display-mode: standalone)').matches ||
    (navigator as Navigator & { standalone?: boolean }).standalone === true;

const pushSupporte = (): boolean =>
    'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;

export const fuseauLocal = (): string => Intl.DateTimeFormat().resolvedOptions().timeZone || 'Europe/Paris';

/** Clé publique VAPID (base64url) convertie pour pushManager.subscribe. */
export const cleEnOctets = (base64Url: string): Uint8Array => {
    const padding = '='.repeat((4 - (base64Url.length % 4)) % 4);
    const base64 = (base64Url + padding).replace(/-/g, '+').replace(/_/g, '/');
    return Uint8Array.from(atob(base64), (caractere) => caractere.charCodeAt(0));
};

const memeCle = (cleAbonnement: ArrayBuffer | null, clePublique: Uint8Array): boolean => {
    if (!cleAbonnement) return false;
    const octets = new Uint8Array(cleAbonnement);
    return octets.length === clePublique.length && octets.every((valeur, index) => valeur === clePublique[index]);
};

export function usePushNotifications() {
    const etat = ref<EtatNotifications>('chargement');
    const enCours = ref(false);
    const rappels = ref<Rappel[]>([]);
    let clePublique: Uint8Array | null = null;

    const abonnementCourant = async (): Promise<PushSubscription | null> => {
        const registration = await navigator.serviceWorker.getRegistration('/');
        return (await registration?.pushManager.getSubscription()) ?? null;
    };

    const enregistrerAbonnement = async (abonnement: PushSubscription): Promise<void> => {
        const json = abonnement.toJSON();
        await apiService.abonnerAppareilPush({
            endpoint: abonnement.endpoint,
            p256dh: json.keys?.p256dh ?? '',
            auth: json.keys?.auth ?? '',
        });
    };

    const chargerRappels = async (): Promise<void> => {
        rappels.value = await apiService.getRappels();
    };

    const initialiser = async (): Promise<void> => {
        if (!pushSupporte()) {
            etat.value = estIos() && !estAppInstallee() ? 'ios-a-installer' : 'non-supporte';
            return;
        }

        try {
            clePublique = cleEnOctets(await apiService.getClePubliquePush());
        } catch {
            etat.value = 'non-disponible';
            return;
        }

        if (Notification.permission === 'denied') {
            etat.value = 'refuse';
            return;
        }

        const abonnement = await abonnementCourant();
        if (abonnement && Notification.permission === 'granted' && memeCle(abonnement.options.applicationServerKey, clePublique)) {
            // Resynchronise l'appareil au cas où le serveur l'aurait retiré (abonnement expiré, changement de compte).
            await enregistrerAbonnement(abonnement);
            await chargerRappels();
            etat.value = 'actif';
        } else {
            etat.value = 'inactif';
        }
    };

    /** À appeler directement depuis un clic : iOS n'autorise la demande de permission qu'après un geste. */
    const activer = async (): Promise<void> => {
        if (!clePublique) return;
        enCours.value = true;
        try {
            const permission = await Notification.requestPermission();
            if (permission !== 'granted') {
                etat.value = permission === 'denied' ? 'refuse' : 'inactif';
                return;
            }

            const registration = await navigator.serviceWorker.register(CHEMIN_SERVICE_WORKER, { scope: '/' });
            await navigator.serviceWorker.ready;

            // Un ancien abonnement (autre clé, ex. OneSignal) empêche de s'abonner avec la nouvelle clé.
            let abonnement = await registration.pushManager.getSubscription();
            if (abonnement && !memeCle(abonnement.options.applicationServerKey, clePublique)) {
                await abonnement.unsubscribe();
                abonnement = null;
            }
            abonnement ??= await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: clePublique,
            });

            await enregistrerAbonnement(abonnement);
            await chargerRappels();
            // À la première activation, seul le rappel du bilan s'active ; les autres se règlent dans la liste.
            const bilan = rappels.value.find((rappel) => rappel.type === 'BilanQuotidien');
            if (bilan && !bilan.actif) {
                await enregistrerRappel('BilanQuotidien', { actif: true });
            }
            etat.value = 'actif';
        } catch (error) {
            console.error('Push activation error:', error);
            throw new Error(MESSAGE_ECHEC_ACTIVATION);
        } finally {
            enCours.value = false;
        }
    };

    const desactiver = async (): Promise<void> => {
        enCours.value = true;
        try {
            const abonnement = await abonnementCourant();
            if (abonnement) {
                await apiService.desabonnerAppareilPush(abonnement.endpoint);
                await abonnement.unsubscribe();
            }
            etat.value = 'inactif';
        } finally {
            enCours.value = false;
        }
    };

    const enregistrerRappel = async (type: TypeRappel, modifications: Partial<ReglageRappel>): Promise<void> => {
        const rappel = rappels.value.find((r) => r.type === type);
        if (!rappel) return;
        const nouveau: Rappel = { ...rappel, ...modifications, fuseauHoraire: fuseauLocal() };
        await apiService.putRappel(type, {
            actif: nouveau.actif,
            heure: nouveau.heure,
            jourSemaine: nouveau.jourSemaine,
            fuseauHoraire: nouveau.fuseauHoraire,
        });
        rappels.value = rappels.value.map((r) => (r.type === type ? nouveau : r));
    };

    const envoyerTest = (): Promise<void> => apiService.envoyerNotificationTest();

    return { etat, enCours, rappels, initialiser, activer, desactiver, enregistrerRappel, envoyerTest };
}
