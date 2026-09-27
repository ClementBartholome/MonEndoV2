export type User = {
    email: string;
    carnetSanteId: number;
    tokenExpiry: Date;
    /** Consentement aux données de santé donné pour la politique en vigueur ; absent dans une session antérieure à la 1.3.0. */
    consentementAJour?: boolean;
};

/** Réponse de `POST Account/consentement`. */
export interface ReponseConsentement {
    tokenExpiry: Date;
    consentementAJour: boolean;
}