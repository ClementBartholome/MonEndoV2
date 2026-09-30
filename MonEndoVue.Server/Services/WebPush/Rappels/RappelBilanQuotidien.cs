using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services.WebPush.Rappels;

/// <summary>Rappel quotidien du bilan, omis si le bilan de la journée locale est déjà rempli.</summary>
public class RappelBilanQuotidien(AppDbContext context) : IRegleRappel
{
    public TypeRappel Type => TypeRappel.BilanQuotidien;
    public bool EstHebdomadaire => false;
    public TimeOnly HeureParDefaut => new(21, 0);
    public DayOfWeek? JourParDefaut => null;
    public MessagePush Message { get; } = new("MonEndo", "N'oublie pas de remplir ton bilan quotidien.", "/bilan-quotidien?ajouter");

    public Task<bool> SuiviDejaFaitAsync(int carnetSanteId, DateOnly jourLocal, TimeZoneInfo fuseau, CancellationToken cancellationToken)
    {
        var (debut, fin) = FuseauxHoraires.JourneeEnUtc(jourLocal, fuseau);
        return context.BilansQuotidiens.AnyAsync(
            b => b.CarnetSanteId == carnetSanteId && b.Date >= debut && b.Date < fin,
            cancellationToken);
    }
}
