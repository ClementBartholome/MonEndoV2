using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Auth.OAuth2.Flows;
using Microsoft.Extensions.Configuration;

namespace MonEndoVue.Server.Services;

public class GoogleApiService(IConfiguration config)
{
    private readonly string clientId = config["GoogleApi:ClientId"];
    private readonly string clientSecret = config["GoogleApi:ClientSecret"];

    public Task<string> GetAuthorizationUrl(string redirectUri)
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

        var authorizationUrl = flow.CreateAuthorizationCodeRequest(redirectUri);
        authorizationUrl.RedirectUri = redirectUri;
        return Task.FromResult(authorizationUrl.Build().ToString());
    }
}