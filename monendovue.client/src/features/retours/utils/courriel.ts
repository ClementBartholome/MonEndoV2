import type { TypeDeRetour } from '../config/types-de-retour';

/** Ce qui aide à comprendre un retour sans rien révéler du suivi : aucune donnée de santé, aucun identifiant de compte. */
export interface ContexteDeRetour {
  /** Page d'où l'on vient (chemin seulement), ou null si la page a été ouverte directement. */
  page: string | null;
  version: string;
  appareil: string;
}

const SAUT_DE_LIGNE = '\r\n';

/** Corps du message : les lignes à compléter, puis le contexte technique, que l'utilisatrice peut effacer. */
export function corpsDuMessage(type: TypeDeRetour, contexte: ContexteDeRetour): string {
  return [
    'Bonjour,',
    '',
    ...type.invites.flatMap((invite) => [invite, '']),
    '— Informations utiles (tu peux les laisser) —',
    `Page : ${contexte.page ?? 'ouverte directement'}`,
    `Version de MonEndo : ${contexte.version}`,
    `Appareil : ${contexte.appareil}`,
  ].join(SAUT_DE_LIGNE);
}

/** Lien `mailto:` avec l'objet et le corps préremplis ; l'adresse de l'éditeur vient de la configuration, jamais d'une saisie. */
export function lienMailto(adresse: string, type: TypeDeRetour, contexte: ContexteDeRetour): string {
  const parametres = `subject=${encodeURIComponent(type.sujet)}&body=${encodeURIComponent(corpsDuMessage(type, contexte))}`;
  return `mailto:${adresse}?${parametres}`;
}
