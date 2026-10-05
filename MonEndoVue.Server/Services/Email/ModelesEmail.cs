using System.Net;

namespace MonEndoVue.Server.Services.Email;

/// <summary>
/// Messages de compte envoyés par e-mail. Règles : aucune donnée de santé, aucun nom, rien d'autre que le lien utile ; le texte
/// reste compréhensible sans HTML. Les liens portent le jeton dans le <b>fragment</b> (<c>#</c>) : un fragment n'est jamais
/// envoyé au serveur, ni aux journaux d'accès, ni en <c>Referer</c> ; la page le lit puis le poste en corps JSON.
/// </summary>
public static class ModelesEmail
{
    /// <summary>Durées annoncées dans les messages (les jetons expirent réellement à ces échéances).</summary>
    public const string ValiditeConfirmation = "24 heures";
    public const string ValiditeReinitialisation = "1 heure";

    public static string LienConfirmation(string adresseApplication, string email, string jeton) =>
        LienAvecFragment(adresseApplication, "/confirmer-adresse", email, jeton);

    public static string LienReinitialisation(string adresseApplication, string email, string jeton) =>
        LienAvecFragment(adresseApplication, "/reinitialiser-mot-de-passe", email, jeton);

    private static string LienAvecFragment(string adresseApplication, string chemin, string email, string jeton) =>
        $"{adresseApplication.TrimEnd('/')}{chemin}#email={Uri.EscapeDataString(email)}&jeton={Uri.EscapeDataString(jeton)}";

    public static MessageEmail Confirmation(string a, string lien) => Composer(
        a,
        "Confirme ton adresse e-mail · MonEndo",
        "Pour finir de créer ton compte MonEndo, confirme ton adresse e-mail.",
        "Confirmer mon adresse",
        lien,
        $"Le lien est valable {ValiditeConfirmation}. Si tu n'es pas à l'origine de cette demande, ignore ce message : le compte ne sera pas activé.");

    public static MessageEmail ReinitialisationMotDePasse(string a, string lien) => Composer(
        a,
        "Réinitialise ton mot de passe · MonEndo",
        "Tu as demandé à réinitialiser ton mot de passe MonEndo. Le lien ci-dessous te permet d'en choisir un nouveau.",
        "Choisir un nouveau mot de passe",
        lien,
        $"Le lien est valable {ValiditeReinitialisation} et ne sert qu'une fois. Si tu n'es pas à l'origine de cette demande, ignore ce message : ton mot de passe reste inchangé.");

    /// <summary>
    /// Réponse à une inscription avec une adresse déjà utilisée : l'écran répond toujours la même chose (pas d'énumération),
    /// c'est ce message qui prévient la personne réelle.
    /// </summary>
    public static MessageEmail CompteExistant(string a, string lienConnexion, string lienOubli) => Composer(
        a,
        "Tu as déjà un compte MonEndo",
        "Quelqu'un (peut-être toi) a essayé de créer un compte MonEndo avec cette adresse, qui est déjà utilisée. Tu peux simplement te connecter.",
        "Me connecter",
        lienConnexion,
        $"Mot de passe oublié ? Tu peux en choisir un nouveau ici : {lienOubli}\nSi tu n'es pas à l'origine de cette demande, ignore ce message : il ne se passe rien.");

    private static MessageEmail Composer(string a, string sujet, string introduction, string libelleBouton, string lien, string pied)
    {
        var texte = $"Bonjour,\n\n{introduction}\n\n{libelleBouton} : {lien}\n\n{pied}\n\nL'équipe MonEndo";
        var html = $$"""
            <!doctype html>
            <html lang="fr">
            <body style="margin:0;padding:24px;background:#faeee7;font-family:Arial,Helvetica,sans-serif;color:#33272a">
              <div style="max-width:520px;margin:0 auto;background:#fffaf7;border-radius:16px;padding:28px">
                <p style="margin:0 0 16px;font-size:16px">Bonjour,</p>
                <p style="margin:0 0 24px;font-size:16px;line-height:1.5">{{Encoder(introduction)}}</p>
                <p style="margin:0 0 24px"><a href="{{Encoder(lien)}}" style="display:inline-block;background:#f2b3c2;color:#33272a;text-decoration:none;font-weight:bold;padding:14px 22px;border-radius:12px">{{Encoder(libelleBouton)}}</a></p>
                <p style="margin:0 0 16px;font-size:13px;line-height:1.5;color:#594a4e">Si le bouton ne s'ouvre pas, copie ce lien dans ton navigateur :<br><span style="word-break:break-all">{{Encoder(lien)}}</span></p>
                <p style="margin:0 0 16px;font-size:13px;line-height:1.5;color:#594a4e">{{Encoder(pied).Replace("\n", "<br>")}}</p>
                <p style="margin:0;font-size:13px;color:#594a4e">L'équipe MonEndo</p>
              </div>
            </body>
            </html>
            """;
        return new MessageEmail(a, sujet, texte, html);
    }

    private static string Encoder(string valeur) => WebUtility.HtmlEncode(valeur);
}
