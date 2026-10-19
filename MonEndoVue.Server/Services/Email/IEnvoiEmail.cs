namespace MonEndoVue.Server.Services.Email;

/// <summary>Un message à envoyer : texte brut et version HTML du même contenu.</summary>
public sealed record MessageEmail(string A, string Sujet, string Texte, string Html);

public enum IssueEnvoiEmail
{
    Envoye,
    /// <summary>Envoi désactivé (configuration absente) : rien n'est parti.</summary>
    Desactive,
    /// <summary>Le relais a refusé ou n'a pas répondu : à réessayer plus tard.</summary>
    Echec,
}

/// <summary>
/// Envoi d'e-mails : une abstraction parce que l'envoi sort du processus et doit être remplacé en test (<c>FauxEnvoiEmail</c>).
/// Une implémentation ne journalise jamais l'adresse, l'objet ni le contenu d'un message.
/// </summary>
public interface IEnvoiEmail
{
    /// <summary>Vrai si un relais est configuré ; sinon l'inscription et les messages de compte sont refusés plutôt que perdus.</summary>
    bool Disponible { get; }

    Task<IssueEnvoiEmail> EnvoyerAsync(MessageEmail message, CancellationToken ct = default);
}
