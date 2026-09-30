/**
 * Adresse de base de l'API : le même serveur dans l'image Docker (chemins absolus, justes quelle que soit la page, ex.
 * /medicaments/12), sinon l'adresse configurée. Une seule définition pour tous les appels et pour les images servies par l'API.
 */
export const API_URL: string = import.meta.env.VITE_DOCKER === 'true'
    ? '/'
    : (import.meta.env.MODE === 'production' ? import.meta.env.VITE_API_URL_PROD : import.meta.env.VITE_API_URL);

/** Adresse complète d'un chemin de l'API (ex. une photo servie par `Acne/photos/12`). */
export const urlDeApi = (chemin: string): string => `${API_URL}${chemin}`;
