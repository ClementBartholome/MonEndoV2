/**
 * Sources de la page « S'informer » : des liens vers des sites publics, aucun contenu médical maison.
 * Un lien de plus = une entrée ici. Chaque lien doit figurer dans `.claude/skills/endometriose/sources.md`.
 * Les adresses d'Ameli changent parfois : revérifier les liens à chaque version.
 */
export interface SourceInformation {
  titre: string;
  /** Organisme, suivi d'une précision sur le contenu. */
  origine: string;
  detail: string;
  url: string;
  /** Langue du contenu quand ce n'est pas le français. */
  langue?: 'en';
}

export interface GroupeSources {
  id: string;
  titre: string;
  sources: SourceInformation[];
}

/** Date de consultation des sources, affichée sur la page. */
export const SOURCES_CONSULTEES_LE = '27 septembre 2026';

export const GROUPES_SOURCES: GroupeSources[] = [
  {
    id: 'comprendre',
    titre: 'Comprendre la maladie',
    sources: [
      { titre: 'Symptômes, diagnostic et évolution', origine: 'Ameli', detail: "la fiche de l'Assurance maladie", url: 'https://www.ameli.fr/assure/sante/themes/endometriose/symptomes-diagnostic-evolution' },
      { titre: "Le diagnostic de l'endométriose", origine: 'EndoFrance', detail: "l'association de patientes", url: 'https://www.endofrance.org/diagnostic-endometriose/' },
      { titre: "Traitement de l'endométriose", origine: 'Ameli', detail: 'les différentes approches', url: 'https://www.ameli.fr/assure/sante/themes/endometriose/traitement' },
      { titre: 'Endometriosis', origine: 'OMS', detail: "fiche d'information, en anglais", url: 'https://www.who.int/news-room/fact-sheets/detail/endometriosis', langue: 'en' },
    ],
  },
  {
    id: 'consultation',
    titre: 'Parler de ta douleur en consultation',
    sources: [
      { titre: "Évaluer la douleur due à l'endométriose", origine: 'Santé.fr', detail: 'types de douleur et échelles', url: 'https://www.sante.fr/endometriose/evaluer-la-douleur-due-lendometriose' },
      { titre: "Prise en charge de l'endométriose", origine: 'HAS', detail: 'recommandations pour les soignants (2017)', url: 'https://www.has-sante.fr/jcms/c_2820459/fr/prise-en-charge-de-l-endometriose-recommandations' },
    ],
  },
  {
    id: 'quotidien',
    titre: 'Vivre au quotidien et tes droits',
    sources: [
      { titre: 'Suivi médical et vie quotidienne', origine: 'Ameli', detail: 'activité, alimentation, soutien psy, travail (RQTH)', url: 'https://www.ameli.fr/assure/sante/themes/endometriose/suivi-medical-vie-quotidienne' },
      { titre: 'ALD : prise en charge des formes invalidantes', origine: 'Santé.fr', detail: 'conditions et démarches', url: 'https://www.sante.fr/endometriose/prise-en-charge-de-lendometriose-ald' },
    ],
  },
  {
    id: 'associations',
    titre: 'Associations et recherche',
    sources: [
      { titre: 'EndoFrance', origine: 'Association', detail: 'écoute, groupes et informations', url: 'https://www.endofrance.org/' },
      { titre: "Fondation pour la Recherche sur l'Endométriose", origine: 'Fondation', detail: 'qualité de vie et santé mentale', url: 'https://www.fondation-endometriose.org/endometriose/qualite-de-vie/' },
    ],
  },
];
