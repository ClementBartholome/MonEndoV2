using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services.WebPush;

/// <summary>
/// Notifications de l'utilisatrice connectée : appareils abonnés, réglage du rappel, notification de test.
/// Le carnet est toujours déduit de l'utilisatrice : aucun identifiant de carnet n'est accepté en entrée.
/// </summary>
public class NotificationsService(
    AppDbContext context,
    NotificationsPushService notifications,
    IOptions<WebPushOptions> webPushOptions,
    TimeProvider timeProvider)
{
    public const string MessageNonConfigure = "Les notifications ne sont pas disponibles pour le moment.";

    public ResultatOperation<string> ClePublique()
    {
        var options = webPushOptions.Value;
        return options.EstConfiguree
            ? ResultatOperation<string>.Succes(options.PublicKey!)
            : ResultatOperation<string>.Echec(StatutOperation.Indisponible, MessageNonConfigure);
    }

    public async Task<ResultatOperation> AbonnerAsync(string userId, AbonnementPushDto dto, CancellationToken cancellationToken)
    {
        var carnetSanteId = await CarnetDeAsync(userId, cancellationToken);
        if (carnetSanteId is null) return ResultatOperation.Echec(StatutOperation.NonAuthentifie);

        if (!Uri.TryCreate(dto.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps)
        {
            return ResultatOperation.Echec(StatutOperation.Invalide, "Abonnement aux notifications invalide.");
        }

        // Un appareil n'a qu'un abonnement : s'il était lié à un autre compte, il est réaffecté au compte connecté.
        var abonnement = await context.AbonnementsPush
            .FirstOrDefaultAsync(a => a.Endpoint == dto.Endpoint, cancellationToken);
        if (abonnement is null)
        {
            abonnement = new AbonnementPush { Endpoint = dto.Endpoint, CreeLe = timeProvider.GetUtcNow().UtcDateTime };
            context.AbonnementsPush.Add(abonnement);
        }

        abonnement.CarnetSanteId = carnetSanteId.Value;
        abonnement.P256dh = dto.P256dh;
        abonnement.Auth = dto.Auth;
        await context.SaveChangesAsync(cancellationToken);
        return ResultatOperation.Succes();
    }

    public async Task<ResultatOperation> DesabonnerAsync(string userId, string endpoint, CancellationToken cancellationToken)
    {
        var carnetSanteId = await CarnetDeAsync(userId, cancellationToken);
        if (carnetSanteId is null) return ResultatOperation.Echec(StatutOperation.NonAuthentifie);

        var abonnement = await context.AbonnementsPush.FirstOrDefaultAsync(
            a => a.Endpoint == endpoint && a.CarnetSanteId == carnetSanteId, cancellationToken);
        if (abonnement is not null)
        {
            context.AbonnementsPush.Remove(abonnement);
            await context.SaveChangesAsync(cancellationToken);
        }

        return ResultatOperation.Succes();
    }

    public async Task<ResultatOperation<PreferenceRappelDto>> GetPreferencesAsync(string userId, CancellationToken cancellationToken)
    {
        var carnetSanteId = await CarnetDeAsync(userId, cancellationToken);
        if (carnetSanteId is null) return ResultatOperation<PreferenceRappelDto>.Echec(StatutOperation.NonAuthentifie);

        var preference = await context.PreferencesRappel
            .FirstOrDefaultAsync(p => p.CarnetSanteId == carnetSanteId, cancellationToken)
            ?? new PreferenceRappel();

        return ResultatOperation<PreferenceRappelDto>.Succes(new PreferenceRappelDto
        {
            RappelActif = preference.RappelActif,
            HeureRappel = preference.HeureRappel.ToString("HH:mm", CultureInfo.InvariantCulture),
            FuseauHoraire = preference.FuseauHoraire,
        });
    }

    public async Task<ResultatOperation> PutPreferencesAsync(string userId, PreferenceRappelDto dto, CancellationToken cancellationToken)
    {
        var carnetSanteId = await CarnetDeAsync(userId, cancellationToken);
        if (carnetSanteId is null) return ResultatOperation.Echec(StatutOperation.NonAuthentifie);

        if (!TimeOnly.TryParseExact(dto.HeureRappel, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var heure))
        {
            return ResultatOperation.Echec(StatutOperation.Invalide, "L'heure du rappel doit être au format HH:mm.");
        }

        if (!NotificationsPushService.EstFuseauValide(dto.FuseauHoraire))
        {
            return ResultatOperation.Echec(StatutOperation.Invalide, "Fuseau horaire inconnu.");
        }

        var preference = await context.PreferencesRappel
            .FirstOrDefaultAsync(p => p.CarnetSanteId == carnetSanteId, cancellationToken);
        if (preference is null)
        {
            preference = new PreferenceRappel { CarnetSanteId = carnetSanteId.Value };
            context.PreferencesRappel.Add(preference);
        }

        preference.RappelActif = dto.RappelActif;
        preference.HeureRappel = heure;
        preference.FuseauHoraire = dto.FuseauHoraire;
        await context.SaveChangesAsync(cancellationToken);
        return ResultatOperation.Succes();
    }

    public async Task<ResultatOperation<int>> EnvoyerTestAsync(string userId, CancellationToken cancellationToken)
    {
        var carnetSanteId = await CarnetDeAsync(userId, cancellationToken);
        if (carnetSanteId is null) return ResultatOperation<int>.Echec(StatutOperation.NonAuthentifie);

        if (!webPushOptions.Value.EstConfiguree)
        {
            return ResultatOperation<int>.Echec(StatutOperation.Indisponible, MessageNonConfigure);
        }

        var envoyes = await notifications.EnvoyerAuCarnetAsync(
            carnetSanteId.Value,
            new MessagePush("MonEndo", "Les notifications fonctionnent sur cet appareil.", "/parametres"),
            cancellationToken);

        return envoyes > 0
            ? ResultatOperation<int>.Succes(envoyes)
            : ResultatOperation<int>.Echec(StatutOperation.Invalide,
                "Aucun appareil n'a reçu la notification. Réactive les notifications sur cet appareil.");
    }

    private async Task<int?> CarnetDeAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userId)) return null;

        return await context.CarnetSantes
            .Where(c => c.UserId == userId)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
