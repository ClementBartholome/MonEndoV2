using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services.Agenda;

namespace MonEndoVue.Server.Services.WebPush.Rappels;

/// <summary>
/// Rappel la veille d'un rendez-vous : envoyé seulement si le calendrier choisi (agenda Google lié) contient un rendez-vous à heure
/// fixe le lendemain (jour local). Omis sans agenda lié, sans calendrier choisi, sans rendez-vous, ou si Google ne répond pas.
/// La notification ne cite jamais le titre ni le lieu du rendez-vous : un écran verrouillé peut être lu par d'autres.
/// </summary>
public class RappelRendezVous(AppDbContext context, AgendaService agenda) : IRegleRappel
{
    public TypeRappel Type => TypeRappel.RendezVous;
    public bool EstHebdomadaire => false;
    public TimeOnly HeureParDefaut => new(19, 0);
    public DayOfWeek? JourParDefaut => null;
    public MessagePush Message { get; } =
        new("MonEndo", "Demain, un rendez-vous. Ton résumé peut t'aider à le préparer.", "/agenda");

    /// <summary>Sans agenda lié, ou sans calendrier choisi, aucun rendez-vous n'est lu : le réglage le dit au lieu de rester muet.</summary>
    public async Task<string?> EnAttenteAsync(int carnetSanteId, CancellationToken cancellationToken)
    {
        var userId = await context.CarnetSantes.Where(c => c.Id == carnetSanteId).Select(c => c.UserId).SingleOrDefaultAsync(cancellationToken);
        return await agenda.RaisonSansAgendaAsync(userId ?? string.Empty, cancellationToken) switch
        {
            RaisonSansAgenda.NonLie => "Ton agenda Google n'est pas lié : le rappel partira dès qu'il le sera.",
            RaisonSansAgenda.CalendrierNonChoisi => "Aucun calendrier choisi dans ton agenda Google : le rappel partira dès que tu en auras choisi un.",
            _ => null,
        };
    }

    /// <summary>« Rien à rappeler » (rappel omis) tant qu'aucun rendez-vous à heure fixe n'est prévu le lendemain.</summary>
    public async Task<bool> SuiviDejaFaitAsync(int carnetSanteId, DateOnly jourLocal, TimeZoneInfo fuseau, CancellationToken cancellationToken)
    {
        var userId = await context.CarnetSantes.Where(c => c.Id == carnetSanteId).Select(c => c.UserId).SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrEmpty(userId)) return true;

        var (debut, fin) = FuseauxHoraires.JourneeEnUtc(jourLocal.AddDays(1), fuseau);
        var evenements = await agenda.GetEvenementsAsync(
            userId, new DateTimeOffset(debut, TimeSpan.Zero), new DateTimeOffset(fin, TimeSpan.Zero), cancellationToken);

        // Agenda non lié, calendrier non choisi ou Google indisponible : pas de rappel plutôt qu'un rappel faux.
        return evenements.Statut != StatutOperation.Succes || !evenements.Valeur!.Any(e => !e.JourneeEntiere);
    }
}
