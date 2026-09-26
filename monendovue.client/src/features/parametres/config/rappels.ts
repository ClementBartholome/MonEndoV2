import type { TypeRappel } from '@/features/parametres/types/notifications';

export interface PresentationRappel {
  titre: string;
  description: string;
  icone: string;
}

/** Libellés des rappels proposés par l'API ; ajouter un type de rappel = ajouter une entrée ici (et sa règle côté serveur). */
export const presentationRappels: Record<TypeRappel, PresentationRappel> = {
  BilanQuotidien: {
    titre: 'Rappel du bilan quotidien',
    description: "Envoyé seulement si ton bilan du jour n'est pas encore rempli.",
    icone: 'fact_check',
  },
  SuiviAcne: {
    titre: 'Rappel photo du suivi acné',
    description: "Une fois par semaine, seulement si tu n'as pas ajouté de photo d'acné depuis 7 jours.",
    icone: 'photo_camera',
  },
};

/** Jours dans l'ordre français (lundi → dimanche) ; valeur 0 = dimanche, comme côté serveur. */
export const joursSemaine: { valeur: number; libelle: string }[] = [
  { valeur: 1, libelle: 'Lundi' },
  { valeur: 2, libelle: 'Mardi' },
  { valeur: 3, libelle: 'Mercredi' },
  { valeur: 4, libelle: 'Jeudi' },
  { valeur: 5, libelle: 'Vendredi' },
  { valeur: 6, libelle: 'Samedi' },
  { valeur: 0, libelle: 'Dimanche' },
];
