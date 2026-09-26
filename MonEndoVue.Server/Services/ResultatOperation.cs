namespace MonEndoVue.Server.Services;

/// <summary>Issue métier d'une opération, indépendante de HTTP (traduite en réponse par le contrôleur).</summary>
public enum StatutOperation
{
    Succes,
    Invalide,
    NonAuthentifie,
    Introuvable,
    Interdit,
    Indisponible,
}

public sealed record ResultatOperation(StatutOperation Statut, string? Message = null)
{
    public static ResultatOperation Succes() => new(StatutOperation.Succes);
    public static ResultatOperation Echec(StatutOperation statut, string? message = null) => new(statut, message);
}

public sealed record ResultatOperation<T>(StatutOperation Statut, T? Valeur, string? Message = null)
{
    public static ResultatOperation<T> Succes(T valeur) => new(StatutOperation.Succes, valeur);
    public static ResultatOperation<T> Echec(StatutOperation statut, string? message = null) => new(statut, default, message);
}
