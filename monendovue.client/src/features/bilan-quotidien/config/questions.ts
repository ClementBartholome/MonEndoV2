import type { CategorieBilan } from '@/features/bilan-quotidien/config/categories';
import type { ReponsesBilan } from '@/features/bilan-quotidien/types/bilan-quotidien';

export type ChampReponse = keyof ReponsesBilan;

export interface OptionChoix {
  valeur: string;
  libelle: string;
}

/** Question qui s'ouvre quand la réponse est « Oui » (intensité, abondance). */
export interface SuiteQuestion {
  champ: ChampReponse;
  libelle: string;
  options: OptionChoix[];
  /** Le serveur exige cette réponse dès que la question principale est « Oui » (intensité) ; sinon elle est facultative. */
  requise: boolean;
}

export interface QuestionBilan {
  champ: ChampReponse;
  libelle: string;
  /** Précision affichée sous la question. */
  aide?: string;
  genre: 'ouiNon' | 'choix';
  /** Pour `choix`. */
  options?: OptionChoix[];
  suite?: SuiteQuestion;
  /** Libellé court dans le récapitulatif d'un jour. */
  recap: string;
}

const intensites: OptionChoix[] = [
  { valeur: 'Légère', libelle: 'Légère' },
  { valeur: 'Modérée', libelle: 'Modérée' },
  { valeur: 'Forte', libelle: 'Forte' },
];

const intensite = (champ: ChampReponse): SuiteQuestion => ({ champ, libelle: 'Intensité', options: intensites, requise: true });

/**
 * Libellés non médicaux issus de carnets de suivi publics (voir `.claude/skills/endometriose/sources.md`) : décrire, jamais interpréter.
 * Ajouter une question = une entrée ici, le champ dans les types, côté serveur le modèle, le validateur et la synthèse.
 */
export const questionsTransitSuite: QuestionBilan[] = [
  { champ: 'douleurSelle', libelle: 'Une douleur en allant à la selle ?', genre: 'ouiNon', suite: intensite('intensiteDouleurSelle'), recap: 'Douleur en allant à la selle' },
  { champ: 'nausees', libelle: 'Des nausées ?', genre: 'ouiNon', recap: 'Nausées' },
  { champ: 'sangSelles', libelle: 'Des traces de sang dans les selles ?', genre: 'ouiNon', recap: 'Traces de sang dans les selles' },
];

export interface GroupeQuestions {
  /** Phrase d'introduction du bloc. */
  intro?: string;
  questions: QuestionBilan[];
}

export const groupesDeQuestions: Partial<Record<CategorieBilan, GroupeQuestions>> = {
  urinaire: {
    intro: 'Ces repères servent à en parler en consultation. Tu choisis ce que tu notes.',
    questions: [
      { champ: 'douleurUriner', libelle: 'Une douleur en urinant ?', genre: 'ouiNon', suite: intensite('intensiteDouleurUriner'), recap: 'Douleur en urinant' },
      { champ: 'enviesUrinaires', libelle: "Des envies d'uriner fréquentes ou pressantes ?", genre: 'ouiNon', recap: 'Envies fréquentes ou pressantes' },
      { champ: 'difficulteVider', libelle: 'Du mal à vider complètement la vessie ?', genre: 'ouiNon', recap: 'Difficulté à vider la vessie' },
      {
        champ: 'sangUrines', libelle: 'Du sang visible dans les urines ?', genre: 'ouiNon', recap: 'Sang visible dans les urines',
        aide: "Pendant les règles, ce n'est pas toujours facile à distinguer : note ce que tu observes.",
      },
    ],
  },
  saignements: {
    questions: [
      {
        champ: 'saignementsHorsRegles', libelle: 'Des saignements en dehors de tes règles ?', genre: 'ouiNon', recap: 'Saignements hors règles',
        aide: "Les jours de règles se notent dans l'onglet Règles.",
        suite: {
          champ: 'abondanceSaignementsHorsRegles', libelle: 'Quelle abondance ?', requise: false,
          options: [
            { valeur: 'Traces', libelle: 'Traces' },
            { valeur: 'Legers', libelle: 'Légers' },
            { valeur: 'Abondants', libelle: 'Abondants' },
          ],
        },
      },
    ],
  },
  'nuit-journee': {
    questions: [
      {
        champ: 'nuit', libelle: 'Comment était ta nuit ?', aide: 'La nuit avant cette journée.', genre: 'choix', recap: 'Nuit',
        options: [
          { valeur: 'Bonne', libelle: 'Bonne' },
          { valeur: 'Moyenne', libelle: 'Moyenne' },
          { valeur: 'Difficile', libelle: 'Difficile' },
        ],
      },
      { champ: 'reveilsDouleur', libelle: 'Des réveils à cause de la douleur ?', genre: 'ouiNon', recap: 'Réveils à cause de la douleur' },
      {
        champ: 'limitationJournee', libelle: 'Tes symptômes ont-ils limité ta journée ?', genre: 'choix', recap: 'Journée',
        options: [
          { valeur: 'PasLimitee', libelle: 'Pas limitée' },
          { valeur: 'PeuLimitee', libelle: 'Un peu limitée' },
          { valeur: 'TresLimitee', libelle: 'Très limitée' },
        ],
      },
      { champ: 'absenceTravail', libelle: 'Une absence au travail ou en cours, même partielle ?', genre: 'ouiNon', recap: 'Absence au travail ou en cours' },
      { champ: 'activiteAnnulee', libelle: 'Une activité annulée ou reportée ?', genre: 'ouiNon', recap: 'Activité annulée ou reportée' },
    ],
  },
  rapports: {
    intro: "Catégorie discrète : elle n'apparaît que si tu l'actives dans les paramètres du bilan.",
    questions: [
      {
        champ: 'douleurRapport', libelle: 'Une douleur pendant ou après un rapport ?', genre: 'choix', recap: 'Douleur pendant ou après un rapport',
        options: [
          { valeur: 'Oui', libelle: 'Oui' },
          { valeur: 'Non', libelle: 'Non' },
          { valeur: 'PasDeRapport', libelle: 'Pas de rapport' },
        ],
      },
    ],
  },
};

/** Toutes les questions du bilan hors mesures, transit compris : pour contrôler la saisie et vider une suite devenue sans objet. */
export const toutesLesQuestions: QuestionBilan[] = [
  ...questionsTransitSuite,
  ...Object.values(groupesDeQuestions).flatMap((groupe) => groupe?.questions ?? []),
];

type Reponses = Partial<Record<ChampReponse, unknown>>;

const estRenseigne = (valeur: unknown): boolean => valeur !== null && valeur !== undefined;

/** Nombre de questions auxquelles la personne a répondu (une suite n'est pas comptée à part). */
export const nombreDeReponses = (questions: QuestionBilan[], reponses: Reponses): number =>
  questions.filter((q) => estRenseigne(reponses[q.champ])).length;

/** Résumé d'un bloc replié : neutre, jamais le contenu des réponses (l'écran peut être vu par d'autres). */
export const resumeDesReponses = (questions: QuestionBilan[], reponses: Reponses): string => {
  const n = nombreDeReponses(questions, reponses);
  if (n === 0) return 'Rien de noté';
  return `${n} ${n > 1 ? 'réponses notées' : 'réponse notée'}`;
};

/** Première question « Oui » dont l'intensité exigée par le serveur manque encore. */
export const suiteManquante = (questions: QuestionBilan[], reponses: Reponses): QuestionBilan | undefined =>
  questions.find((q) => q.suite?.requise && reponses[q.champ] === true && !estRenseigne(reponses[q.suite.champ]));

export interface LigneRecap {
  libelle: string;
  valeur: string;
}

/** Réponses données à un groupe de questions, pour le récapitulatif d'un jour (rien pour ce qui n'est pas renseigné). */
export const lignesDeRecap = (questions: QuestionBilan[], reponses: Reponses): LigneRecap[] =>
  questions.flatMap((q) => {
    const reponse = reponses[q.champ];
    if (!estRenseigne(reponse)) return [];
    if (q.genre === 'choix') {
      const option = q.options?.find((o) => o.valeur === reponse);
      return [{ libelle: q.recap, valeur: option?.libelle ?? String(reponse) }];
    }
    if (reponse === false) return [{ libelle: q.recap, valeur: 'Non' }];
    const suite = q.suite ? reponses[q.suite.champ] : null;
    const precision = q.suite?.options.find((o) => o.valeur === suite)?.libelle.toLowerCase();
    return [{ libelle: q.recap, valeur: precision ? `Oui · ${precision}` : 'Oui' }];
  });
