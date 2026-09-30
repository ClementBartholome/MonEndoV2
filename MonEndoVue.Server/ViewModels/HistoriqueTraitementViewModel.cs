namespace MonEndoVue.Server.ViewModels;

/// <summary>Historique d'un traitement sur un mois (<c>GET Traitements/{id}/historique</c>).</summary>
public class HistoriqueTraitementViewModel
{
    public required TraitementViewModel Traitement { get; init; }

    /// <summary>Prises prévues du mois jusqu'au jour demandé (traitement à heures fixes ; 0 sinon).</summary>
    public int Prevues { get; init; }

    /// <summary>Prises faites (prévues ou au besoin) ou séances, selon le type de traitement.</summary>
    public int Faites { get; init; }

    public int Ignorees { get; init; }

    /// <summary>Même compte le mois précédent, pour comparer sans jugement.</summary>
    public int FaitesMoisPrecedent { get; init; }

    /// <summary>Jours du mois avec au moins une prise ou une séance, du plus récent au plus ancien.</summary>
    public required IReadOnlyList<JourHistoriqueTraitementViewModel> Jours { get; init; }
}

public class JourHistoriqueTraitementViewModel
{
    /// <summary>« yyyy-MM-dd »</summary>
    public required string Jour { get; init; }

    public required IReadOnlyList<EntreeHistoriqueTraitementViewModel> Entrees { get; init; }
}

public class EntreeHistoriqueTraitementViewModel
{
    /// <summary>Identifiant de la prise ou de la séance (pour la retirer).</summary>
    public int Id { get; init; }

    /// <summary>« Pris », « Ignore » ou « Seance ».</summary>
    public required string Nature { get; init; }

    /// <summary>Date et heure locales, sans fuseau.</summary>
    public DateTime Date { get; init; }

    /// <summary>« HH:mm » de la prise prévue ; null pour une prise au besoin ou une séance.</summary>
    public string? HeurePrevue { get; init; }
}
