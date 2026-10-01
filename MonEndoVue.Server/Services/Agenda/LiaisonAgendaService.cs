using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services.Agenda;

/// <summary>Adresse d'autorisation Google et cookie d'état à poser dans le navigateur.</summary>
public sealed record DebutLiaison(string UrlAutorisation, string CookieEtat);

public sealed record StatutLiaison(bool Disponible, bool Liee, DateTime? LieeLe, string? CalendrierId);

/// <summary>
/// Liaison de l'agenda Google de l'utilisatrice connectée : flux OAuth (code + PKCE, état lié au navigateur), jeton
/// d'actualisation chiffré en base, jeton d'accès à la demande, déliaison avec révocation chez Google.
/// Le carnet est toujours déduit de l'utilisatrice : aucun identifiant de carnet n'est accepté en entrée.
/// </summary>
public class LiaisonAgendaService(
    AppDbContext context,
    GoogleOAuthClient google,
    IOptions<GoogleOAuthOptions> options,
    IDataProtectionProvider protection,
    IMemoryCache cache,
    TimeProvider horloge,
    ILogger<LiaisonAgendaService> logger)
{
    public const string MessageIndisponible = "La liaison avec Google n'est pas disponible pour le moment.";
    public const string MessageEchec = "La liaison avec Google n'a pas abouti. Tu peux réessayer.";

    private IDataProtector ProtecteurEtat => protection.CreateProtector("LiaisonAgenda.Etat");
    private IDataProtector ProtecteurJeton => protection.CreateProtector("LiaisonAgenda.JetonActualisation");

    public async Task<StatutLiaison> StatutAsync(string userId, CancellationToken ct)
    {
        var liaison = await LiaisonDeAsync(userId, ct);
        return new StatutLiaison(options.Value.EstConfiguree, liaison is not null, liaison?.LieeLe, liaison?.CalendrierId);
    }

    public async Task<ResultatOperation<DebutLiaison>> DemarrerAsync(string userId, CancellationToken ct)
    {
        if (!options.Value.EstConfiguree)
        {
            return ResultatOperation<DebutLiaison>.Echec(StatutOperation.Indisponible, MessageIndisponible);
        }

        if (await CarnetDeAsync(userId, ct) is null)
        {
            return ResultatOperation<DebutLiaison>.Echec(StatutOperation.NonAuthentifie);
        }

        var etat = EtatLiaison.Creer(userId, horloge.GetUtcNow());
        return ResultatOperation<DebutLiaison>.Succes(
            new DebutLiaison(google.UrlAutorisationPour(etat.Etat, etat.DefiPkce), etat.Proteger(ProtecteurEtat)));
    }

    /// <summary>
    /// Retour de Google. L'utilisatrice est identifiée par le cookie d'état (posé à son départ, SameSite=Lax), pas par
    /// le code ni la session. Succès : liaison enregistrée (une précédente est remplacée et révoquée).
    /// </summary>
    public async Task<ResultatOperation> FinaliserAsync(
        string? cookieEtat, string? etatRecu, string? code, string? erreurGoogle, CancellationToken ct)
    {
        var etat = EtatLiaison.Lire(ProtecteurEtat, cookieEtat, etatRecu, horloge.GetUtcNow());
        if (etat is null)
        {
            logger.LogWarning("Google Calendar linking rejected: invalid or expired state");
            return ResultatOperation.Echec(StatutOperation.Invalide, MessageEchec);
        }

        if (!string.IsNullOrEmpty(erreurGoogle) || string.IsNullOrEmpty(code))
        {
            // Refus de l'utilisatrice (access_denied) ou erreur de Google : rien à enregistrer.
            logger.LogInformation("Google Calendar linking not completed ({GoogleError})", erreurGoogle ?? "no_code");
            return ResultatOperation.Echec(StatutOperation.Invalide, MessageEchec);
        }

        var carnetId = await CarnetDeAsync(etat.UserId, ct);
        if (carnetId is null) return ResultatOperation.Echec(StatutOperation.NonAuthentifie);

        var (jetons, _) = await google.EchangerCodeAsync(code, etat.VerificateurPkce, ct);
        if (jetons is null) return ResultatOperation.Echec(StatutOperation.Indisponible, MessageEchec);
        if (!GoogleOAuthClient.PorteesAccordees(jetons.Portee))
        {
            // Une permission décochée chez Google : sans elle, impossible de lister puis lire le calendrier choisi.
            logger.LogInformation("Google Calendar linking not completed (partial scopes granted)");
            await google.RevoquerAsync(jetons.JetonActualisation ?? jetons.JetonAcces, ct);
            return ResultatOperation.Echec(StatutOperation.Invalide, MessageEchec);
        }

        if (string.IsNullOrEmpty(jetons.JetonActualisation))
        {
            logger.LogWarning("Google Calendar linking failed: no refresh token returned");
            return ResultatOperation.Echec(StatutOperation.Indisponible, MessageEchec);
        }

        var liaison = await context.LiaisonsAgenda.FirstOrDefaultAsync(l => l.CarnetSanteId == carnetId, ct);
        var ancienJeton = liaison is null ? null : Reveler(liaison);
        if (liaison is null)
        {
            liaison = new LiaisonAgenda { CarnetSanteId = carnetId.Value };
            context.LiaisonsAgenda.Add(liaison);
        }

        liaison.JetonActualisationProtege = ProtecteurJeton.Protect(jetons.JetonActualisation);
        liaison.LieeLe = horloge.GetUtcNow().UtcDateTime;
        // Nouveau compte Google possible : l'ancien choix n'a plus de sens, rien n'est lu avant qu'elle en fasse un.
        liaison.CalendrierId = null;
        await context.SaveChangesAsync(ct);
        MemoriserJetonAcces(carnetId.Value, jetons);

        if (ancienJeton is not null) await google.RevoquerAsync(ancienJeton, ct);
        logger.LogInformation("Google Calendar linked for carnet {CarnetSanteId}", carnetId);
        return ResultatOperation.Succes();
    }

    /// <summary>Calendrier choisi par l'utilisatrice, ou null (pas de liaison, ou aucun choix encore).</summary>
    public async Task<string?> CalendrierChoisiAsync(string userId, CancellationToken ct) =>
        (await LiaisonDeAsync(userId, ct))?.CalendrierId;

    /// <summary>Enregistre le calendrier choisi ; l'appelant a déjà vérifié qu'il figure dans la liste de l'utilisatrice.</summary>
    public async Task<ResultatOperation> EnregistrerCalendrierAsync(string userId, string calendrierId, CancellationToken ct)
    {
        var liaison = await LiaisonDeAsync(userId, ct);
        if (liaison is null) return ResultatOperation.Echec(StatutOperation.Introuvable);

        liaison.CalendrierId = calendrierId;
        await context.SaveChangesAsync(ct);
        return ResultatOperation.Succes();
    }

    /// <summary>Supprime la liaison puis révoque l'accord chez Google (échec de révocation non bloquant).</summary>
    public async Task<ResultatOperation> DelierAsync(string userId, CancellationToken ct)
    {
        var liaison = await LiaisonDeAsync(userId, ct);
        if (liaison is null) return ResultatOperation.Echec(StatutOperation.Introuvable);

        var jeton = Reveler(liaison);
        context.LiaisonsAgenda.Remove(liaison);
        await context.SaveChangesAsync(ct);
        cache.Remove(CleCache(liaison.CarnetSanteId));
        if (jeton is not null) await google.RevoquerAsync(jeton, ct);
        logger.LogInformation("Google Calendar unlinked for carnet {CarnetSanteId}", liaison.CarnetSanteId);
        return ResultatOperation.Succes();
    }

    /// <summary>
    /// Révoque chez Google l'accord d'un carnet avant la suppression de son compte. La ligne en base part avec le
    /// carnet : cette méthode ne touche pas à la base.
    /// </summary>
    public async Task RevoquerPourSuppressionAsync(int carnetId, CancellationToken ct)
    {
        var liaison = await context.LiaisonsAgenda.AsNoTracking().FirstOrDefaultAsync(l => l.CarnetSanteId == carnetId, ct);
        var jeton = liaison is null ? null : Reveler(liaison);
        cache.Remove(CleCache(carnetId));
        if (jeton is not null) await google.RevoquerAsync(jeton, ct);
    }

    /// <summary>
    /// Jeton d'accès pour lire l'agenda : Introuvable sans liaison (ou si l'accord n'est plus valide : la liaison est
    /// alors supprimée), Indisponible si Google ne répond pas.
    /// </summary>
    public async Task<ResultatOperation<string>> JetonAccesAsync(string userId, CancellationToken ct)
    {
        var liaison = await LiaisonDeAsync(userId, ct);
        if (liaison is null || !options.Value.EstConfiguree)
        {
            return ResultatOperation<string>.Echec(StatutOperation.Introuvable);
        }

        if (cache.TryGetValue(CleCache(liaison.CarnetSanteId), out string? enCache) && enCache is not null)
        {
            return ResultatOperation<string>.Succes(enCache);
        }

        var jetonActualisation = Reveler(liaison);
        if (jetonActualisation is null)
        {
            // Clés Data Protection perdues : le jeton est inutilisable, la liaison est à refaire.
            return await OublierAsync(liaison, ct);
        }

        var (jetons, echec) = await google.ActualiserAsync(jetonActualisation, ct);
        if (jetons is null)
        {
            return echec!.Revoque
                ? await OublierAsync(liaison, ct)
                : ResultatOperation<string>.Echec(StatutOperation.Indisponible);
        }

        MemoriserJetonAcces(liaison.CarnetSanteId, jetons);
        return ResultatOperation<string>.Succes(jetons.JetonAcces);
    }

    private async Task<ResultatOperation<string>> OublierAsync(LiaisonAgenda liaison, CancellationToken ct)
    {
        context.LiaisonsAgenda.Remove(liaison);
        await context.SaveChangesAsync(ct);
        cache.Remove(CleCache(liaison.CarnetSanteId));
        logger.LogInformation("Google Calendar link of carnet {CarnetSanteId} dropped (access no longer valid)", liaison.CarnetSanteId);
        return ResultatOperation<string>.Echec(StatutOperation.Introuvable);
    }

    private void MemoriserJetonAcces(int carnetId, JetonsGoogle jetons) =>
        cache.Set(CleCache(carnetId), jetons.JetonAcces, jetons.Validite - TimeSpan.FromMinutes(1));

    private string? Reveler(LiaisonAgenda liaison)
    {
        try
        {
            return ProtecteurJeton.Unprotect(liaison.JetonActualisationProtege);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    private static string CleCache(int carnetId) => $"agenda-jeton-acces-{carnetId}";

    private Task<int?> CarnetDeAsync(string userId, CancellationToken ct) =>
        context.CarnetSantes.Where(c => c.UserId == userId).Select(c => (int?)c.Id).SingleOrDefaultAsync(ct);

    private Task<LiaisonAgenda?> LiaisonDeAsync(string userId, CancellationToken ct) =>
        context.LiaisonsAgenda.FirstOrDefaultAsync(l => l.CarnetSante!.UserId == userId, ct);
}
