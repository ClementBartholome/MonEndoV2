using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services.Agenda;
using MonEndoVue.Server.Services.Photos;

namespace MonEndoVue.Server.Services.SuppressionCompte;

/// <summary>
/// Suppression définitive d'un compte et de toutes ses données (droit à l'effacement, RGPD art. 17, et retrait du
/// consentement) : photos de suivi, toutes les entités du carnet, rappels, abonnements push, carnet et compte Identity.
/// Utilisée à la demande de l'utilisatrice (mot de passe exigé) et, à terme, pour les comptes inactifs.
/// </summary>
public class SuppressionCompteService(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    IStockagePhotos stockagePhotos,
    LiaisonAgendaService liaisonAgenda,
    ILogger<SuppressionCompteService> logger)
{
    public const string MessageMotDePasseIncorrect = "Le mot de passe est incorrect.";

    /// <summary>Suppression demandée par l'utilisatrice connectée, confirmée par son mot de passe.</summary>
    public async Task<ResultatOperation> SupprimerAsync(string userId, string motDePasse, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return ResultatOperation.Echec(StatutOperation.NonAuthentifie);
        if (!await userManager.CheckPasswordAsync(user, motDePasse))
        {
            return ResultatOperation.Echec(StatutOperation.Invalide, MessageMotDePasseIncorrect);
        }

        return await SupprimerDefinitivementAsync(user, ct);
    }

    /// <summary>
    /// Supprime d'abord les photos (hors base) : si le stockage est indisponible, rien n'est supprimé et la demande
    /// peut être refaite. Les données en base partent ensuite en une seule transaction (un seul SaveChanges).
    /// </summary>
    public async Task<ResultatOperation> SupprimerDefinitivementAsync(ApplicationUser user, CancellationToken ct)
    {
        var carnetId = await context.CarnetSantes.Where(c => c.UserId == user.Id).Select(c => (int?)c.Id).SingleOrDefaultAsync(ct);

        if (carnetId is { } id)
        {
            var photos = await context.SymptomesCycles
                .Where(s => s.CarnetSanteId == id && s.PhotoUrl != null && s.PhotoUrl != "")
                .Select(s => s.PhotoUrl!)
                .ToListAsync(ct);
            try
            {
                foreach (var photo in photos) await stockagePhotos.SupprimerAsync(photo, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Suppression du compte du carnet {CarnetSanteId} interrompue : photos inaccessibles", id);
                return ResultatOperation.Echec(StatutOperation.Indisponible,
                    "La suppression n'a pas pu aboutir. Aucune donnée n'a été supprimée : réessaie dans quelques minutes.");
            }

            // Accord Google révoqué avant de perdre le jeton ; un échec est journalisé et ne bloque pas l'effacement.
            await liaisonAgenda.RevoquerPourSuppressionAsync(id, ct);
            await MarquerDonneesDuCarnetAsync(id, ct);
        }

        context.Users.Remove(user);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Compte {UserId} et carnet {CarnetSanteId} supprimés définitivement", user.Id, carnetId);
        return ResultatOperation.Succes();
    }

    /// <summary>Toutes les entités rattachées au carnet, explicitement (les prises et séances ne cascadent pas depuis le traitement).</summary>
    private async Task MarquerDonneesDuCarnetAsync(int carnetId, CancellationToken ct)
    {
        context.DonneesMedicaments.RemoveRange(await context.DonneesMedicaments.Where(e => e.CarnetSanteId == carnetId).ToListAsync(ct));
        context.DonneesTraitementNonMedicamenteux.RemoveRange(await context.DonneesTraitementNonMedicamenteux.Where(e => e.CarnetSanteId == carnetId).ToListAsync(ct));
        context.Medicaments.RemoveRange(await context.Medicaments.Where(e => e.CarnetSanteId == carnetId).ToListAsync(ct));
        context.DonneesDouleurs.RemoveRange(await context.DonneesDouleurs.Where(e => e.CarnetSanteId == carnetId).ToListAsync(ct));
        context.DonneesActivitePhysique.RemoveRange(await context.DonneesActivitePhysique.Where(e => e.CarnetSanteId == carnetId).ToListAsync(ct));
        context.DonneesTransit.RemoveRange(await context.DonneesTransit.Where(e => e.CarnetSanteId == carnetId).ToListAsync(ct));
        context.JourRegles.RemoveRange(await context.JourRegles.Where(e => e.CarnetSanteId == carnetId).ToListAsync(ct));
        context.BilansQuotidiens.RemoveRange(await context.BilansQuotidiens.Where(e => e.CarnetSanteId == carnetId).ToListAsync(ct));
        context.SymptomesCycles.RemoveRange(await context.SymptomesCycles.Where(e => e.CarnetSanteId == carnetId).ToListAsync(ct));
        context.EpisodesAcne.RemoveRange(await context.EpisodesAcne.Where(e => e.CarnetSanteId == carnetId).ToListAsync(ct));
        context.Rappels.RemoveRange(await context.Rappels.Where(e => e.CarnetSanteId == carnetId).ToListAsync(ct));
        context.LiaisonsAgenda.RemoveRange(await context.LiaisonsAgenda.Where(e => e.CarnetSanteId == carnetId).ToListAsync(ct));
        context.AbonnementsPush.RemoveRange(await context.AbonnementsPush.Where(e => e.CarnetSanteId == carnetId).ToListAsync(ct));
        context.CarnetSantes.RemoveRange(await context.CarnetSantes.Where(c => c.Id == carnetId).ToListAsync(ct));
    }
}
