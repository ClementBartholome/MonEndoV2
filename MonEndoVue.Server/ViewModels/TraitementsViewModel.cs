namespace MonEndoVue.Server.ViewModels;

/// <summary>Page Traitements pour un jour : prises prévues, au besoin, soins, et la liste des traitements.</summary>
public class TraitementsDuJourViewModel
{
    public required IReadOnlyList<PrisePrevueViewModel> PrisesPrevues { get; init; }
    public required IReadOnlyList<TraitementAuBesoinViewModel> AuBesoin { get; init; }
    public required IReadOnlyList<SoinViewModel> Soins { get; init; }
    public required IReadOnlyList<TraitementViewModel> EnCours { get; init; }
    public required IReadOnlyList<TraitementViewModel> Termines { get; init; }
}

/// <summary>Une prise prévue ce jour-là, avec la réponse notée s'il y en a une.</summary>
public class PrisePrevueViewModel
{
    public int TraitementId { get; init; }
    public required string Nom { get; init; }
    public string? Dose { get; init; }
    /// <summary>« HH:mm ».</summary>
    public required string HeurePrevue { get; init; }
    public ReponsePriseViewModel? Reponse { get; init; }
}

public class ReponsePriseViewModel
{
    public int PriseId { get; init; }
    /// <summary>« Pris » ou « Ignore ».</summary>
    public required string Statut { get; init; }
    /// <summary>Date locale sans fuseau.</summary>
    public DateTime Date { get; init; }
}

public class TraitementAuBesoinViewModel
{
    public int Id { get; init; }
    public required string Nom { get; init; }
    public string? Dose { get; init; }
    public DateTime? DernierePrise { get; init; }
}

public class SoinViewModel
{
    public int Id { get; init; }
    public required string Nom { get; init; }
    public DateTime? DerniereSeance { get; init; }
}

/// <summary>Traitement complet (liste et formulaire de modification).</summary>
public class TraitementViewModel
{
    public int Id { get; init; }
    public required string Nom { get; init; }
    /// <summary>« Medicamenteux » ou « NonMedicamenteux ».</summary>
    public required string Type { get; init; }
    public string? Dose { get; init; }
    /// <summary>« AuBesoin », « ChaqueJour », « CertainsJours », « TousLesNJours ».</summary>
    public required string Frequence { get; init; }
    /// <summary>Noms des jours (« Lundi »…).</summary>
    public required IReadOnlyList<string> JoursSemaine { get; init; }
    public int? IntervalleJours { get; init; }
    /// <summary>« HH:mm », dans l'ordre.</summary>
    public required IReadOnlyList<string> Horaires { get; init; }
    public DateOnly DateDebut { get; init; }
    public DateOnly? DateFin { get; init; }
}
