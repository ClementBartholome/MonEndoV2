using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace MonEndoVue.Server.Services.Agenda;

/// <summary>
/// État d'une liaison en cours : lie le retour de Google au navigateur et à l'utilisatrice qui l'ont demandée.
/// Il voyage dans un cookie chiffré et signé (Data Protection) de courte durée, car le cookie de session
/// (SameSite=Strict) n'est pas envoyé lors du retour de Google.
/// </summary>
public sealed record EtatLiaison(string UserId, string Etat, string VerificateurPkce, DateTimeOffset ExpireLe)
{
    public static readonly TimeSpan Duree = TimeSpan.FromMinutes(10);

    public static EtatLiaison Creer(string userId, DateTimeOffset maintenant) =>
        new(userId, AleatoireUrl(32), AleatoireUrl(64), maintenant + Duree);

    /// <summary>Défi PKCE (méthode S256) du vérificateur.</summary>
    public string DefiPkce => Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(VerificateurPkce)))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public string Proteger(IDataProtector protecteur) => protecteur.Protect(JsonSerializer.Serialize(this));

    /// <summary>Null si le cookie est absent, altéré, expiré ou ne correspond pas à l'état renvoyé par Google.</summary>
    public static EtatLiaison? Lire(IDataProtector protecteur, string? cookie, string? etatRecu, DateTimeOffset maintenant)
    {
        if (string.IsNullOrEmpty(cookie) || string.IsNullOrEmpty(etatRecu)) return null;
        try
        {
            var etat = JsonSerializer.Deserialize<EtatLiaison>(protecteur.Unprotect(cookie));
            var egal = etat is not null && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(etat.Etat), Encoding.UTF8.GetBytes(etatRecu));
            return egal && etat!.ExpireLe > maintenant ? etat : null;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        {
            return null;
        }
    }

    private static string AleatoireUrl(int octets) =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(octets)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
