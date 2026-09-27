namespace MonEndoVue.Server.Services.Agenda;

/// <summary>
/// Agenda Google en lecture seule (section de configuration « Agenda »), en attendant une liaison propre à chaque
/// utilisatrice. La clé API reste côté serveur ; seule une utilisatrice listée dans <see cref="Calendriers"/> a un agenda.
/// </summary>
public class AgendaOptions
{
    public const string Section = "Agenda";

    /// <summary>Clé de l'API Google Calendar, jamais transmise au client.</summary>
    public string? CleApi { get; set; }

    /// <summary>Identifiant de l'utilisatrice (AspNetUsers.Id) → identifiant du calendrier Google qui lui est affiché.</summary>
    public Dictionary<string, string> Calendriers { get; set; } = new();

    /// <summary>Calendrier de l'utilisatrice, ou null si elle n'en a pas (ou si la clé API manque).</summary>
    public string? CalendrierDe(string userId)
    {
        if (string.IsNullOrWhiteSpace(CleApi) || string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var calendrier = Calendriers
            .FirstOrDefault(c => string.Equals(c.Key, userId, StringComparison.OrdinalIgnoreCase))
            .Value;
        return string.IsNullOrWhiteSpace(calendrier) ? null : calendrier;
    }
}
