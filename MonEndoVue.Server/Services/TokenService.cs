using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services.Consentement;

namespace MonEndoVue.Server.Services
{
    public class TokenService(IConfiguration configuration)
    {
        public (string token, DateTime expiry) GenerateAccessToken(ApplicationUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(configuration["Jwt:Key"] ?? string.Empty);
            var issuer = configuration["Authentication:Schemes:Bearer:ValidIssuer"];
            var audience = configuration
                .GetSection("Authentication:Schemes:Bearer:ValidAudiences")
                .Get<string[]>()?
                .FirstOrDefault();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(ClaimsDe(user)),
                Expires = DateTime.Now.AddMinutes(30),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            return (tokenString, tokenDescriptor.Expires.Value);
        }

        private static IEnumerable<Claim> ClaimsDe(ApplicationUser user)
        {
            yield return new Claim(ClaimTypes.Name, user.UserName!);
            yield return new Claim(ClaimTypes.NameIdentifier, user.Id);
            // Version de la politique acceptée : lue par ExigeConsentementFilter à chaque requête, sans accès à la base.
            if (user.VersionPolitiqueAcceptee is { } version) yield return new Claim(PolitiqueConfidentialite.TypeClaim, version);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}