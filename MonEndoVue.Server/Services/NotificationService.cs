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