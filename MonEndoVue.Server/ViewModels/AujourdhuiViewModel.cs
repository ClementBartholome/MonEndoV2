using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.ViewModels;

/// <summary>Accueil « Aujourd'hui » : ce qui concerne le jour demandé, sans identifiant technique d'un autre carnet.</summary>
public class AujourdhuiViewModel
{
    public required CycleAujourdhuiViewModel Cycle { get; init; }
    /// <summary>Null tant que le bilan du jour n'est pas rempli.</summary>
    public BilanAujourdhuiViewModel? Bilan { get; init; }
    public required IReadOnlyList<TraitementAujourdhuiViewModel> Traitements { get; init; }
    public required SemaineViewModel Semaine { get; init; }
}

public class CycleAujourdhuiViewModel
{
    public bool EnRegles { get; init; }
    public int? JourDeRegles { get; init; }
    public int? JourDuCycle { get; init; }
}

public class BilanAujourdhuiViewModel
{
    public int DouleurMoyenne { get; init; }
    /// <summary>Codes de l'enum <see cref="Emotion"/> (libellés côté client, config/emotions.ts).</summary>
    public required IReadOnlyList<string> Emotions { get; init; }
    public int? Fatigue { get; init; }
}

/// <summary>Traitement médicamenteux en cours, avec les prises notées ce jour-là.</summary>
public class TraitementAujourdhuiViewModel
{
    public int Id { get; init; }
    public required string Nom { get; init; }
    public string? Posologie { get; init; }
    public int PrisesDuJour { get; init; }
    public DateTime? DernierePrise { get; init; }
}

/// <summary>Faits des 7 derniers jours (jour compris), descriptifs : aucune interprétation.</summary>
public class SemaineViewModel
{
    public int JoursAvecDouleur { get; init; }
    public int JoursAvecDouleurPendantRegles { get; init; }
    /// <summary>Moyennes de fatigue des bilans (null si aucun bilan ne la renseigne sur la période).</summary>
    public double? FatigueMoyenne { get; init; }
    public double? FatigueMoyennePrecedente { get; init; }
}
