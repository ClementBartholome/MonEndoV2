/** Les deux façons d'écrire à l'éditeur depuis la page « Une suggestion ? Un bug ? » : un type de plus = une entrée ici. */
export interface TypeDeRetour {
  /** Objet du message : permet de trier la boîte de réception d'un coup d'œil. */
  sujet: string;
  titre: string;
  detail: string;
  icone: string;
  /** Lignes à compléter, préremplies dans le corps du message. */
  invites: string[];
}

export const TYPES_DE_RETOUR = {
  suggestion: {
    sujet: 'MonEndo — suggestion',
    titre: "J'ai une idée",
    detail: 'Une amélioration, un oubli, ce qui te manque',
    icone: 'lightbulb',
    invites: ['Mon idée :'],
  },
  bug: {
    sujet: 'MonEndo — bug',
    titre: 'Je signale un problème',
    detail: 'Quelque chose ne marche pas comme prévu',
    icone: 'bug_report',
    invites: ['Ce que je faisais :', "Ce que j'attendais :", "Ce qui s'est passé :"],
  },
} as const satisfies Record<string, TypeDeRetour>;

export type CleDeRetour = keyof typeof TYPES_DE_RETOUR;
