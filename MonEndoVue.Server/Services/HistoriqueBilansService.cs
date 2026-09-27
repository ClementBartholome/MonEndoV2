using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Services;

/// <summary>Historique des bilans d'une période, avec les jours de règles : carnet déduit de la session.</summary>
public class HistoriqueBilansService(AppDbContext context)
{
    /// <summary>Un mois affiché avec ses semaines entamées tient en 6 semaines : au-delà, la demande est refusée.</summary>
    public const int DureeMaximaleJours = 42;

    public async Task<ResultatOperation<HistoriqueBilansViewModel>> GetPeriodeAsync(
        string userId, DateOnly du, DateOnly au, CancellationToken cancellationToken)
    {
        var carnetSanteId = await CarnetDeAsync(userId, cancellationToken);
        if (carnetSanteId is null) return ResultatOperation<HistoriqueBilansViewModel>.Echec(StatutOperation.NonAuthentifie);

        if (au < du || au.DayNumber - du.DayNumber + 1 > DureeMaximaleJours)
        {
            return ResultatOperation<HistoriqueBilansViewModel>.Echec(StatutOperation.Invalide,
                $"La période doit compter entre 1 et {DureeMaximaleJours} jours.");
        }

        // Dates enregistrées sans fuseau : on compare au jour calendaire, fin de période incluse.
        var debut = du.ToDateTime(TimeOnly.MinValue);
        var finExclue = au.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var bilans = await context.BilansQuotidiens
            .AsNoTracking()
            .Where(b => b.CarnetSanteId == carnetSanteId && b.Date >= debut && b.Date < finExclue)
            .OrderBy(b => b.Date)
            .ToListAsync(cancellationToken);

        var joursRegles = await context.JourRegles
            .Where(j => j.CarnetSanteId == carnetSanteId && j.Date >= debut && j.Date < finExclue)
            .Select(j => j.Date)
            .ToListAsync(cancellationToken);

        return ResultatOperation<HistoriqueBilansViewModel>.Succes(new HistoriqueBilansViewModel
        {
            Bilans = bilans.Select(BilanQuotidienViewModel.Depuis).ToList(),
            JoursRegles = joursRegles.Select(DateOnly.FromDateTime).Distinct().Order().ToList(),
        });
    }

    private async Task<int?> CarnetDeAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userId)) return null;

        return await context.CarnetSantes
            .Where(c => c.UserId == userId)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
