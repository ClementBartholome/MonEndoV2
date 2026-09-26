namespace MonEndoVue.Server.Services.WebPush;

/// <summary>Clés VAPID (section de configuration « WebPush »). Sans elles, l'envoi de notifications est désactivé.</summary>
public class WebPushOptions
{
    public const string Section = "WebPush";

    /// <summary>Contact de l'émetteur, exigé par les services push (ex. mailto:contact@exemple.fr).</summary>
    public string? Subject { get; set; }
    public string? PublicKey { get; set; }
    public string? PrivateKey { get; set; }

    public bool EstConfiguree =>
        !string.IsNullOrWhiteSpace(Subject) &&
        !string.IsNullOrWhiteSpace(PublicKey) &&
        !string.IsNullOrWhiteSpace(PrivateKey);
}
