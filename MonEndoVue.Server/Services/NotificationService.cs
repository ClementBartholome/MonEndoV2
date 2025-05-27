// using FirebaseAdmin;
// using FirebaseAdmin.Messaging;
// using Google.Apis.Auth.OAuth2;
// using MonEndoVue.Server.Data;
// using Microsoft.EntityFrameworkCore;
// using MonEndoVue.Server.Models;
//
// namespace MonEndoVue.Server.Services;
//
// public class NotificationService(
//     IServiceProvider serviceProvider,
//     ILogger<NotificationService> logger)
// {
//     private readonly SemaphoreSlim _tokenRefreshSemaphore = new(1, 1);
//     private string? _cachedAccessToken;
//     private DateTime _tokenExpiryTime = DateTime.MinValue;
//     private readonly FirebaseMessaging _messaging = FirebaseMessaging.DefaultInstance;
//     
//     private const int MaxRetries = 3;
//     private const int RetryDelaySeconds = 30;
//
//     public async Task SendNotifications(CancellationToken stoppingToken)
//     {
//         try
//         {
//             using var scope = serviceProvider.CreateScope();
//             var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//
//             var users = await GetUsersNeedingNotification(dbContext, stoppingToken);
//             logger.LogInformation("Found {UserCount} users to notify", users.Count);
//
//             foreach (var user in users)
//             {
//                 if (stoppingToken.IsCancellationRequested)
//                 {
//                     logger.LogInformation("Notification sending cancelled");
//                     break;
//                 }
//
//                 if (string.IsNullOrEmpty(_cachedAccessToken) || DateTime.UtcNow >= _tokenExpiryTime)
//                 {
//                     await RefreshTokenWithRetry(stoppingToken);
//                 }
//
//                 foreach (var deviceToken in user.DeviceToken)
//                 {
//                     await SendNotificationWithRetry(user, deviceToken, stoppingToken);
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             logger.LogError(ex, "An error occurred while processing notifications");
//             throw;
//         }
//     }
//
//     private async Task<List<ApplicationUser>> GetUsersNeedingNotification(AppDbContext dbContext, CancellationToken stoppingToken)
//     {
//         return await dbContext.Users
//             .Include(u => u.CarnetSante)
//             .ThenInclude(c => c.BilansQuotidiens)
//             .Include(u => u.DeviceToken)
//             .Where(u => u.CarnetSante != null &&
//                        u.CarnetSante.BilansQuotidiens.All(b => b.Date.Date != DateTime.Today))
//             .ToListAsync(stoppingToken);
//     }
//
//     // private async Task RefreshTokenWithRetry(CancellationToken stoppingToken)
//     // {
//     //     const int maxAttempts = 3;
//     //     var attempt = 0;
//     //     var baseDelay = TimeSpan.FromSeconds(5);
//     //     const string authUri = "https://www.googleapis.com/auth/firebase.messaging";
//     //
//     //     while (attempt < maxAttempts)
//     //     {
//     //         attempt++;
//     //         logger.LogInformation("Starting token refresh attempt {Attempt} of {MaxAttempts}", attempt, maxAttempts);
//     //
//     //         try
//     //         {
//     //             // Attendez le sémaphore sans timeout
//     //             await _tokenRefreshSemaphore.WaitAsync(stoppingToken);
//     //             try
//     //             {
//     //                 var app = FirebaseApp.DefaultInstance;
//     //                 if (app?.Options.Credential == null)
//     //                 {
//     //                     logger.LogError("Firebase credentials not found");
//     //                     throw new InvalidOperationException("Firebase credentials not properly configured");
//     //                 }
//     //
//     //                 logger.LogDebug("Creating scoped credential");
//     //                 var scoped = app.Options.Credential.CreateScoped(authUri);
//     //                 
//     //                 // Utilisez directement le stoppingToken sans créer de timeout supplémentaire
//     //                 logger.LogDebug("Requesting new access token");
//     //                 var token = await scoped.UnderlyingCredential.GetAccessTokenForRequestAsync(authUri, stoppingToken);
//     //                 
//     //                 if (string.IsNullOrEmpty(token))
//     //                 {
//     //                     throw new InvalidOperationException("Received empty token from Firebase");
//     //                 }
//     //
//     //                 _cachedAccessToken = token;
//     //                 _tokenExpiryTime = DateTime.UtcNow.AddMinutes(50);
//     //
//     //                 logger.LogInformation("Successfully refreshed Firebase token");
//     //                 return;
//     //             }
//     //             catch (Exception ex)
//     //             {
//     //                 logger.LogError(ex, "Error occurred while requesting access token");
//     //                 throw;
//     //             }
//     //             finally
//     //             {
//     //                 _tokenRefreshSemaphore.Release();
//     //             }
//     //         }
//     //         catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
//     //         {
//     //             logger.LogWarning("Token refresh cancelled by request");
//     //             throw;
//     //         }
//     //         catch (Exception ex)
//     //         {
//     //             if (attempt >= maxAttempts)
//     //             {
//     //                 logger.LogError(ex, "Failed to refresh token after {Attempts} attempts", maxAttempts);
//     //                 throw new InvalidOperationException(
//     //                     "The access token has expired and could not be refreshed", 
//     //                     ex);
//     //             }
//     //
//     //             var delay = TimeSpan.FromSeconds(baseDelay.TotalSeconds * Math.Pow(2, attempt - 1));
//     //             logger.LogWarning(ex, 
//     //                 "Token refresh attempt {Attempt} failed. Retrying in {Delay}s", 
//     //                 attempt, 
//     //                 delay.TotalSeconds);
//     //
//     //             try
//     //             {
//     //                 await Task.Delay(delay, stoppingToken);
//     //             }
//     //             catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
//     //             {
//     //                 logger.LogWarning("Retry delay cancelled by request");
//     //                 throw;
//     //             }
//     //         }
//     //     }
//     // }
//
//     // private async Task SendNotificationWithRetry(ApplicationUser user, DeviceToken deviceToken, CancellationToken stoppingToken)
//     // {
//     //     var message = new Message
//     //     {
//     //         Token = deviceToken.Token,
//     //         Notification = new Notification
//     //         {
//     //             Title = "Rappel",
//     //             Body = "N'oublie pas de remplir ton bilan quotidien !"
//     //         },
//     //         Android = new AndroidConfig
//     //         {
//     //             TimeToLive = TimeSpan.FromHours(1),
//     //             Priority = Priority.High,
//     //         },
//     //         Apns = new ApnsConfig { Aps = new Aps { ContentAvailable = true } }
//     //     };
//     //
//     //     for (var attempt = 1; attempt <= MaxRetries; attempt++)
//     //     {
//     //         try
//     //         {
//     //             if (string.IsNullOrEmpty(_cachedAccessToken) || DateTime.UtcNow >= _tokenExpiryTime)
//     //             {
//     //                 logger.LogWarning("No valid token available. Skipping notification for {UserName}", user.UserName);
//     //                 await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), stoppingToken);
//     //                 continue;
//     //             }
//     //
//     //             await _messaging.SendAsync(message, stoppingToken);
//     //             logger.LogInformation(
//     //                 "Notification sent to {UserName} on device {DeviceToken}",
//     //                 user.UserName, deviceToken.Token);
//     //             return;
//     //         }
//     //         catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
//     //         {
//     //             logger.LogWarning("Notification sending cancelled for {UserName}", user.UserName);
//     //             throw;
//     //         }
//     //         catch (Exception ex)
//     //         {
//     //             var shouldRetry = ShouldRetryException(ex);
//     //             if (attempt == MaxRetries || !shouldRetry)
//     //             {
//     //                 logger.LogError(ex,
//     //                     "Failed to send notification to {UserName} after {RetryCount} attempts",
//     //                     user.UserName, attempt);
//     //
//     //                 if (IsInvalidTokenException(ex))
//     //                 {
//     //                     await RemoveInvalidToken(deviceToken, user, stoppingToken);
//     //                 }
//     //                 break;
//     //             }
//     //
//     //             logger.LogWarning(
//     //                 "Attempt {Attempt} failed for {UserName}. Retrying after {Delay} seconds...",
//     //                 attempt, user.UserName, RetryDelaySeconds);
//     //             
//     //             try
//     //             {
//     //                 await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), stoppingToken);
//     //             }
//     //             catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
//     //             {
//     //                 logger.LogWarning("Retry delay cancelled for {UserName}", user.UserName);
//     //                 throw;
//     //             }
//     //         }
//     //     }
//     // }
//
//     private async Task RemoveInvalidToken(DeviceToken deviceToken, ApplicationUser user, CancellationToken stoppingToken)
//     {
//         try
//         {
//             using var scope = serviceProvider.CreateScope();
//             var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//
//             dbContext.DeviceTokens.Remove(deviceToken);
//             await dbContext.SaveChangesAsync(stoppingToken);
//
//             logger.LogInformation(
//                 "Removed invalid token for user {UserName}",
//                 user.UserName);
//         }
//         catch (Exception ex)
//         {
//             logger.LogError(ex,
//                 "Failed to remove invalid token for user {UserName}",
//                 user.UserName);
//         }
//     }
//
//     private static bool ShouldRetryException(Exception ex)
//     {
//         if (ex is FirebaseMessagingException firebaseEx)
//         {
//             return firebaseEx.ErrorCode is ErrorCode.Unavailable or ErrorCode.Internal or ErrorCode.DeadlineExceeded;
//         }
//
//         return ex is TaskCanceledException or TimeoutException;
//     }
//
//     private static bool IsInvalidTokenException(Exception ex)
//     {
//         if (ex is FirebaseMessagingException firebaseEx)
//         {
//             return firebaseEx.ErrorCode == ErrorCode.InvalidArgument ||
//                    firebaseEx.ErrorCode == ErrorCode.NotFound ||
//                    firebaseEx.ErrorCode == ErrorCode.PermissionDenied;
//         }
//
//         return false;
//     }
// }

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Hubs;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services;

public class NotificationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationService> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly HttpClient _httpClient;
    private const string AppId = "d3434227-a679-4122-b83d-3d1a4e7c1b19";
    private const string OneSignalApiUrl = "https://api.onesignal.com/notifications";

    public NotificationService(
        IServiceProvider serviceProvider,
        ILogger<NotificationService> logger,
        IHubContext<NotificationHub> hubContext,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _hubContext = hubContext;
        
        var oneSignalApiKey = configuration["OneSignal:ApiKey"];

        // Configuration du handler HTTP personnalisé
        var handler = new HttpClientHandler
        {
            Proxy = new WebProxy
            {
                // Ne pas utiliser de proxy pour OneSignal
                BypassProxyOnLocal = true,
                BypassList = new[] { "api.onesignal.com" } 
            },
            UseProxy = true // On garde UseProxy à true pour les autres domaines
        };

        
        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {oneSignalApiKey}");
    }

    public async Task SendNotifications(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var users = await GetUsersNeedingNotification(dbContext, stoppingToken);
            _logger.LogInformation("Found {UserCount} users to notify", users.Count);

            if (users.Count != 0)
            {
                const string notificationMessage = "Notification à envoyer";
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", notificationMessage, stoppingToken);

                // await SendOneSignalNotification(stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing notifications");
            throw;
        }
    }

    private async Task<List<ApplicationUser>> GetUsersNeedingNotification(AppDbContext dbContext, CancellationToken stoppingToken)
    {
        return await dbContext.Users
            .Include(u => u.CarnetSante)
            .ThenInclude(c => c.BilansQuotidiens)
            .Where(u => u.CarnetSante != null &&
                       u.CarnetSante.BilansQuotidiens.All(b => b.Date.Date != DateTime.Today) &&
                       u.UserName == "coralie.owczaruk@yahoo.fr")
            .ToListAsync(stoppingToken);
    }

    private async Task SendOneSignalNotification(CancellationToken stoppingToken)
    {
        var notification = new
        {
            app_id = AppId,
            target_channel = "push",
            included_segments = new[] { "Total Subscriptions" },
            contents = new
            {
                en = "Don't forget to fill in your daily report!",
                fr = "N'oublie pas de remplir ton bilan quotidien !",
            },
            url = "https://monendoapp.fr/bilan-quotidien",
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(OneSignalApiUrl, notification, stoppingToken);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("OneSignal notification sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OneSignal notification");
            throw;
        }
    }
}