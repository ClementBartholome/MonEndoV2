using MonEndoVue.Server.Services.SuppressionCompte;
using Quartz;

namespace MonEndoVue.Server.Jobs;

/// <summary>Chaque nuit à 3 h 30 (heure du serveur) : suppression des comptes inactifs depuis 2 ans.</summary>
[DisallowConcurrentExecution]
public class SuppressionComptesInactifsJob(ComptesInactifsService comptesInactifs) : IJob
{
    public const string Cron = "0 30 3 * * ?";

    public Task Execute(IJobExecutionContext context) => comptesInactifs.SupprimerAsync(context.CancellationToken);
}
