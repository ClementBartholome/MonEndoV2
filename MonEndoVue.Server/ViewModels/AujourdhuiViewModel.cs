using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.ViewModels;

/// <summary>Accueil « Aujourd'hui » : ce qui concerne le jour demandé, sans identifiant technique d'un autre carnet.</summary>
public class AujourdhuiViewModel
{
    public required CycleAujourdhuiViewModel Cycle { get; init; }
    /// <summary>Null tant que le bilan du jour n'est pas rempli.</summary>
    public BilanAujourdhuiViewModel? Bilan { get; init; }
    /// <summary>Prises prévues aujourd'hui, dans l'ordre des horaires (voir <see cref="TraitementsDuJourViewModel"/>).</summary>
    public required IReadOnlyList<PrisePrevueViewModel> PrisesPrevues { get; init; }
    /// <summary>Traitements « au besoin » en cours.</summary>
    public required IReadOnlyList<TraitementAuBesoinViewModel> AuBesoin { get; init; }
    public required SemaineViewModel Semaine { get; init; }
    /// <summary>Épisode d'acné en cours (null sans épisode en cours) : l'accueil n'en parle que pendant un épisode.</summary>
    public AcneAujourdhuiViewModel? Acne { get; init; }
}

public class AcneAujourdhuiViewModel
{
    /// <summary>Début de l'épisode en cours (AAAA-MM-JJ).</summary>
    public required string Debut { get; init; }
    /// <summary>Durée de l'épisode jusqu'au jour demandé, jour de début compris.</summary>
    public int Jours { get; init; }
    /// <summary>Jour de la dernière photo de suivi (AAAA-MM-JJ), toutes périodes confondues ; null sans photo.</summary>
    public string? DernierePhoto { get; init; }
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

/// <summary>Faits des 7 derniers jours (jour compris), descriptifs : aucune interprétation.</summary>
public class SemaineViewModel
{
    public int JoursAvecDouleur { get; init; }
    public int JoursAvecDouleurPendantRegles { get; init; }
    /// <summary>Moyennes de fatigue des bilans (null si aucun bilan ne la renseigne sur la période).</summary>
    public double? FatigueMoyenne { get; init; }
    public double? FatigueMoyennePrecedente { get; init; }
}
