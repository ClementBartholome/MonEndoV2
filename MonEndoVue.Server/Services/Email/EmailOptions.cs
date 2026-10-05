namespace MonEndoVue.Server.Services.Email;

/// <summary>
/// Envoi d'e-mails par un relais SMTP (section de configuration « Email »). Le fournisseur est interchangeable : seuls ces
/// réglages changent. Absente ou invalide, la configuration désactive l'envoi (les inscriptions sont alors refusées proprement).
/// </summary>
public class EmailOptions
{
    public const string Section = "Email";

    /// <summary>Serveur du relais (ex. smtp-relay.brevo.com).</summary>
    public string? Hote { get; set; }

    /// <summary>Port avec STARTTLS (587 par défaut).</summary>
    public int Port { get; set; } = 587;

    /// <summary>Identifiant de connexion au relais (fourni par le service, pas forcément l'adresse d'expédition).</summary>
    public string? Utilisateur { get; set; }

    /// <summary>Clé SMTP du relais : un secret, jamais journalisé ni versionné.</summary>
    public string? MotDePasse { get; set; }

    /// <summary>Adresse d'expédition (ex. no-reply@monendoapp.fr), dont le domaine est authentifié chez le fournisseur.</summary>
    public string? Expediteur { get; set; }

    public string NomExpediteur { get; set; } = "MonEndo";

    /// <summary>Adresse publique de l'application, base des liens envoyés par e-mail (ex. https://monendoapp.fr).</summary>
    public string? AdresseApplication { get; set; }

    /// <summary>Ce qui empêche l'envoi, sans jamais citer de valeur secrète ; null si la configuration est valide.</summary>
    public string? Erreur()
    {
        if (string.IsNullOrWhiteSpace(Hote) && string.IsNullOrWhiteSpace(Utilisateur) && string.IsNullOrWhiteSpace(MotDePasse) && string.IsNullOrWhiteSpace(Expediteur))
        {
            return "section Email absente ou vide à la racine de la configuration";
        }

        if (string.IsNullOrWhiteSpace(Hote)) return "Email:Hote est absent";
        if (Port is < 1 or > 65535) return "Email:Port doit être compris entre 1 et 65535";
        if (string.IsNullOrWhiteSpace(Utilisateur)) return "Email:Utilisateur est absent";
        if (string.IsNullOrWhiteSpace(MotDePasse)) return "Email:MotDePasse est absent";
        if (!AdresseValide(Expediteur)) return "Email:Expediteur doit être une adresse e-mail";
        if (!Uri.TryCreate(AdresseApplication, UriKind.Absolute, out var adresse) || adresse.Scheme != Uri.UriSchemeHttps && !adresse.IsLoopback)
        {
            return "Email:AdresseApplication doit être une adresse https absolue (ou locale en développement)";
        }

        return null;
    }

    private static bool AdresseValide(string? adresse) =>
        !string.IsNullOrWhiteSpace(adresse) && System.Net.Mail.MailAddress.TryCreate(adresse, out var analysee) && analysee.Address == adresse;
}
