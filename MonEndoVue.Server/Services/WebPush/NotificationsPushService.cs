using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;

namespace MonEndoVue.Server.Services.WebPush;

/// <summary>Envoi des notifications d'un carnet et des rappels quotidiens du bilan.</summary>
public class NotificationsPushService(
    AppDbContext context,
    IEnvoiPush envoiPush,
    TimeProvider timeProvider,
    ILogger<NotificationsPushService> logger)
{
    public static readonly MessagePush MessageRappelBilan =
        new("MonEndo", "N'oublie pas de remplir ton bilan quotidien.", "/bilan-quotidien");

    public static bool EstFuseauValide(string? fuseau) =>
        !string.IsNullOrWhiteSpace(fuseau) && TimeZoneInfo.TryFindSystemTimeZoneById(fuseau, out _);

    /// <summary>Bornes UTC [début, fin[ d'une journée locale dans le fuseau donné.</summary>
    public static (DateTime Debut, DateTime Fin) JourneeEnUtc(DateOnly jour, TimeZoneInfo fuseau)
    {
        var debutLocal = jour.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return (TimeZoneInfo.ConvertTimeToUtc(debutLocal, fuseau),
            TimeZoneInfo.ConvertTimeToUtc(debutLocal.AddDays(1), fuseau));
    }

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
    /// Envoie le rappel aux carnets dont l'heure de rappel locale est atteinte, qui ne l'ont pas encore reçu aujourd'hui
    /// et dont le bilan du jour (journée locale) n'est pas rempli.
    /// </summary>
    /// <returns>Nombre de carnets notifiés.</returns>
    public async Task<int> EnvoyerRappelsDusAsync(CancellationToken cancellationToken)
    {
        var maintenant = timeProvider.GetUtcNow();
        var preferences = await context.PreferencesRappel
            .Where(p => p.RappelActif)
            .ToListAsync(cancellationToken);

        var notifies = 0;
        foreach (var preference in preferences)
        {
            if (!TimeZoneInfo.TryFindSystemTimeZoneById(preference.FuseauHoraire, out var fuseau))
            {
                logger.LogWarning("Unknown time zone for reminder of carnet {CarnetSanteId}", preference.CarnetSanteId);
                continue;
            }

            var local = TimeZoneInfo.ConvertTime(maintenant, fuseau).DateTime;
            var aujourdhui = DateOnly.FromDateTime(local);
            if (TimeOnly.FromDateTime(local) < preference.HeureRappel || preference.DernierRappelLe == aujourdhui)
            {
                continue;
            }

            var (debut, fin) = JourneeEnUtc(aujourdhui, fuseau);
            var bilanRempli = await context.BilansQuotidiens.AnyAsync(
                b => b.CarnetSanteId == preference.CarnetSanteId && b.Date >= debut && b.Date < fin,
                cancellationToken);
            if (bilanRempli)
            {
                continue;
            }

            if (await EnvoyerAuCarnetAsync(preference.CarnetSanteId, MessageRappelBilan, cancellationToken) > 0)
            {
                preference.DernierRappelLe = aujourdhui;
                notifies++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return notifies;
    }
}
