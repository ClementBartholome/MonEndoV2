/**
 * Questions à poser au médecin, notées avant la consultation : gardées sur cet appareil seulement (jamais envoyées au
 * serveur), pour les retrouver d'une visite à l'autre, et effacées à la déconnexion.
 */
const CLE = 'monendo.questions-rendez-vous';

export const QUESTIONS_MAX = 1000;

export const lireQuestions = (): string => localStorage.getItem(CLE) ?? '';

export const enregistrerQuestions = (texte: string): void => {
    if (texte.trim()) localStorage.setItem(CLE, texte.slice(0, QUESTIONS_MAX));
    else localStorage.removeItem(CLE);
};

export const effacerQuestions = (): void => localStorage.removeItem(CLE);
