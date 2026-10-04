using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Consentement;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Consentement porté par le jeton d'accès et exigé par le filtre global.</summary>
public class ConsentementTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("2020-01-01", false)]
    [InlineData(PolitiqueConfidentialite.Version, true)]
    public void EstAJour_SelonLaVersionAcceptee(string? version, bool attendu)
    {
        Assert.Equal(attendu, PolitiqueConfidentialite.EstAJour(new ApplicationUser { VersionPolitiqueAcceptee = version }));
    }

    [Fact]
    public void GenerateAccessToken_AvecConsentement_PorteLaVersionAcceptee()
    {
        var jeton = Lire(Jetons().GenerateAccessToken(Utilisatrice(PolitiqueConfidentialite.Version)).token);

        Assert.Contains(jeton.Claims, c => c.Type == PolitiqueConfidentialite.TypeClaim && c.Value == PolitiqueConfidentialite.Version);
    }

    [Fact]
    public void GenerateAccessToken_SansConsentement_NePortePasDeVersion()
    {
        var jeton = Lire(Jetons().GenerateAccessToken(Utilisatrice(null)).token);

        Assert.DoesNotContain(jeton.Claims, c => c.Type == PolitiqueConfidentialite.TypeClaim);
    }

    [Fact]
    public void Filtre_RequeteAnonyme_LaissePasser()
    {
        var contexte = Contexte(new ClaimsPrincipal(new ClaimsIdentity()));

        new ExigeConsentementFilter().OnAuthorization(contexte);

        Assert.Null(contexte.Result);
    }

    [Fact]
    public void Filtre_ConsentementAJour_LaissePasser()
    {
        var contexte = Contexte(Connectee(PolitiqueConfidentialite.Version));

        new ExigeConsentementFilter().OnAuthorization(contexte);

        Assert.Null(contexte.Result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2020-01-01")]
    public void Filtre_ConsentementAbsentOuPerime_Refuse403AvecLeCode(string? version)
    {
        var contexte = Contexte(Connectee(version));

        new ExigeConsentementFilter().OnAuthorization(contexte);

        var refus = Assert.IsType<ObjectResult>(contexte.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, refus.StatusCode);
        Assert.Equal(PolitiqueConfidentialite.CodeConsentementRequis, refus.Value!.GetType().GetProperty("code")!.GetValue(refus.Value));
    }

    [Fact]
    public void Filtre_EndpointSansConsentement_LaissePasser()
    {
        var contexte = Contexte(Connectee(null), new SansConsentementAttribute());

        new ExigeConsentementFilter().OnAuthorization(contexte);

        Assert.Null(contexte.Result);
    }

    private static TokenService Jetons() => new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { [CleSignatureJwt.Reglage] = "cle-de-test-uniquement-pour-les-tests-unitaires" })
        .Build());

    private static ApplicationUser Utilisatrice(string? version) =>
        new() { Id = "u1", UserName = "u1@local", VersionPolitiqueAcceptee = version };

    private static JwtSecurityToken Lire(string jeton) => new JwtSecurityTokenHandler().ReadJwtToken(jeton);

    private static ClaimsPrincipal Connectee(string? version)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "u1") };
        if (version != null) claims.Add(new Claim(PolitiqueConfidentialite.TypeClaim, version));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static AuthorizationFilterContext Contexte(ClaimsPrincipal utilisatrice, params object[] metadonnees)
    {
        var action = new ActionContext(new DefaultHttpContext { User = utilisatrice }, new RouteData(),
            new ActionDescriptor { EndpointMetadata = metadonnees.ToList() });
        return new AuthorizationFilterContext(action, []);
    }
}
