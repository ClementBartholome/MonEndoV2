using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services.Sessions;

/// <summary>Jeton de renouvellement à poser dans le cookie de l'appareil, et sa date d'expiration (UTC).</summary>
public sealed record JetonSession(string Jeton, DateTime Expiration);

public enum IssueRenouvellement
{
    Renouvelee,
    Inconnue,
    Expiree,
}

public sealed record ResultatRenouvellement(IssueRenouvellement Issue, string? UserId = null, JetonSession? Session = null);

/// <summary>
/// Sessions par appareil (jetons de renouvellement hachés, un par appareil). Ne connaît pas HTTP : le contrôleur pose les cookies.
/// </summary>
public class SessionsService(AppDbContext context, TokenService tokenService, TimeProvider horloge)
{
    /// <summary>Durée d'une session sans activité : elle glisse à chaque renouvellement.</summary>
    public static readonly TimeSpan DureeSession = TimeSpan.FromDays(2);

    /// <summary>Temps pendant lequel l'ancien jeton reste accepté après une rotation (réponse de renouvellement perdue).</summary>
    public static readonly TimeSpan ToleranceReponsePerdue = TimeSpan.FromHours(6);

    /// <summary>Nombre d'appareils conservés par compte ; au-delà, le moins récemment utilisé est retiré.</summary>
    public const int AppareilsMax = 10;

    public static string Hacher(string jeton) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(jeton)));

    /// <summary>Nouvelle session pour un appareil (connexion, inscription).</summary>
    public async Task<JetonSession> OuvrirAsync(string userId, CancellationToken ct = default)
    {
        var maintenant = horloge.GetUtcNow().UtcDateTime;
        await PurgerAsync(userId, maintenant, ct);

        var jeton = tokenService.GenerateRefreshToken();
        context.SessionsAppareil.Add(new SessionAppareil
        {
            UserId = userId,
            JetonHache = Hacher(jeton),
            CreeLe = maintenant,
            DerniereUtilisationLe = maintenant,
            ExpireLe = maintenant + DureeSession,
        });
        await context.SaveChangesAsync(ct);
        return new JetonSession(jeton, maintenant + DureeSession);
    }

    /// <summary>Échange le jeton de l'appareil contre un nouveau (la session glisse). Seule la session de cet appareil change.</summary>
    public async Task<ResultatRenouvellement> RenouvelerAsync(string jeton, CancellationToken ct = default)
    {
        for (var essai = 0; ; essai++)
        {
            try
            {
                return await RenouvelerUneFoisAsync(jeton, ct);
            }
            catch (DbUpdateConcurrencyException) when (essai == 0)
            {
                // Deux renouvellements simultanés avec le même jeton : le second retrouve l'ancien jeton dans la tolérance.
                context.ChangeTracker.Clear();
            }
        }
    }

    private async Task<ResultatRenouvellement> RenouvelerUneFoisAsync(string jeton, CancellationToken ct)
    {
        var maintenant = horloge.GetUtcNow().UtcDateTime;
        var empreinte = Hacher(jeton);

        var session = await context.SessionsAppareil.SingleOrDefaultAsync(s => s.JetonHache == empreinte, ct);
        var ancienJeton = false;
        if (session is null)
        {
            session = await context.SessionsAppareil.SingleOrDefaultAsync(s => s.JetonPrecedentHache == empreinte, ct);
            ancienJeton = session is not null;
            if (session is null) return await DepuisLegacyAsync(jeton, maintenant, ct);
        }

        if (session.ExpireLe <= maintenant)
        {
            context.SessionsAppareil.Remove(session);
            await context.SaveChangesAsync(ct);
            return new ResultatRenouvellement(IssueRenouvellement.Expiree);
        }

        if (ancienJeton && (session.RotationLe is not { } rotation || rotation + ToleranceReponsePerdue <= maintenant))
        {
            return new ResultatRenouvellement(IssueRenouvellement.Inconnue);
        }

        var nouveau = tokenService.GenerateRefreshToken();
        if (!ancienJeton)
        {
            session.JetonPrecedentHache = session.JetonHache;
            session.RotationLe = maintenant;
        }

        session.JetonHache = Hacher(nouveau);
        session.DerniereUtilisationLe = maintenant;
        session.ExpireLe = maintenant + DureeSession;
        await context.SaveChangesAsync(ct);
        return new ResultatRenouvellement(IssueRenouvellement.Renouvelee, session.UserId, new JetonSession(nouveau, session.ExpireLe));
    }

    /// <summary>
    /// Transition depuis le jeton unique en clair de <see cref="ApplicationUser.RefreshToken"/> : une session ouverte avant la mise à jour
    /// est convertie à son premier renouvellement, sans déconnecter personne. À retirer avec ces colonnes.
    /// </summary>
    private async Task<ResultatRenouvellement> DepuisLegacyAsync(string jeton, DateTime maintenant, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(jeton)) return new ResultatRenouvellement(IssueRenouvellement.Inconnue);
        var user = await context.Users.SingleOrDefaultAsync(u => u.RefreshToken == jeton, ct);
        if (user is null) return new ResultatRenouvellement(IssueRenouvellement.Inconnue);
        if (user.RefreshTokenExpiryTime is not { } expiration || expiration <= maintenant)
        {
            return new ResultatRenouvellement(IssueRenouvellement.Expiree);
        }

        user.RefreshToken = string.Empty;
        user.RefreshTokenExpiryTime = null;
        var session = await OuvrirAsync(user.Id, ct);
        return new ResultatRenouvellement(IssueRenouvellement.Renouvelee, user.Id, session);
    }

    /// <summary>Ferme la session de l'appareil qui présente ce jeton (déconnexion).</summary>
    public async Task RevoquerAsync(string jeton, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(jeton)) return;
        var empreinte = Hacher(jeton);
        context.SessionsAppareil.RemoveRange(await context.SessionsAppareil
            .Where(s => s.JetonHache == empreinte || s.JetonPrecedentHache == empreinte).ToListAsync(ct));

        foreach (var user in await context.Users.Where(u => u.RefreshToken == jeton).ToListAsync(ct))
        {
            user.RefreshToken = string.Empty;
            user.RefreshTokenExpiryTime = null;
        }

        await context.SaveChangesAsync(ct);
    }

    /// <summary>Ferme toutes les sessions du compte sauf celle de l'appareil courant (changement de mot de passe).</summary>
    public async Task RevoquerAutresAsync(string userId, string? jetonCourant, CancellationToken ct = default)
    {
        var empreinte = string.IsNullOrEmpty(jetonCourant) ? null : Hacher(jetonCourant);
        context.SessionsAppareil.RemoveRange(await context.SessionsAppareil
            .Where(s => s.UserId == userId && (empreinte == null || (s.JetonHache != empreinte && s.JetonPrecedentHache != empreinte)))
            .ToListAsync(ct));
        await context.SaveChangesAsync(ct);
    }

    private async Task PurgerAsync(string userId, DateTime maintenant, CancellationToken ct)
    {
        var sessions = await context.SessionsAppareil.Where(s => s.UserId == userId).OrderByDescending(s => s.DerniereUtilisationLe).ToListAsync(ct);
        var aRetirer = sessions.Where(s => s.ExpireLe <= maintenant)
            .Concat(sessions.Where(s => s.ExpireLe > maintenant).Skip(AppareilsMax - 1));
        context.SessionsAppareil.RemoveRange(aRetirer.ToList());
    }
}
