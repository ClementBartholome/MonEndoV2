using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Comptes;
using MonEndoVue.Server.Services.Consentement;
using MonEndoVue.Server.Services.Sessions;

namespace MonEndoVue.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [SansConsentement]
    public class AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        CarnetSanteService carnetSanteService, TokenService tokenService,
        TimeProvider horloge,
        SessionsService sessions,
        ComptesService comptes,
        ILogger<AccountController> logger
        )
        : ControllerBase
    {
        
        public const string CodeAdresseNonConfirmee = "adresse-non-confirmee";
        private const string MessageEmailIndisponible = "Ce service est momentanément indisponible. Réessaie un peu plus tard.";

        /// <summary>
        /// Inscription : le compte est créé <b>non confirmé</b>, sans session ; un lien de confirmation part par e-mail. La réponse
        /// (202) est la même si l'adresse est déjà inscrite : c'est l'e-mail qui prévient la personne. Erreurs : mot de passe ou
        /// consentement (400, liste de messages), aucun relais d'e-mails (503).
        /// </summary>
        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register([FromBody] InscriptionDto identifiants, CancellationToken ct) =>
            Reponse(await comptes.InscrireAsync(identifiants, ct), () => Accepted(new { ConfirmationRequise = true }), listeDErreurs: true);

        /// <summary>Confirme l'adresse avec le jeton du lien (lu dans le fragment par la page) ; sans ouvrir de session.</summary>
        [HttpPost("confirmer-adresse")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ConfirmerAdresse([FromBody] ConfirmationAdresseDto confirmation) =>
            Reponse(await comptes.ConfirmerAdresseAsync(confirmation.Email, confirmation.Jeton), NoContent);

        /// <summary>Renvoie le lien de confirmation (202 quelle que soit l'adresse).</summary>
        [HttpPost("renvoyer-confirmation")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> RenvoyerConfirmation([FromBody] AdresseDto adresse) =>
            Reponse(await comptes.RenvoyerConfirmationAsync(adresse.Email), Accepted);

        /// <summary>Mot de passe oublié : un lien part si un compte existe (202 dans tous les cas).</summary>
        [HttpPost("mot-de-passe-oublie")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> MotDePasseOublie([FromBody] AdresseDto adresse) =>
            Reponse(await comptes.DemanderReinitialisationAsync(adresse.Email), Accepted);

        /// <summary>Nouveau mot de passe depuis le lien ; ferme toutes les sessions du compte.</summary>
        [HttpPost("reinitialiser-mot-de-passe")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ReinitialiserMotDePasse([FromBody] ReinitialisationMotDePasseDto demande, CancellationToken ct) =>
            Reponse(await comptes.ReinitialiserAsync(demande, ct), NoContent);

        private IActionResult Reponse(ResultatCompte resultat, Func<IActionResult> succes, bool listeDErreurs = false) => resultat.Issue switch
        {
            IssueCompte.Acceptee => succes(),
            IssueCompte.EmailIndisponible => StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = MessageEmailIndisponible }),
            // L'inscription garde son ancien format (liste de messages) ; les autres opérations renvoient { message, erreurs }.
            _ => listeDErreurs ? BadRequest(resultat.Erreurs) : BadRequest(new { message = resultat.Erreurs.FirstOrDefault(), erreurs = resultat.Erreurs }),
        };

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login([FromBody] IdentifiantsDto identifiants)
        {
            try
            {
                // Adresse inconnue, compte verrouillé ou mot de passe erroné : même réponse, pour ne pas révéler qu'un compte existe.
                var (issue, user) = await comptes.VerifierConnexionAsync(identifiants.Email, identifiants.Password);
                if (issue == IssueConnexion.AdresseNonConfirmee) return StatusCode(StatusCodes.Status403Forbidden, new { code = CodeAdresseNonConfirmee });
                if (issue != IssueConnexion.Acceptee || user is null) return Unauthorized();

                var tokenExpiry = await OuvrirSessionAsync(user);

                var carnetSante = await carnetSanteService.GetCarnetSanteByUserId(user.Id);
                return Ok(new { TokenExpiry = tokenExpiry, user.UserName, CarnetSanteId = carnetSante.Id, ConsentementAJour = PolitiqueConfidentialite.EstAJour(user) });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Login failed");
                return StatusCode(500, new { message = "Une erreur est survenue lors de la connexion." });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                logger.LogWarning("Refresh token is not provided");
                return BadRequest("Refresh token is not provided");
            }

            // Seule la session de cet appareil est renouvelée : se connecter ailleurs ne la ferme pas.
            var renouvellement = await sessions.RenouvelerAsync(refreshToken);
            if (renouvellement.Issue == IssueRenouvellement.Expiree)
            {
                logger.LogWarning("Expired refresh token");
                return BadRequest("Expired refresh token");
            }

            var user = renouvellement.UserId is null ? null : await userManager.FindByIdAsync(renouvellement.UserId);
            if (user == null || renouvellement.Session is not { } session)
            {
                logger.LogWarning("Invalid refresh token");
                return BadRequest("Invalid refresh token");
            }

            var tokenExpiry = await PoserSessionAsync(user, session);

            logger.LogInformation("Refresh token successfully generated for user: {UserId}", user.Id);
            return Ok(new { TokenExpiry = tokenExpiry });
        }
        
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();

            // Le jeton de renouvellement meurt avec la session : un cookie recopié ne rouvrirait rien.
            if (Request.Cookies["refreshToken"] is { Length: > 0 } cookie)
            {
                await sessions.RevoquerAsync(cookie);
            }
            
            // Efface les cookies actuels (Path=/)
            Response.Cookies.Delete("accessToken", new CookieOptions { Path = "/" });
            Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/" });

            // Efface les anciens cookies qui auraient ete crees avec le path par defaut /Account
            Response.Cookies.Delete("accessToken", new CookieOptions { Path = "/Account" });
            Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/Account" });

            return Ok();
        }
        
        /// <summary>
        /// Enregistre le consentement aux données de santé pour la politique en vigueur (comptes créés avant son recueil,
        /// ou nouvelle version de la politique), puis émet un jeton d'accès qui le porte.
        /// </summary>
        [Authorize]
        [HttpPost("consentement")]
        public async Task<IActionResult> DonnerConsentement()
        {
            var user = await userManager.FindByIdAsync(User.GetCurrentUserId());
            if (user == null) return Unauthorized();

            PolitiqueConfidentialite.Enregistrer(user, horloge.GetUtcNow());
            // Un nouveau jeton d'accès (il porte la version acceptée) sur la session de cet appareil, sans en ouvrir une autre.
            var courante = Request.Cookies["refreshToken"] is { Length: > 0 } cookie ? await sessions.RenouvelerAsync(cookie) : null;
            var tokenExpiry = courante is { Issue: IssueRenouvellement.Renouvelee, UserId: not null, Session: { } existante } && courante.UserId == user.Id
                ? await PoserSessionAsync(user, existante)
                : await PoserSessionAsync(user, await sessions.OuvrirAsync(user.Id));

            logger.LogInformation("Consentement aux données de santé enregistré pour {UserId}", user.Id);
            return Ok(new { TokenExpiry = tokenExpiry, ConsentementAJour = true });
        }

        [Authorize]
        [HttpPost("change-password")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangementMotDePasseDto changement)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest();
            }

            // Les échecs comptent comme à la connexion : une session volée ne peut pas deviner le mot de passe actuel.
            switch (await userManager.VerifierAsync(user, changement.CurrentPassword))
            {
                case IssueVerificationMotDePasse.Verrouille:
                    return StatusCode(StatusCodes.Status429TooManyRequests, new { message = VerificationMotDePasse.MessageTropDeTentatives });
                case IssueVerificationMotDePasse.Incorrect:
                    return BadRequest(new[] { MessagesIdentite.Traduire(new IdentityError { Code = "PasswordMismatch" }) });
            }

            var result = await userManager.ChangePasswordAsync(user, changement.CurrentPassword, changement.NewPassword);
            if (result.Succeeded)
            {
                // Une session volée ne survit pas au changement de mot de passe : seuls les autres appareils sont déconnectés.
                await sessions.RevoquerAutresAsync(user.Id, Request.Cookies["refreshToken"]);
                return Ok();
            }
            
            var errors = MessagesIdentite.Traduire(result.Errors);
            
            return BadRequest(errors);
        }
        
        /// <summary>Ouvre la session d’un nouvel appareil (connexion, inscription).</summary>
        private async Task<DateTime> OuvrirSessionAsync(ApplicationUser user) =>
            await PoserSessionAsync(user, await sessions.OuvrirAsync(user.Id));

        /// <summary>
        /// Émet un jeton d'accès (30 min) et pose avec le jeton de renouvellement de l'appareil (2 jours glissants) en cookies
        /// HttpOnly (Path=/, SameSite=Strict). Le jeton de renouvellement n'est jamais écrit sur l'utilisatrice : seule son empreinte
        /// est en base (<see cref="SessionsService"/>).
        /// </summary>
        private async Task<DateTime> PoserSessionAsync(ApplicationUser user, JetonSession session)
        {
            var (accessToken, tokenExpiry) = tokenService.GenerateAccessToken(user);

            var maintenant = horloge.GetUtcNow();
            user.DerniereActiviteLe = maintenant.UtcDateTime;
            await userManager.UpdateAsync(user);

            Response.Cookies.Append("accessToken", accessToken, CookieSession(maintenant.AddMinutes(30)));
            Response.Cookies.Append("refreshToken", session.Jeton, CookieSession(new DateTimeOffset(DateTime.SpecifyKind(session.Expiration, DateTimeKind.Utc))));

            return tokenExpiry;
        }

        private static CookieOptions CookieSession(DateTimeOffset expiration) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = expiration
        };
    }
}