using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MonEndoVue.Server.Services.WebPush;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

public class WebPushServiceTests
{
    private sealed class FauxServicePush(Func<HttpRequestMessage, HttpResponseMessage> repondre) : HttpMessageHandler
    {
        public HttpRequestMessage? DerniereRequete { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            DerniereRequete = request;
            return Task.FromResult(repondre(request));
        }
    }

    private static readonly MessagePush Message = new("MonEndo", "Texte", "/bilan-quotidien");

    private static (WebPushService Service, FauxServicePush Handler) Creer(HttpStatusCode statut, bool configure = true)
    {
        var handler = new FauxServicePush(_ => new HttpResponseMessage(statut));
        var service = new WebPushService(
            new HttpClient(handler),
            configure ? PushDeTest.OptionsConfigurees() : PushDeTest.OptionsNonConfigurees(),
            NullLogger<WebPushService>.Instance);
        return (service, handler);
    }

    [Fact]
    public async Task EnvoyerAsync_SansConfiguration_NEnvoieRien()
    {
        var (service, handler) = Creer(HttpStatusCode.Created, configure: false);

        var resultat = await service.EnvoyerAsync(PushDeTest.Abonnement(1), Message, CancellationToken.None);

        Assert.Equal(ResultatEnvoiPush.NonConfigure, resultat);
        Assert.Null(handler.DerniereRequete);
    }

    [Fact]
    public async Task EnvoyerAsync_ServiceAccepte_EnvoieUnMessageChiffreAes128gcmAvecVapid()
    {
        var (service, handler) = Creer(HttpStatusCode.Created);

        var resultat = await service.EnvoyerAsync(PushDeTest.Abonnement(1), Message, CancellationToken.None);

        Assert.Equal(ResultatEnvoiPush.Envoye, resultat);
        var requete = Assert.IsType<HttpRequestMessage>(handler.DerniereRequete);
        Assert.Equal("https://push.example/abonnement-1", requete.RequestUri!.ToString());
        Assert.Equal("vapid", requete.Headers.Authorization?.Scheme);
        Assert.Contains("aes128gcm", requete.Content!.Headers.ContentEncoding);
    }

    [Theory]
    [InlineData(HttpStatusCode.Gone)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task EnvoyerAsync_AbonnementDisparu_SignaleUnAbonnementExpire(HttpStatusCode statut)
    {
        var (service, _) = Creer(statut);

        var resultat = await service.EnvoyerAsync(PushDeTest.Abonnement(1), Message, CancellationToken.None);

        Assert.Equal(ResultatEnvoiPush.AbonnementExpire, resultat);
    }

    [Fact]
    public async Task EnvoyerAsync_ErreurDuServicePush_SignaleUnEchec()
    {
        var (service, _) = Creer(HttpStatusCode.InternalServerError);

        var resultat = await service.EnvoyerAsync(PushDeTest.Abonnement(1), Message, CancellationToken.None);

        Assert.Equal(ResultatEnvoiPush.Echec, resultat);
    }

    [Fact]
    public async Task EnvoyerAsync_ServicePushInjoignable_SignaleUnEchec()
    {
        var handler = new FauxServicePush(_ => throw new HttpRequestException("injoignable"));
        var service = new WebPushService(new HttpClient(handler), PushDeTest.OptionsConfigurees(), NullLogger<WebPushService>.Instance);

        var resultat = await service.EnvoyerAsync(PushDeTest.Abonnement(1), Message, CancellationToken.None);

        Assert.Equal(ResultatEnvoiPush.Echec, resultat);
    }

    [Fact]
    public void SerialiserMessage_ProduitLeFormatLuParLeServiceWorker()
    {
        var json = JsonDocument.Parse(WebPushService.SerialiserMessage(Message)).RootElement;

        Assert.Equal("MonEndo", json.GetProperty("title").GetString());
        Assert.Equal("Texte", json.GetProperty("body").GetString());
        Assert.Equal("/bilan-quotidien", json.GetProperty("url").GetString());
    }
}
