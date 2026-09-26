/**
 * L'API sérialise avec ReferenceHandler.Preserve : une liste arrive soit en tableau, soit en `{ $values: [...] }`.
 * Renvoie toujours un tableau (vide si la valeur est absente).
 */
export const enTableau = <T>(valeur: T[] | { $values: T[] } | null | undefined): T[] => {
  if (!valeur) return [];
  return Array.isArray(valeur) ? valeur : valeur.$values ?? [];
};
