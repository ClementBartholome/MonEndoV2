import type { IntensiteTransit, TransitBilan } from '@/features/bilan-quotidien/types/bilan-quotidien';

export interface BristolType {
  type: number;
  titre: string;
  description: string;
  /** Repère indicatif, formulé sans diagnostic. */
  tendance: string;
}

/** Échelle de Bristol (aspect des selles), types 1 à 7. */
export const echelleBristol: BristolType[] = [
  { type: 1, titre: 'Petites boules dures', description: 'Morceaux durs et séparés, comme des noisettes, difficiles à évacuer.', tendance: 'Tendance constipation' },
  { type: 2, titre: 'Saucisse grumeleuse', description: 'En forme de saucisse, mais dure et formée de morceaux collés.', tendance: 'Tendance constipation' },
  { type: 3, titre: 'Saucisse craquelée', description: 'En forme de saucisse, avec des craquelures en surface.', tendance: 'Transit habituel' },
  { type: 4, titre: 'Saucisse lisse', description: 'En forme de saucisse ou de serpent, lisse et souple.', tendance: 'Transit habituel' },
  { type: 5, titre: 'Morceaux mous', description: 'Morceaux mous aux bords nets, évacués facilement.', tendance: 'Tendance selles molles' },
  { type: 6, titre: 'Selles pâteuses', description: 'Morceaux duveteux aux bords irréguliers, consistance pâteuse.', tendance: 'Tendance diarrhée' },
  { type: 7, titre: 'Selles liquides', description: 'Entièrement liquides, sans morceau solide.', tendance: 'Tendance diarrhée' },
];

export const intensitesTransit: IntensiteTransit[] = ['Légère', 'Modérée', 'Forte'];

export const transitVide = (): TransitBilan => ({
  selles: null,
  typeBristol: null,
  crampesEstomac: null,
  intensiteCrampes: null,
  ballonnements: null,
  intensiteBallonnements: null,
});

/** Mêmes règles que BilanTransitValidator côté serveur : une intensité est requise dès qu'un symptôme est présent. */
export const estTransitComplet = (transit: TransitBilan): boolean =>
  (transit.crampesEstomac !== true || transit.intensiteCrampes !== null) &&
  (transit.ballonnements !== true || transit.intensiteBallonnements !== null);

export const libelleBristol = (type: number | null | undefined): string | null => {
  const entree = echelleBristol.find((b) => b.type === type);
  return entree ? `Type ${entree.type} · ${entree.titre}` : null;
};
