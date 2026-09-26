using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services.WebPush.Rappels;

namespace MonEndoVue.Server.Services.WebPush;

/// <summary>
/// Notifications de l'utilisatrice connectée : appareils abonnés, réglage du rappel, notification de test.
/// Le carnet est toujours déduit de l'utilisatrice : aucun identifiant de carnet n'est accepté en entrée.
/// </summary>
public class NotificationsService(
    AppDbContext context,
    NotificationsPushService notifications,
    IEnumerable<IRegleRappel> regles,
    IOptions<WebPushOptions> webPushOptions,
    TimeProvider timeProvider)
{
    private const string FuseauParDefaut = "Europe/Paris";
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

    /// <summary>Tous les rappels proposés, avec les valeurs par défaut de leur règle pour ceux jamais réglés.</summary>
    public async Task<ResultatOperation<List<RappelDto>>> GetRappelsAsync(string userId, CancellationToken cancellationToken)
    {
        var carnetSanteId = await CarnetDeAsync(userId, cancellationToken);
        if (carnetSanteId is null) return ResultatOperation<List<RappelDto>>.Echec(StatutOperation.NonAuthentifie);

        var existants = await context.Rappels
            .Where(r => r.CarnetSanteId == carnetSanteId)
            .ToDictionaryAsync(r => r.Type, cancellationToken);

        var rappels = regles.Select(regle => existants.TryGetValue(regle.Type, out var rappel)
                ? VersDto(regle, rappel.Actif, rappel.Heure, rappel.JourSemaine, rappel.FuseauHoraire)
                : VersDto(regle, false, regle.HeureParDefaut, regle.JourParDefaut, FuseauParDefaut))
            .ToList();
        return ResultatOperation<List<RappelDto>>.Succes(rappels);
    }

    public async Task<ResultatOperation> PutRappelAsync(string userId, string type, RappelDto dto, CancellationToken cancellationToken)
    {
        var carnetSanteId = await CarnetDeAsync(userId, cancellationToken);
        if (carnetSanteId is null) return ResultatOperation.Echec(StatutOperation.NonAuthentifie);

        var regle = Enum.TryParse<TypeRappel>(type, ignoreCase: true, out var typeRappel)
            ? regles.FirstOrDefault(r => r.Type == typeRappel)
            : null;
        if (regle is null) return ResultatOperation.Echec(StatutOperation.Introuvable);

        if (!TimeOnly.TryParseExact(dto.Heure, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var heure))
        {
            return ResultatOperation.Echec(StatutOperation.Invalide, "L'heure du rappel doit être au format HH:mm.");
        }

        if (!FuseauxHoraires.EstValide(dto.FuseauHoraire))
        {
            return ResultatOperation.Echec(StatutOperation.Invalide, "Fuseau horaire inconnu.");
        }

        if (regle.EstHebdomadaire && dto.JourSemaine is not (>= 0 and <= 6))
        {
            return ResultatOperation.Echec(StatutOperation.Invalide, "Choisis le jour du rappel.");
        }

        var rappel = await context.Rappels.FirstOrDefaultAsync(
            r => r.CarnetSanteId == carnetSanteId && r.Type == regle.Type, cancellationToken);
        if (rappel is null)
        {
            rappel = new Rappel { CarnetSanteId = carnetSanteId.Value, Type = regle.Type };
            context.Rappels.Add(rappel);
        }

        rappel.Actif = dto.Actif;
        rappel.Heure = heure;
        rappel.JourSemaine = regle.EstHebdomadaire ? (DayOfWeek)dto.JourSemaine!.Value : null;
        rappel.FuseauHoraire = dto.FuseauHoraire;
        await context.SaveChangesAsync(cancellationToken);
        return ResultatOperation.Succes();
    }

    private static RappelDto VersDto(IRegleRappel regle, bool actif, TimeOnly heure, DayOfWeek? jour, string fuseau) => new()
    {
        Type = regle.Type.ToString(),
        EstHebdomadaire = regle.EstHebdomadaire,
        Actif = actif,
        Heure = heure.ToString("HH:mm", CultureInfo.InvariantCulture),
        JourSemaine = jour is null ? null : (int)jour.Value,
        FuseauHoraire = fuseau,
    };

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
