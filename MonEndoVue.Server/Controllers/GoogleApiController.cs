using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Web;
using Google.Apis.Calendar.v3;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Services;

namespace MonEndoVue.Server.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class GoogleApiController(IConfiguration config, ILogger<GoogleApiController> logger) : ControllerBase
{
    private readonly string clientId = config["GoogleApi:ClientId"] ?? string.Empty;
    private readonly string clientSecret = config["GoogleApi:ClientSecret"] ?? string.Empty;

    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize(string code, string state)
    {
        logger.LogInformation("Entering Authorize method with code: {Code} and state: {State}", code, state);

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            logger.LogError("Code or state parameter is null or empty");
            return BadRequest("Code or state parameter is null or empty");
        }

        try
        {
            var userId = state;
            
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = new string[] { CalendarService.Scope.Calendar },
            });

            var redirectUri = Request.Scheme + "://" + Request.Host.Value + Url.Action("Authorize", "GoogleApi");
            logger.LogInformation("Redirect URI: {RedirectUri}", redirectUri);
            
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            var tokenResponse = await flow.ExchangeCodeForTokenAsync(userId, code, redirectUri, cts.Token);
            logger.LogInformation("Token response received with refresh token: {RefreshToken}", tokenResponse.RefreshToken);

            var redirectUrl = $"https://monendoapp.fr/?accessToken={tokenResponse.AccessToken}&refreshToken={tokenResponse.RefreshToken}";
            logger.LogInformation("Redirecting to URL: {RedirectUrl}", redirectUrl);

            return Redirect(redirectUrl);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            logger.LogError(ex, "The request to Google API timed out");
            return StatusCode(504, "Gateway Timeout");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the authorization process");
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpGet("authenticate")]
    public IActionResult Authenticate(string userId)
    {
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            },
            Scopes = new string[] { CalendarService.Scope.Calendar }
        });

        var uri = Request.Scheme + "://" + Request.Host.Value + Url.Action("Authorize", "GoogleApi");

        var codeRequestUrl = flow.CreateAuthorizationCodeRequest(uri);
        codeRequestUrl.State = userId; 
        var authorizationUrl = codeRequestUrl.Build();

        return Ok(authorizationUrl);
    }
    
    [HttpGet("refreshToken")]
    public async Task<IActionResult> RefreshToken(string userId, string refreshToken)
    {
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            },
            Scopes = new string[] { CalendarService.Scope.Calendar }
        });

        var response = await flow.RefreshTokenAsync(userId, refreshToken, CancellationToken.None);
        var token = response.AccessToken;

        return Ok(token);
    }
}