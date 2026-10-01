export interface CritereMotDePasse {
    cle: string;
    libelle: string;
    respecte: (motDePasse: string) => boolean;
}

/** Les règles d'Identity côté serveur (`PasswordRequires*`) : les montrer pendant la saisie évite un refus au moment de valider. */
export const CRITERES_MOT_DE_PASSE: CritereMotDePasse[] = [
    { cle: 'longueur', libelle: '8 caractères au moins', respecte: (m) => m.length >= 8 },
    { cle: 'casse', libelle: 'Majuscule et minuscule', respecte: (m) => /[a-z]/.test(m) && /[A-Z]/.test(m) },
    { cle: 'chiffre', libelle: 'Un chiffre', respecte: (m) => /\d/.test(m) },
    { cle: 'special', libelle: 'Un caractère spécial', respecte: (m) => /[^A-Za-z0-9]/.test(m) },
];

export const motDePasseValide = (motDePasse: string): boolean => CRITERES_MOT_DE_PASSE.every((critere) => critere.respecte(motDePasse));

/** Adresse plausible (le serveur reste juge) : un texte, un @, un texte, un point, un texte. */
export const emailPlausible = (email: string): boolean => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());
