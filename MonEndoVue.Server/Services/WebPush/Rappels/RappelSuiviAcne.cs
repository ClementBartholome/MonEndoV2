using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services.WebPush.Rappels;

/// <summary>
/// Rappel hebdomadaire du suivi photo de l'acné, omis si une photo d'acné a été ajoutée sur les 7 derniers jours locaux
/// (aujourd'hui compris). Ouvre directement la page Acné.
/// </summary>
public class RappelSuiviAcne(AppDbContext context) : IRegleRappel
{
    public const string TypeSymptomeAcne = "Acné";
    private const int JoursSansPhoto = 7;

    public TypeRappel Type => TypeRappel.SuiviAcne;
    public bool EstHebdomadaire => true;
    public TimeOnly HeureParDefaut => new(20, 0);
    public DayOfWeek? JourParDefaut => DayOfWeek.Sunday;
    public MessagePush Message { get; } =
        new("MonEndo", "C'est le moment de la photo de suivi de ton acné.", "/acne");

    /// <summary>Sans épisode en cours (ils se ferment aussi d'eux-mêmes à la reprise des anciens jours d'acné), rien ne part.</summary>
    public async Task<string?> EnAttenteAsync(int carnetSanteId, CancellationToken cancellationToken) =>
        await context.EpisodesAcne.AnyAsync(e => e.CarnetSanteId == carnetSanteId && e.Fin == null, cancellationToken)
            ? null
            : "Aucun épisode d'acné en cours : le rappel partira dès que tu en noteras un.";

    public async Task<bool> SuiviDejaFaitAsync(int carnetSanteId, DateOnly jourLocal, TimeZoneInfo fuseau, CancellationToken cancellationToken)
    {
        // Pas d'épisode en cours : rien à suivre, donc aucun rappel (la photo de la semaine accompagne un épisode).
        if (!await context.EpisodesAcne.AnyAsync(e => e.CarnetSanteId == carnetSanteId && e.Fin == null, cancellationToken)) return true;

        var debut = FuseauxHoraires.JourneeEnUtc(jourLocal.AddDays(1 - JoursSansPhoto), fuseau).Debut;
        var fin = FuseauxHoraires.JourneeEnUtc(jourLocal, fuseau).Fin;
        return await context.SymptomesCycles.AnyAsync(
            s => s.CarnetSanteId == carnetSanteId
                 && s.TypeSymptome == TypeSymptomeAcne
                 && s.PhotoUrl != null && s.PhotoUrl != ""
                 && s.Date >= debut && s.Date < fin,
            cancellationToken);
    }
}
