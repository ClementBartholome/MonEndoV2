/**
 * Lien reçu par e-mail (confirmation d'adresse, nouveau mot de passe) : l'adresse et le jeton sont dans le fragment
 * (`#email=…&jeton=…`), qui n'est jamais envoyé au serveur. On le lit une fois, puis on le retire de la barre d'adresse
 * (historique, partage d'écran, capture) sans recharger la page.
 */
export function lireLienDuFragment(): { email: string; jeton: string } | null {
    const fragment = window.location.hash.replace(/^#/, '');
    if (fragment) window.history.replaceState(window.history.state, '', window.location.pathname);
    const valeurs = new URLSearchParams(fragment);
    const email = valeurs.get('email');
    const jeton = valeurs.get('jeton');
    return email && jeton ? { email, jeton } : null;
}

/** Adresse transmise d'une page à l'autre par l'état de la navigation (jamais dans l'URL). */
export function emailDeLaNavigation(): string {
    const etat = window.history.state as { email?: unknown } | null;
    return typeof etat?.email === 'string' ? etat.email : '';
}
