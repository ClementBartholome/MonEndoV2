using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Services.Frise;

/// <summary>
/// La frise : un trait par jour, teinté par la douleur la plus forte notée ce jour-là (douleur moyenne du bilan ou entrée de
/// douleur), et les jours de règles. Purement descriptive : aucune moyenne, aucune prédiction. Les dates sont enregistrées
/// au jour calendaire local, sans fuseau : le jour de fin vient du client (comme l'accueil).
/// </summary>
public class FriseService(AppDbContext context, TimeProvider horloge)
{
    /// <summary>Plus longue frise demandée : deux mois (page Cycle : 30 jours).</summary>
    public const int JoursMaximum = 62;

    /// <summary>Écart toléré entre le jour demandé et la date du serveur (fuseaux horaires).</summary>
    private const int EcartMaximalJours = 2;

    /// <summary>Frise de l'utilisatrice connectée : les <paramref name="jours"/> jours qui finissent à <paramref name="au"/> (compris).</summary>
    public async Task<ResultatOperation<IReadOnlyList<JourFriseViewModel>>> GetAsync(string userId, DateOnly au, int jours, CancellationToken ct)
    {
        var carnetId = await context.CarnetSantes.Where(c => c.UserId == userId).Select(c => (int?)c.Id).SingleOrDefaultAsync(ct);
        if (carnetId is not { } id) return ResultatOperation<IReadOnlyList<JourFriseViewModel>>.Echec(StatutOperation.NonAuthentifie);

        if (jours < 1 || jours > JoursMaximum)
            return ResultatOperation<IReadOnlyList<JourFriseViewModel>>.Echec(StatutOperation.Invalide, $"La frise couvre de 1 à {JoursMaximum} jours.");
        var aujourdhuiServeur = DateOnly.FromDateTime(horloge.GetUtcNow().UtcDateTime);
        if (Math.Abs(au.DayNumber - aujourdhuiServeur.DayNumber) > EcartMaximalJours)
            return ResultatOperation<IReadOnlyList<JourFriseViewModel>>.Echec(StatutOperation.Invalide, "La frise se termine aujourd'hui.");

        return ResultatOperation<IReadOnlyList<JourFriseViewModel>>.Succes(await JoursAsync(id, au.AddDays(1 - jours), au, ct));
    }

    /// <summary>Un élément par jour, du plus ancien au plus récent, de <paramref name="du"/> à <paramref name="au"/> compris.</summary>
    public async Task<IReadOnlyList<JourFriseViewModel>> JoursAsync(int carnetId, DateOnly du, DateOnly au, CancellationToken ct)
    {
        var debut = du.ToDateTime(TimeOnly.MinValue);
        var finExclue = au.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var douleursBilans = await context.BilansQuotidiens.AsNoTracking()
            .Where(b => b.CarnetSanteId == carnetId && b.Date >= debut && b.Date < finExclue)
            .Select(b => new { b.Date, Douleur = b.DouleurMoyenne })
            .ToListAsync(ct);
        var douleursNotees = await context.DonneesDouleurs.AsNoTracking()
            .Where(d => d.CarnetSanteId == carnetId && d.Date >= debut && d.Date < finExclue)
            .Select(d => new { d.Date, Douleur = d.Intensite })
            .ToListAsync(ct);
        var regles = (await context.JourRegles.AsNoTracking()
                .Where(j => j.CarnetSanteId == carnetId && j.Date >= debut && j.Date < finExclue)
                .Select(j => j.Date)
                .ToListAsync(ct))
            .Select(DateOnly.FromDateTime)
            .ToHashSet();

        var douleurParJour = douleursBilans.Concat(douleursNotees)
            .GroupBy(d => DateOnly.FromDateTime(d.Date))
            .ToDictionary(g => g.Key, g => g.Max(d => d.Douleur));

        return Enumerable.Range(0, au.DayNumber - du.DayNumber + 1)
            .Select(du.AddDays)
            .Select(jour => new JourFriseViewModel
            {
                Jour = jour.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Douleur = douleurParJour.TryGetValue(jour, out var douleur) ? douleur : null,
                Regles = regles.Contains(jour),
            })
            .ToList();
    }
}
