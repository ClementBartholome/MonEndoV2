/**
 * Après un déploiement, une app restée ouverte (onglet, app de l'écran d'accueil) réclame encore les fichiers de l'ancienne
 * version, chargés à la demande (pages, jsPDF) : ils n'existent plus et l'action échoue (« Le PDF n'a pas pu être créé »).
 * Vite le signale par l'évènement `vite:preloadError` : on recharge alors la page une fois, pour passer à la nouvelle version.
 * Pas de rechargement hors connexion ni deux fois de suite en quelques secondes (vraie panne : l'erreur suit son cours).
 */
const CLE = 'monendo-rechargement-mise-a-jour';
export const DELAI_ENTRE_RECHARGEMENTS_MS = 10_000;

function dernierRechargement(): number {
    try {
        return Number(sessionStorage.getItem(CLE) ?? 0);
    } catch {
        return 0;
    }
}

function noterRechargement(maintenant: number) {
    try {
        sessionStorage.setItem(CLE, String(maintenant));
    } catch {
        // Sans stockage, le garde-fou repose sur le délai de la page rechargée.
    }
}

export function rechargerApresMiseAJour(fenetre: Window = window) {
    fenetre.addEventListener('vite:preloadError', (evenement) => {
        const maintenant = Date.now();
        if (!fenetre.navigator.onLine || maintenant - dernierRechargement() < DELAI_ENTRE_RECHARGEMENTS_MS) return;
        evenement.preventDefault();
        noterRechargement(maintenant);
        fenetre.location.reload();
    });
}
