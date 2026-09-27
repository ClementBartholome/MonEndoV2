using Microsoft.AspNetCore.Http;
using MonEndoVue.Server.Services;

namespace MonEndoVue.Server.Tests.Services;

public class EntetesSecuriteTests
{
    private static HeaderDictionary Entetes()
    {
        var entetes = new HeaderDictionary();
        EntetesSecurite.Appliquer(entetes);
        return entetes;
    }

    [Fact]
    public void Appliquer_PoseLesEntetesDeSecurite()
    {
        IHeaderDictionary entetes = Entetes();

        Assert.Equal("DENY", entetes.XFrameOptions);
        Assert.Equal("nosniff", entetes.XContentTypeOptions);
        Assert.Equal("strict-origin-when-cross-origin", entetes["Referrer-Policy"]);
        Assert.Equal("camera=(self), microphone=(), geolocation=()", entetes["Permissions-Policy"]);
    }

    [Fact]
    public void Appliquer_PoseLaPolitiqueDeContenuEnModeBloquant()
    {
        IHeaderDictionary entetes = Entetes();

        Assert.Equal(EntetesSecurite.ContentSecurityPolicy, entetes.ContentSecurityPolicy);
        // Une seule politique : la même en « Report-Only » ferait doublon dans la console.
        Assert.Equal(string.Empty, entetes.ContentSecurityPolicyReportOnly.ToString());
    }

    [Theory]
    [InlineData("default-src 'self'")]
    [InlineData("script-src 'self'")]
    [InlineData("object-src 'none'")]
    [InlineData("base-uri 'self'")]
    [InlineData("frame-ancestors 'none'")]
    public void ContentSecurityPolicy_ContientLesDirectivesDeBase(string directive)
    {
        var directives = EntetesSecurite.ContentSecurityPolicy.Split("; ");

        Assert.Contains(directive, directives);
    }

    [Fact]
    public void ContentSecurityPolicy_NAutorisePasDeScriptEnLigneNiEval()
    {
        Assert.DoesNotContain("'unsafe-eval'", EntetesSecurite.ContentSecurityPolicy);
        var scripts = EntetesSecurite.ContentSecurityPolicy.Split("; ").Single(d => d.StartsWith("script-src"));
        Assert.DoesNotContain("'unsafe-inline'", scripts);
    }
}
