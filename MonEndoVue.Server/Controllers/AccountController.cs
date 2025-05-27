using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;

namespace MonEndoVue.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        CarnetSanteService carnetSanteService, TokenService tokenService,
        DeviceTokenService deviceTokenService, ILogger<AccountController> logger
        )
        : ControllerBase
    {
        
        [HttpPost("register")]
        public async Task<IActionResult> Register(string email, string password)
        {
            var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var translatedErrors = TranslateErrors(result.Errors);
                return BadRequest(translatedErrors);
            }
            await signInManager.SignInAsync(user, isPersistent: false);
            await carnetSanteService.CreateCarnetSante(user.Id);

            var (accessToken, tokenExpiry) = tokenService.GenerateAccessToken(user);
            var refreshToken = tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(2);
            await userManager.UpdateAsync(user);

            var accessTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddMinutes(30)
            };

            var refreshTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddDays(2)
            };

            Response.Cookies.Append("accessToken", accessToken, accessTokenOptions);
            Response.Cookies.Append("refreshToken", refreshToken, refreshTokenOptions);

            var carnetSante = await carnetSanteService.GetCarnetSanteId(user.Id);
            return Ok(new { AccessToken = accessToken, RefreshToken = refreshToken, TokenExpiry = tokenExpiry, user.UserName, CarnetSanteId = carnetSante.Id });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(string email, string password)
        {
            logger.LogInformation("Login method called with email: {Email}", email);
            try
            {
                var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: false);

                if (!result.Succeeded) return Unauthorized();
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return Unauthorized();
                }

                var (accessToken, tokenExpiry) = tokenService.GenerateAccessToken(user);
                var refreshToken = tokenService.GenerateRefreshToken();
                
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.Now.AddDays(2);
                await userManager.UpdateAsync(user);
                
                var accessTokenOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.Now.AddMinutes(30)
                };
                
                var refreshTokenOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.Now.AddDays(2)
                };
                
                Response.Cookies.Append("accessToken", accessToken, accessTokenOptions);
                Response.Cookies.Append("refreshToken", refreshToken, refreshTokenOptions);

                var carnetSante = await carnetSanteService.GetCarnetSanteId(user.Id);
                return Ok(new { AccessToken = accessToken, RefreshToken = refreshToken, TokenExpiry = tokenExpiry, user.UserName, CarnetSanteId = carnetSante.Id });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                Console.WriteLine(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
            {
                logger.LogWarning("Refresh token is not provided");
                return BadRequest("Refresh token is not provided");
            }

            var user = await userManager.Users.SingleOrDefaultAsync(u => u.RefreshToken == refreshToken);
            if (user == null)
            {
                logger.LogWarning("Invalid refresh token");
                return BadRequest("Invalid refresh token");
            }

            if (user.RefreshTokenExpiryTime <= DateTime.Now)
            {
                logger.LogWarning("Expired refresh token");
                return BadRequest("Expired refresh token");
            }

            var (newAccessToken, tokenExpiry) = tokenService.GenerateAccessToken(user);
            var newRefreshToken = tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(2);
            await userManager.UpdateAsync(user);

            var accessTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddMinutes(30)
            };

            var refreshTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddDays(2)
            };

            Response.Cookies.Append("accessToken", newAccessToken, accessTokenOptions);
            Response.Cookies.Append("refreshToken", newRefreshToken, refreshTokenOptions);

            logger.LogInformation("Refresh token successfully generated for user: {UserId}", user.Id);
            return Ok(new { AccessToken = newAccessToken, RefreshToken = newRefreshToken, TokenExpiry = tokenExpiry });
        }
        
        [HttpPost("check-token-validity")]
        public async Task<IActionResult> CheckTokenValidity([FromBody] TokenRequest tokenRequest)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(tokenRequest.AccessToken) as JwtSecurityToken;

            if (jwtToken == null)
            {
                return BadRequest("Invalid access token");
            }

            if (jwtToken.ValidTo < DateTime.Now)
            {
                return Ok(new { IsValid = false, Message = "Token has expired" });
            }

            var principal = tokenService.GetPrincipalFromExpiredToken(tokenRequest.AccessToken);
            var username = principal.Identity?.Name;
            if (username == null) return BadRequest("Invalid access token");

            var user = await userManager.FindByNameAsync(username);
            if (user == null) return BadRequest("User not found.");

            if (user.RefreshToken != tokenRequest.RefreshToken)
            {
                return Ok(new { IsValid = false, Message = "Invalid refresh token" });
            }

            return Ok(new { IsValid = true });
        }
        
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            
            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");

            return Ok();
        }
        
        [Authorize]
        [HttpPost("force-change-password")]
        public async Task<IActionResult> ForceChangePassword(string userId, string newPassword)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var removePasswordResult = await userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                return BadRequest(removePasswordResult.Errors);
            }

            var addPasswordResult = await userManager.AddPasswordAsync(user, newPassword);
            if (addPasswordResult.Succeeded)
            {
                return Ok();
            }

            return BadRequest(addPasswordResult.Errors);
        }
        
        [Authorize]
        [HttpPost("device-token")]
        public async Task<IActionResult> SaveDeviceToken([FromBody] SaveDeviceTokenRequest request)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest();
            }

            var existingToken = await deviceTokenService.GetExistingTokenAsync(request.DeviceToken, user.Id);

            if (existingToken != null)
            {
                logger.LogInformation("Device token already exists for user: " + user.UserName);
                return Ok();
            }

            await deviceTokenService.SaveDeviceTokenAsync(request.DeviceToken, user.Id);

            return Ok();
        }
        
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest();
            }

            var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                return Ok();
            }
            
            var errors = TranslateErrors(result.Errors);
            
            return BadRequest(errors);
        }
        
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