using MonEndoVue.Server.Services.WebPush;
using Quartz;

namespace MonEndoVue.Server.Jobs;

/// <summary>Toutes les 15 minutes : envoie les rappels de bilan dont l'heure locale est atteinte.</summary>
[DisallowConcurrentExecution]
public class RappelBilanJob(NotificationsPushService notifications, ILogger<RappelBilanJob> logger) : IJob
{
    public const string Cron = "0 0/15 * * * ?";

    public async Task Execute(IJobExecutionContext context)
    {
        var notifies = await notifications.EnvoyerRappelsDusAsync(context.CancellationToken);
        if (notifies > 0)
        {
            logger.LogInformation("Daily report reminders sent to {Count} carnet(s)", notifies);
        }
    }
}
