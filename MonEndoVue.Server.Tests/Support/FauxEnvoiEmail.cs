using MonEndoVue.Server.Services.Email;

namespace MonEndoVue.Server.Tests.Support;

/// <summary>Remplace le relais SMTP : garde les messages « envoyés » pour les vérifier, sans réseau.</summary>
public sealed class FauxEnvoiEmail : IEnvoiEmail
{
    public List<MessageEmail> Envoyes { get; } = [];

    /// <summary>Issue renvoyée à chaque envoi ; <see cref="IssueEnvoiEmail.Echec"/> simule un relais en panne.</summary>
    public IssueEnvoiEmail Issue { get; set; } = IssueEnvoiEmail.Envoye;

    public bool Disponible { get; set; } = true;

    public Task<IssueEnvoiEmail> EnvoyerAsync(MessageEmail message, CancellationToken ct = default)
    {
        if (!Disponible) return Task.FromResult(IssueEnvoiEmail.Desactive);
        if (Issue == IssueEnvoiEmail.Envoye) Envoyes.Add(message);
        return Task.FromResult(Issue);
    }

    /// <summary>Jeton du dernier lien du message : lu dans le fragment, comme le fait la page.</summary>
    public static (string Email, string Jeton) LireLien(MessageEmail message)
    {
        var debut = message.Texte.IndexOf("http", StringComparison.Ordinal);
        var lien = message.Texte[debut..].Split(['\n', ' '])[0];
        var fragment = new Uri(lien).Fragment.TrimStart('#');
        var valeurs = fragment.Split('&').Select(p => p.Split('=', 2)).ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]));
        return (valeurs["email"], valeurs["jeton"]);
    }
}
