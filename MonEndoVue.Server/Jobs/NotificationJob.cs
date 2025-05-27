using MonEndoVue.Server.Services;
using Quartz;

namespace MonEndoVue.Server.Jobs;

public class NotificationJob(
    ILogger<NotificationJob> logger, NotificationService notificationService)
    : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try 
        {
            await notificationService.SendNotifications(context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing notification job");
            throw; 
        }
    }
}