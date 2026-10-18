using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services.WebPush.Rappels;

/// <summary>
/// Règle propre à un type de rappel : valeurs par défaut, message (avec la page à ouvrir) et condition « suivi déjà fait ».
/// Le calendrier commun (fuseau, heure atteinte, jour de la semaine, un envoi par jour) est géré par
/// <see cref="NotificationsPushService"/>. Ajouter un rappel = ajouter une implémentation enregistrée dans Program.cs.
/// </summary>
public interface IRegleRappel
{
    TypeRappel Type { get; }
    bool EstHebdomadaire { get; }
    TimeOnly HeureParDefaut { get; }
    DayOfWeek? JourParDefaut { get; }
    MessagePush Message { get; }

    /// <summary>Vrai si l'utilisatrice a déjà fait le suivi visé : le rappel est alors omis.</summary>
    Task<bool> SuiviDejaFaitAsync(int carnetSanteId, DateOnly jourLocal, TimeZoneInfo fuseau, CancellationToken cancellationToken);

    /// <summary>
    /// Ce qui empêche durablement le rappel de partir, à expliquer sous son réglage (ex. aucun épisode d'acné en cours) ;
    /// null si rien ne l'empêche. Par défaut : rien.
    /// </summary>
    Task<string?> EnAttenteAsync(int carnetSanteId, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
}
