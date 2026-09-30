/**
 * Couleurs d'une valeur d'intensité (0 à 10) sur l'échelle commune `--intensite-N` : texte foncé jusqu'à 5,
 * clair à partir de 6. Sert aux pastilles des listes comme aux choix sélectionnés des échelles de saisie.
 */
export function couleursIntensite(intensite: number) {
  const couleur = `var(--intensite-${intensite})`;
  return {
    background: couleur,
    borderColor: couleur,
    color: intensite >= 6 ? 'var(--couleur-sur-fonce)' : 'var(--couleur-texte)',
  };
}
