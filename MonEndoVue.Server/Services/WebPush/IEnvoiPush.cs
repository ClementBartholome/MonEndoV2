using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services.WebPush;

/// <summary>Contenu d'une notification affichée par le service worker <c>push-sw.js</c>.</summary>
public record MessagePush(string Titre, string Texte, string Url);

public enum ResultatEnvoiPush
{
    Envoye,
    /// <summary>L'appareil s'est désabonné (404/410) : l'abonnement doit être supprimé.</summary>
    AbonnementExpire,
    Echec,
    NonConfigure,
}

public interface IEnvoiPush
{
    Task<ResultatEnvoiPush> EnvoyerAsync(AbonnementPush abonnement, MessagePush message, CancellationToken cancellationToken);
}
