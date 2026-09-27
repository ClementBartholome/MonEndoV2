namespace MonEndoVue.Server.ViewModels;

/// <summary>
/// Événement de l'agenda, limité à ce que l'interface affiche (pas de description ni de participants).
/// <see cref="Debut"/> et <see cref="Fin"/> gardent le format de Google : date et heure avec fuseau, ou date seule
/// (AAAA-MM-JJ) pour un événement sur la journée entière.
/// </summary>
public sealed record EvenementAgendaViewModel(
    string Id,
    string Titre,
    string Debut,
    string? Fin,
    bool JourneeEntiere,
    string? Lieu,
    string? Lien);
