/**
 * L'API sérialise avec ReferenceHandler.Preserve : une liste arrive soit en tableau, soit en `{ $values: [...] }`.
 * Renvoie toujours un tableau (vide si la valeur est absente).
 */
export const enTableau = <T>(valeur: T[] | { $values: T[] } | null | undefined): T[] => {
  if (!valeur) return [];
  return Array.isArray(valeur) ? valeur : valeur.$values ?? [];
};

/**
 * Même réponse sans les marques de ReferenceHandler.Preserve, à tous les niveaux : chaque `{ $values: [...] }` devient un
 * tableau et les `$id` disparaissent. Pour une réponse à plusieurs listes imbriquées, en lecture seule.
 */
export const sansReferences = <T>(valeur: unknown): T => {
  if (Array.isArray(valeur)) return valeur.map((element) => sansReferences(element)) as T;
  if (valeur === null || typeof valeur !== 'object') return valeur as T;
  const objet = valeur as Record<string, unknown>;
  if (Array.isArray(objet.$values)) return sansReferences(objet.$values);
  return Object.fromEntries(Object.entries(objet).filter(([cle]) => cle !== '$id').map(([cle, v]) => [cle, sansReferences(v)])) as T;
};
