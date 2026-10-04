using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
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
        ILogger<AccountController> logger
        )
        : ControllerBase
    {
        
        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register([FromBody] InscriptionDto identifiants)
        {
            // Consentement explicite obligatoire (RGPD, art. 9.2.a) : même format d'erreur que les erreurs d'Identity.
            if (!identifiants.ConsentementDonneesSante)
            {
                return BadRequest(new[] { "Ton accord pour l'utilisation de tes données de santé est nécessaire pour créer un compte." });
            }

            var user = new ApplicationUser { UserName = identifiants.Email, Email = identifiants.Email, EmailConfirmed = true };
            PolitiqueConfidentialite.Enregistrer(user, horloge.GetUtcNow());
            var result = await userManager.CreateAsync(user, identifiants.Password);

            if (!result.Succeeded)
            {
                var translatedErrors = TranslateErrors(result.Errors);
                return BadRequest(translatedErrors);
            }
            await carnetSanteService.CreateCarnetSante(user.Id);

            var tokenExpiry = await OuvrirSessionAsync(user);

            var carnetSante = await carnetSanteService.GetCarnetSanteByUserId(user.Id);
            return Ok(new { TokenExpiry = tokenExpiry, user.UserName, CarnetSanteId = carnetSante.Id, ConsentementAJour = PolitiqueConfidentialite.EstAJour(user) });
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login([FromBody] IdentifiantsDto identifiants)
        {
            try
            {
                var user = await userManager.FindByEmailAsync(identifiants.Email);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Compte verrouillé ou mot de passe erroné : même réponse, pour ne pas révéler qu'un compte existe.
                var result = await signInManager.CheckPasswordSignInAsync(user, identifiants.Password, lockoutOnFailure: true);
                if (!result.Succeeded) return Unauthorized();

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
                    return BadRequest(new[] { TranslateError(new IdentityError { Code = "PasswordMismatch" }) });
            }

            var result = await userManager.ChangePasswordAsync(user, changement.CurrentPassword, changement.NewPassword);
            if (result.Succeeded)
            {
                // Une session volée ne survit pas au changement de mot de passe : seuls les autres appareils sont déconnectés.
                await sessions.RevoquerAutresAsync(user.Id, Request.Cookies["refreshToken"]);
                return Ok();
            }
            
            var errors = TranslateErrors(result.Errors);
            
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

        private static string TranslateError(IdentityError error)
        {
            return error.Code switch
            {
                "PasswordTooShort" => "Le mot de passe doit contenir au moins huit caractères.",
                "PasswordRequiresNonAlphanumeric" => "Le mot de passe doit contenir au moins un caractère spécial.",
                "PasswordRequiresDigit" => "Le mot de passe doit contenir au moins un chiffre.",
                "PasswordRequiresLower" => "Le mot de passe doit contenir au moins une lettre minuscule.",
                "PasswordRequiresUpper" => "Le mot de passe doit contenir au moins une lettre majuscule.",
                "DuplicateUserName" => "Ce nom d'utilisateur est déjà pris.",
                "InvalidEmail" => "L'adresse e-mail est invalide.",
                "DuplicateEmail" => "Cette adresse e-mail est déjà utilisée.",
                "PasswordMismatch" => "Le mot de passe actuel est incorrect.",
                _ => error.Description
            };
        }

        private IEnumerable<string> TranslateErrors(IEnumerable<IdentityError> errors)
        {
            return errors.Select(TranslateError);
        }
    }
}