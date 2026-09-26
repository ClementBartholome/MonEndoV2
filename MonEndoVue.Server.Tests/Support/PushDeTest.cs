using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services.WebPush;

namespace MonEndoVue.Server.Tests.Support;

/// <summary>Outils de test des notifications Web Push : clés réelles, faux envoi, horloge fixe.</summary>
public static class PushDeTest
{
    private static string Base64Url(byte[] octets) =>
        Convert.ToBase64String(octets).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Paire de clés P-256 au format attendu par Web Push (clé publique non compressée, base64url).</summary>
    public static (string Publique, string Privee) GenererClesP256()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parametres = ecdsa.ExportParameters(includePrivateParameters: true);
        var publique = new byte[65];
        publique[0] = 0x04;
        parametres.Q.X!.CopyTo(publique, 1);
        parametres.Q.Y!.CopyTo(publique, 33);
        return (Base64Url(publique), Base64Url(parametres.D!));
    }

    public static IOptions<WebPushOptions> OptionsConfigurees()
    {
        var (publique, privee) = GenererClesP256();
        return Options.Create(new WebPushOptions
        {
            Subject = "mailto:test@local",
            PublicKey = publique,
            PrivateKey = privee,
        });
    }

    public static IOptions<WebPushOptions> OptionsNonConfigurees() => Options.Create(new WebPushOptions());

    /// <summary>Abonnement dont les clés de chiffrement sont valides (générées comme le ferait un navigateur).</summary>
    public static AbonnementPush Abonnement(int carnetSanteId, string endpoint = "https://push.example/abonnement-1") =>
        new()
        {
            CarnetSanteId = carnetSanteId,
            Endpoint = endpoint,
            P256dh = GenererClesP256().Publique,
            Auth = Base64Url(RandomNumberGenerator.GetBytes(16)),
            CreeLe = DateTime.UtcNow,
        };
}

/// <summary>Faux envoi : enregistre les messages et renvoie le résultat configuré par endpoint.</summary>
public sealed class FauxEnvoiPush : IEnvoiPush
{
    public List<(AbonnementPush Abonnement, MessagePush Message)> Envois { get; } = [];
    public Dictionary<string, ResultatEnvoiPush> ResultatsParEndpoint { get; } = [];

    public Task<ResultatEnvoiPush> EnvoyerAsync(AbonnementPush abonnement, MessagePush message, CancellationToken cancellationToken)
    {
        Envois.Add((abonnement, message));
        return Task.FromResult(ResultatsParEndpoint.GetValueOrDefault(abonnement.Endpoint, ResultatEnvoiPush.Envoye));
    }
}

public sealed class HorlogeFixe(DateTimeOffset maintenant) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => maintenant;
}
