using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Services.WebPush.Rappels;

namespace MonEndoVue.Server.Services.WebPush;

/// <summary>Envoi des notifications d'un carnet et des rappels dus, quel que soit leur type.</summary>
public class NotificationsPushService(
    AppDbContext context,
    IEnvoiPush envoiPush,
    IEnumerable<IRegleRappel> regles,
    TimeProvider timeProvider,
    ILogger<NotificationsPushService> logger)
{
    private readonly Dictionary<Models.TypeRappel, IRegleRappel> _regles = regles.ToDictionary(r => r.Type);

    /// <summary>Envoie le message à tous les appareils du carnet ; supprime les abonnements expirés.</summary>
    /// <returns>Nombre d'appareils ayant reçu le message.</returns>
    public async Task<int> EnvoyerAuCarnetAsync(int carnetSanteId, MessagePush message, CancellationToken cancellationToken)
    {
        var abonnements = await context.AbonnementsPush
            .Where(a => a.CarnetSanteId == carnetSanteId)
            .ToListAsync(cancellationToken);

        var envoyes = 0;
        foreach (var abonnement in abonnements)
        {
            var resultat = await envoiPush.EnvoyerAsync(abonnement, message, cancellationToken);
            if (resultat == ResultatEnvoiPush.Envoye)
            {
                envoyes++;
            }
            else if (resultat == ResultatEnvoiPush.AbonnementExpire)
            {
                context.AbonnementsPush.Remove(abonnement);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return envoyes;
    }

    /// <summary>
    /// Envoie les rappels actifs dont l'heure locale est atteinte (et le jour, pour un rappel hebdomadaire), pas encore
    /// envoyés aujourd'hui, et dont la règle indique que le suivi n'est pas déjà fait.
    /// </summary>
    /// <returns>Nombre de rappels envoyés.</returns>
    public async Task<int> EnvoyerRappelsDusAsync(CancellationToken cancellationToken)
    {
        var maintenant = timeProvider.GetUtcNow();
        var rappels = await context.Rappels
            .Where(r => r.Actif)
            .ToListAsync(cancellationToken);

        var envoyes = 0;
        foreach (var rappel in rappels)
        {
            if (!_regles.TryGetValue(rappel.Type, out var regle))
            {
                continue;
            }

            if (!TimeZoneInfo.TryFindSystemTimeZoneById(rappel.FuseauHoraire, out var fuseau))
            {
                logger.LogWarning("Unknown time zone for reminder {Type} of carnet {CarnetSanteId}", rappel.Type, rappel.CarnetSanteId);
                continue;
            }

            var local = TimeZoneInfo.ConvertTime(maintenant, fuseau).DateTime;
            var aujourdhui = DateOnly.FromDateTime(local);
            var pasEncoreLHeure = TimeOnly.FromDateTime(local) < rappel.Heure;
            var mauvaisJour = regle.EstHebdomadaire && local.DayOfWeek != rappel.JourSemaine;
            if (pasEncoreLHeure || mauvaisJour || rappel.DernierEnvoiLe == aujourdhui)
            {
                continue;
            }

            if (await regle.SuiviDejaFaitAsync(rappel.CarnetSanteId, aujourdhui, fuseau, cancellationToken))
            {
                continue;
            }

            if (await EnvoyerAuCarnetAsync(rappel.CarnetSanteId, regle.Message, cancellationToken) > 0)
            {
                rappel.DernierEnvoiLe = aujourdhui;
                envoyes++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return envoyes;
    }
}
