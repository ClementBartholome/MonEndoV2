/**
 * Garde contre les réponses qui arrivent dans le désordre : à chaque nouvelle demande (changement de mois rapide),
 * seule la dernière a le droit de s'afficher ; la réponse d'une demande plus ancienne est ignorée.
 *
 * `const nouvelleDemande = derniereDemande();` une fois par composable, puis dans le chargement
 * `const estCourante = nouvelleDemande();` avant l'appel et `if (!estCourante()) return;` après chaque `await`.
 */
export function derniereDemande(): () => () => boolean {
    let compteur = 0;
    return () => {
        const numero = ++compteur;
        return () => numero === compteur;
    };
}
