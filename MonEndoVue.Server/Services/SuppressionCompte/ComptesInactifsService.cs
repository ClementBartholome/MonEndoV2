using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;

namespace MonEndoVue.Server.Services.SuppressionCompte;

/// <summary>
/// Durée de conservation (RGPD art. 5.1.e, annoncée dans la politique de confidentialité) : un compte sans aucune
/// ouverture ni prolongation de session depuis <see cref="DureeInactivite"/> est supprimé avec toutes ses données.
/// </summary>
public class ComptesInactifsService(
    AppDbContext context,
    SuppressionCompteService suppressionCompte,
    TimeProvider horloge,
    ILogger<ComptesInactifsService> logger)
{
    public static readonly TimeSpan DureeInactivite = TimeSpan.FromDays(2 * 365);

    /// <summary>Supprime les comptes inactifs ; renvoie le nombre de comptes supprimés.</summary>
    public async Task<int> SupprimerAsync(CancellationToken ct)
    {
        var limite = horloge.GetUtcNow().UtcDateTime - DureeInactivite;
        var inactifs = await context.Users
            .Where(u => u.DerniereActiviteLe != null && u.DerniereActiviteLe < limite)
            .ToListAsync(ct);

        var supprimes = 0;
        foreach (var user in inactifs)
        {
            try
            {
                var resultat = await suppressionCompte.SupprimerDefinitivementAsync(user, ct);
                if (resultat.Statut == StatutOperation.Succes) supprimes++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Sans identifiant du compte ni donnée de santé ; le compte sera retenté au prochain passage.
                logger.LogError(ex, "Suppression d'un compte inactif impossible");
                context.ChangeTracker.Clear();
            }
        }

        if (inactifs.Count > 0)
        {
            logger.LogInformation("Comptes inactifs depuis 2 ans : {Supprimes} supprimé(s) sur {Trouves}", supprimes, inactifs.Count);
        }
        return supprimes;
    }
}
