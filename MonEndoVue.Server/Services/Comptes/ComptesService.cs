using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services.Consentement;
using MonEndoVue.Server.Services.Email;
using MonEndoVue.Server.Services.Sessions;

namespace MonEndoVue.Server.Services.Comptes;

public enum IssueCompte
{
    /// <summary>Demande prise en compte (un e-mail part si l'adresse le justifie) : la réponse est la même quelle que soit l'adresse.</summary>
    Acceptee,
    Invalide,
    /// <summary>Aucun relais d'e-mails configuré : l'opération est refusée plutôt que perdue.</summary>
    EmailIndisponible,
}

public sealed record ResultatCompte(IssueCompte Issue, IReadOnlyList<string> Erreurs)
{
    public static readonly ResultatCompte Acceptee = new(IssueCompte.Acceptee, []);
    public static readonly ResultatCompte EmailIndisponible = new(IssueCompte.EmailIndisponible, []);
    public static ResultatCompte Invalide(params string[] erreurs) => new(IssueCompte.Invalide, erreurs);
    public static ResultatCompte Invalide(IReadOnlyList<string> erreurs) => new(IssueCompte.Invalide, erreurs);
}

public enum IssueConnexion
{
    Acceptee,
    /// <summary>Adresse inconnue, mot de passe erroné ou compte verrouillé : une seule réponse, rien ne dit qu'un compte existe.</summary>
    Refusee,
    /// <summary>Bon mot de passe mais adresse jamais confirmée : dit seulement à qui prouve connaître le mot de passe.</summary>
    AdresseNonConfirmee,
}

/// <summary>
/// Comptes : inscription avec confirmation de l'adresse, connexion, mot de passe oublié. Jamais d'énumération : une adresse inscrite
/// et une adresse inconnue reçoivent la même réponse dans le même délai (les e-mails partent en arrière-plan) ; c'est l'e-mail qui
/// renseigne la personne réelle. Les liens portent le jeton dans le fragment (voir <see cref="ModelesEmail"/>).
/// </summary>
public class ComptesService(
    UserManager<ApplicationUser> userManager,
    CarnetSanteService carnetSanteService,
    SessionsService sessions,
    IFileEmails emails,
    IOptions<EmailOptions> emailOptions,
    InvitationsService invitations,
    IOptions<InscriptionsOptions> inscriptionsOptions,
    IMemoryCache cache,
    TimeProvider horloge,
    ILogger<ComptesService> logger)
{
    /// <summary>Un e-mail de compte au plus par adresse et par minute (renvois, demandes répétées, inscriptions en rafale).</summary>
    public static readonly TimeSpan DelaiEntreEmails = TimeSpan.FromMinutes(1);

    /// <summary>Compte créé mais jamais confirmé : supprimé passé ce délai (aucune donnée n'a pu y être saisie).</summary>
    public static readonly TimeSpan DelaiConfirmation = TimeSpan.FromDays(7);

    public const string MessageLienInvalide = "Ce lien n'est plus valable. Demande un nouveau lien.";
    public const string MessageConsentement = "Ton accord pour l'utilisation de tes données de santé est nécessaire pour créer un compte.";

    /// <summary>Empreinte d'un mot de passe quelconque : une adresse inconnue coûte le même calcul qu'une adresse inscrite.</summary>
    private static readonly Lazy<string> EmpreinteFactice = new(() => new PasswordHasher<ApplicationUser>().HashPassword(new ApplicationUser(), Guid.NewGuid().ToString()));

    private string AdresseApplication => emailOptions.Value.AdresseApplication ?? string.Empty;

    public async Task<ResultatCompte> InscrireAsync(InscriptionDto inscription, CancellationToken ct)
    {
        if (!inscription.ConsentementDonneesSante) return ResultatCompte.Invalide(MessageConsentement);
        if (!emails.Disponible) return ResultatCompte.EmailIndisponible;
        // Ouverture progressive : sans code valable, rien n'est créé ni envoyé (un code ne dit rien de l'existence d'une adresse).
        var surInvitation = inscriptionsOptions.Value.SurInvitation;
        if (surInvitation && !await invitations.EstValableAsync(inscription.CodeInvitation, ct)) return ResultatCompte.Invalide(InvitationsService.MessageCodeInvalide);

        var email = inscription.Email.Trim();
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = false };
        PolitiqueConfidentialite.Enregistrer(user, horloge.GetUtcNow());
        var creation = await userManager.CreateAsync(user, inscription.Password);

        if (!creation.Succeeded)
        {
            // Adresse déjà inscrite (et mot de passe valable) : même réponse qu'une inscription réussie, l'e-mail prévient la personne.
            if (creation.Errors.All(e => MessagesIdentite.CodesAdresseDejaPrise.Contains(e.Code)))
            {
                var existant = await userManager.FindByEmailAsync(email);
                if (existant is null) return ResultatCompte.Acceptee;
                if (!existant.EmailConfirmed)
                {
                    // Compte jamais confirmé, peut-être créé par quelqu'un d'autre avec cette adresse : le dernier mot de passe choisi le
                    // remplace (l'empreinte de sécurité change, les anciens liens de confirmation ne valent plus) ; seule la personne qui
                    // reçoit les e-mails pourra l'activer.
                    await userManager.RemovePasswordAsync(existant);
                    await userManager.AddPasswordAsync(existant, inscription.Password);
                    if (PeutEnvoyer(email)) emails.Planifier(await MessageConfirmationAsync(existant));
                }
                else if (PeutEnvoyer(email))
                {
                    emails.Planifier(ModelesEmail.CompteExistant(email, $"{AdresseApplication.TrimEnd('/')}/login", $"{AdresseApplication.TrimEnd('/')}/mot-de-passe-oublie"));
                }
                return ResultatCompte.Acceptee;
            }
            return ResultatCompte.Invalide(MessagesIdentite.Traduire(creation.Errors.Where(e => !MessagesIdentite.CodesAdresseDejaPrise.Contains(e.Code))));
        }

        await carnetSanteService.CreateCarnetSante(user.Id);
        if (surInvitation) await invitations.ConsommerAsync(inscription.CodeInvitation!, ct);
        PeutEnvoyer(email);
        emails.Planifier(await MessageConfirmationAsync(user));
        logger.LogInformation("Compte créé, en attente de confirmation : {UserId}", user.Id);
        return ResultatCompte.Acceptee;
    }

    public async Task<ResultatCompte> ConfirmerAdresseAsync(string email, string jeton)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null) return ResultatCompte.Invalide(MessageLienInvalide);
        if (user.EmailConfirmed) return ResultatCompte.Acceptee;

        var confirmation = await userManager.ConfirmEmailAsync(user, jeton);
        if (!confirmation.Succeeded) return ResultatCompte.Invalide(MessageLienInvalide);

        logger.LogInformation("Adresse confirmée : {UserId}", user.Id);
        return ResultatCompte.Acceptee;
    }

    /// <summary>Renvoie le lien de confirmation ; même réponse pour une adresse inconnue ou déjà confirmée.</summary>
    public async Task<ResultatCompte> RenvoyerConfirmationAsync(string email)
    {
        if (!emails.Disponible) return ResultatCompte.EmailIndisponible;
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is { EmailConfirmed: false } && PeutEnvoyer(user.Email!)) emails.Planifier(await MessageConfirmationAsync(user));
        return ResultatCompte.Acceptee;
    }

    /// <summary>
    /// Mot de passe oublié : un lien de réinitialisation pour tout compte existant, rien pour une adresse inconnue ; la réponse est
    /// toujours la même. Un compte non confirmé reçoit aussi un lien de réinitialisation (jamais de confirmation) : choisir un mot de
    /// passe par ce lien confirme l'adresse et écarte un mot de passe posé par quelqu'un d'autre (pré-détournement de compte).
    /// </summary>
    public async Task<ResultatCompte> DemanderReinitialisationAsync(string email)
    {
        if (!emails.Disponible) return ResultatCompte.EmailIndisponible;
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null || !PeutEnvoyer(user.Email!)) return ResultatCompte.Acceptee;

        var jeton = await userManager.GeneratePasswordResetTokenAsync(user);
        emails.Planifier(ModelesEmail.ReinitialisationMotDePasse(user.Email!, ModelesEmail.LienReinitialisation(AdresseApplication, user.Email!, jeton)));
        return ResultatCompte.Acceptee;
    }

    /// <summary>
    /// Nouveau mot de passe par lien : toutes les sessions sont fermées (un éventuel intrus est déconnecté), le verrouillage est levé,
    /// et l'adresse est tenue pour confirmée (le lien prouve qu'elle est entre les mains de la personne).
    /// </summary>
    public async Task<ResultatCompte> ReinitialiserAsync(ReinitialisationMotDePasseDto demande, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(demande.Email.Trim());
        if (user is null) return ResultatCompte.Invalide(MessageLienInvalide);

        var resultat = await userManager.ResetPasswordAsync(user, demande.Jeton, demande.NouveauMotDePasse);
        if (!resultat.Succeeded)
        {
            return resultat.Errors.Any(e => e.Code == "InvalidToken")
                ? ResultatCompte.Invalide(MessageLienInvalide)
                : ResultatCompte.Invalide(MessagesIdentite.Traduire(resultat.Errors));
        }

        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);
        await userManager.ResetAccessFailedCountAsync(user);
        await userManager.SetLockoutEndDateAsync(user, null);
        await sessions.RevoquerAutresAsync(user.Id, jetonCourant: null, ct);
        logger.LogInformation("Mot de passe réinitialisé, sessions fermées : {UserId}", user.Id);
        return ResultatCompte.Acceptee;
    }

    /// <summary>Vérifie des identifiants de connexion ; les échecs comptent pour le verrouillage comme avant.</summary>
    public async Task<(IssueConnexion Issue, ApplicationUser? User)> VerifierConnexionAsync(string email, string motDePasse)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            userManager.PasswordHasher.VerifyHashedPassword(new ApplicationUser(), EmpreinteFactice.Value, motDePasse);
            return (IssueConnexion.Refusee, null);
        }

        if (await userManager.VerifierAsync(user, motDePasse) != IssueVerificationMotDePasse.Correct) return (IssueConnexion.Refusee, null);
        return user.EmailConfirmed ? (IssueConnexion.Acceptee, user) : (IssueConnexion.AdresseNonConfirmee, null);
    }

    private async Task<MessageEmail> MessageConfirmationAsync(ApplicationUser user)
    {
        var jeton = await userManager.GenerateEmailConfirmationTokenAsync(user);
        return ModelesEmail.Confirmation(user.Email!, ModelesEmail.LienConfirmation(AdresseApplication, user.Email!, jeton));
    }

    /// <summary>Faux si un e-mail est déjà parti vers cette adresse depuis moins d'une minute (l'empreinte seule est gardée en mémoire).</summary>
    private bool PeutEnvoyer(string email)
    {
        var cle = "email-compte:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToUpperInvariant())));
        if (cache.TryGetValue(cle, out _)) return false;
        cache.Set(cle, true, DelaiEntreEmails);
        return true;
    }
}
