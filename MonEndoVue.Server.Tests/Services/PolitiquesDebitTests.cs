using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using MonEndoVue.Server.Services;

namespace MonEndoVue.Server.Tests.Services;

public class PolitiquesDebitTests
{
    private static RouteEndpointBuilder Endpoint(params object[] metadata)
    {
        var builder = new RouteEndpointBuilder(_ => Task.CompletedTask, RoutePatternFactory.Parse("/test"), 0);
        foreach (var item in metadata)
        {
            builder.Metadata.Add(item);
        }
        return builder;
    }

    private static string? PolitiqueEffective(RouteEndpointBuilder builder) =>
        builder.Build().Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;

    [Fact]
    public void AppliquerParDefaut_EndpointSansPolitique_AppliqueLaPolitiqueParDefaut()
    {
        var endpoint = Endpoint();

        PolitiquesDebit.AppliquerParDefaut(endpoint, PolitiquesDebit.Api);

        Assert.Equal(PolitiquesDebit.Api, PolitiqueEffective(endpoint));
    }

    [Fact]
    public void AppliquerParDefaut_ActionAvecPolitiqueAuth_ConserveLaPolitiqueAuth()
    {
        var endpoint = Endpoint(new EnableRateLimitingAttribute(PolitiquesDebit.Auth));

        PolitiquesDebit.AppliquerParDefaut(endpoint, PolitiquesDebit.Api);

        Assert.Equal(PolitiquesDebit.Auth, PolitiqueEffective(endpoint));
    }

    [Fact]
    public void AppliquerParDefaut_ActionSansLimitation_NAjouteAucunePolitique()
    {
        var endpoint = Endpoint(new DisableRateLimitingAttribute());

        PolitiquesDebit.AppliquerParDefaut(endpoint, PolitiquesDebit.Api);

        Assert.Null(PolitiqueEffective(endpoint));
    }
}
