using System.Net;
using System.Text.Json;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.Extensions.Options;
using MonEndoVue.Server.Models;
using PushSubscription = Lib.Net.Http.WebPush.PushSubscription;

namespace MonEndoVue.Server.Services.WebPush;

/// <summary>
/// Envoi Web Push standard (RFC 8030/8291/8292) : authentification VAPID et chiffrement aes128gcm,
/// requis notamment par le service push d'Apple (iOS 16.4+, app ajoutée à l'écran d'accueil).
/// </summary>
public class WebPushService : IEnvoiPush
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly PushServiceClient? _client;
    private readonly ILogger<WebPushService> _logger;

    public WebPushService(HttpClient httpClient, IOptions<WebPushOptions> options, ILogger<WebPushService> logger)
    {
        _logger = logger;
        var config = options.Value;
        if (!config.EstConfiguree)
        {
            return;
        }

        _client = new PushServiceClient(httpClient)
        {
            DefaultAuthentication = new VapidAuthentication(config.PublicKey, config.PrivateKey)
            {
                Subject = config.Subject,
            },
            DefaultAuthenticationScheme = VapidAuthenticationScheme.Vapid,
        };
    }

    public static string SerialiserMessage(MessagePush message) =>
        JsonSerializer.Serialize(new { title = message.Titre, body = message.Texte, url = message.Url }, JsonOptions);

    public async Task<ResultatEnvoiPush> EnvoyerAsync(
        AbonnementPush abonnement, MessagePush message, CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            return ResultatEnvoiPush.NonConfigure;
        }

        var subscription = new PushSubscription { Endpoint = abonnement.Endpoint };
        subscription.SetKey(PushEncryptionKeyName.P256DH, abonnement.P256dh);
        subscription.SetKey(PushEncryptionKeyName.Auth, abonnement.Auth);

        try
        {
            await _client.RequestPushMessageDeliveryAsync(
                subscription,
                new PushMessage(SerialiserMessage(message)) { TimeToLive = 12 * 3600 },
                cancellationToken);
            return ResultatEnvoiPush.Envoye;
        }
        catch (PushServiceClientException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
        {
            return ResultatEnvoiPush.AbonnementExpire;
        }
        catch (Exception ex) when (ex is PushServiceClientException or HttpRequestException)
        {
            _logger.LogWarning(ex, "Web Push delivery failed for subscription {AbonnementId}", abonnement.Id);
            return ResultatEnvoiPush.Echec;
        }
    }
}
