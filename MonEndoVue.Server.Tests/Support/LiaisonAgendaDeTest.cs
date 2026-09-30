using System.Net;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Services.Agenda;

namespace MonEndoVue.Server.Tests.Support;

/// <summary>Outils de test de la liaison de l'agenda : configuration OAuth, faux Google, service prêt à l'emploi.</summary>
public static class LiaisonAgendaDeTest
{
    public const string Code = "code-de-test";
    public const string JetonAcces = "acces-de-test";
    public const string JetonActualisation = "actualisation-de-test";

    public static IOptions<GoogleOAuthOptions> Options(bool configuree = true) =>
        Microsoft.Extensions.Options.Options.Create(configuree
            ? new GoogleOAuthOptions
            {
                ClientId = "client-de-test",
                ClientSecret = "secret-de-test",
                RedirectUri = "https://monendo.test/Agenda/liaison/callback",
            }
            : new GoogleOAuthOptions());

    /// <summary>Faux Google OAuth : répond aux échanges et actualisations de jetons et à la révocation.</summary>
    public static FauxGoogleCalendar FauxGoogle(
        Func<HttpRequestMessage, HttpResponseMessage>? repondre = null) =>
        new(repondre ?? (_ => Jetons()));

    public static HttpResponseMessage Jetons(string? actualisation = JetonActualisation, int validite = 3600) =>
        Json(HttpStatusCode.OK,
            $"{{\"access_token\":\"{JetonAcces}\",\"expires_in\":{validite}" +
            (actualisation is null ? "" : $",\"refresh_token\":\"{actualisation}\"") + "}");

    public static HttpResponseMessage Erreur(string erreur) => Json(HttpStatusCode.BadRequest, $"{{\"error\":\"{erreur}\"}}");

    private static HttpResponseMessage Json(HttpStatusCode statut, string json) =>
        new(statut) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    public static LiaisonAgendaService Service(
        AppDbContext context,
        FauxGoogleCalendar google,
        IOptions<GoogleOAuthOptions>? options = null,
        DateTimeOffset? maintenant = null,
        IDataProtectionProvider? protection = null,
        IMemoryCache? cache = null)
    {
        var configuration = options ?? Options();
        return new LiaisonAgendaService(
            context,
            new GoogleOAuthClient(new HttpClient(google), configuration, NullLogger<GoogleOAuthClient>.Instance),
            configuration,
            protection ?? Protection(),
            cache ?? new MemoryCache(new MemoryCacheOptions()),
            new HorlogeFixe(maintenant ?? AgendaDeTest.Maintenant),
            NullLogger<LiaisonAgendaService>.Instance);
    }

    /// <summary>Clés en mémoire : partager la même instance pour relire un jeton protégé par un autre service.</summary>
    public static IDataProtectionProvider Protection() => new EphemeralDataProtectionProvider();

    /// <summary>Service dont tout appel à Google échoue : pour les tests qui ne doivent jamais l'atteindre.</summary>
    public static LiaisonAgendaService ServiceSansGoogle(AppDbContext context) =>
        Service(context, FauxGoogle(_ => throw new InvalidOperationException("Appel à Google inattendu")));
}
