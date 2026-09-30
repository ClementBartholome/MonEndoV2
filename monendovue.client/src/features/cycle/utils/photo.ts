/** Règles d'une photo de symptôme (suivi de l'acné) avant l'envoi : formats, tailles, nom de fichier. */

/** Taille visée après compression (le serveur accepte plus, mais l'envoi reste rapide en 4G). */
export const TAILLE_MAX_PHOTO = 900 * 1024;
export const LIBELLE_TAILLE_MAX_PHOTO = '900 Ko';
/** Au-delà, le fichier n'est même pas lu (photo d'appareil anormale ou mauvais fichier). */
export const TAILLE_MAX_FICHIER = 20 * 1024 * 1024;

const EXTENSIONS_ACCEPTEES = ['.jpg', '.jpeg', '.png', '.webp', '.heic', '.heif'];

const EXTENSION_PAR_TYPE: Record<string, string> = {
    'image/jpeg': '.jpg',
    'image/jpg': '.jpg',
    'image/pjpeg': '.jpg',
    'image/png': '.png',
    'image/webp': '.webp',
    'image/heic': '.heic',
    'image/heic-sequence': '.heic',
    'image/heif': '.heif',
    'image/heif-sequence': '.heif',
};

function extension(nom: string): string {
    const index = nom.lastIndexOf('.');
    return index < 0 ? '' : nom.substring(index).toLowerCase();
}

function extensionDuType(type: string): string {
    return EXTENSION_PAR_TYPE[type.trim().toLowerCase()] ?? '';
}

export function estImageAcceptee(fichier: File): boolean {
    return EXTENSIONS_ACCEPTEES.includes(extension(fichier.name)) || extensionDuType(fichier.type) !== '';
}

/** Nom envoyé au serveur : toujours avec une extension, qu'il utilise pour valider le format. */
export function nomDeFichierPhoto(fichier: File): string {
    const base = fichier.name.replace(/\.[^.]+$/, '').trim() || `photo-${Date.now()}`;
    return `${base}${extension(fichier.name) || extensionDuType(fichier.type) || '.jpg'}`;
}

export function tailleLisible(octets: number): string {
    if (octets < 1024 * 1024) return `${Math.max(1, Math.round(octets / 1024))} Ko`;
    return `${(octets / (1024 * 1024)).toLocaleString('fr-FR', { maximumFractionDigits: 1 })} Mo`;
}
