namespace MonEndoVue.Server.ViewModels;

/// <summary>
/// Synthèse du suivi sur une période, pour préparer un rendez-vous médical (<c>GET Synthese</c>) : des comptes et des
/// moyennes de ce qui a été noté, puis le détail jour par jour. Aucune interprétation. Dates au format « yyyy-MM-dd ».
/// </summary>
public class SyntheseRendezVousViewModel
{
    public required string Du { get; init; }
    public required string Au { get; init; }
    public required SyntheseReglesViewModel Regles { get; init; }
    public required SyntheseDouleursViewModel Douleurs { get; init; }
    public required SyntheseSymptomesViewModel Symptomes { get; init; }
    public required IReadOnlyList<SyntheseTraitementViewModel> Traitements { get; init; }
    public required SyntheseBilansViewModel Bilans { get; init; }
    public required SyntheseActiviteViewModel Activite { get; init; }
    /// <summary>Ancien suivi du transit par événements (avant les bilans quotidiens).</summary>
    public required IReadOnlyList<SyntheseEvenementViewModel> Transit { get; init; }
}

public class SyntheseReglesViewModel
{
    /// <summary>Jours de règles notés dans la période.</summary>
    public required IReadOnlyList<string> Jours { get; init; }
    /// <summary>Premiers jours des règles commencées dans la période.</summary>
    public required IReadOnlyList<string> Debuts { get; init; }
    /// <summary>Durée moyenne des cycles commencés dans la période ; null avant deux cycles terminés.</summary>
    public int? CycleMoyen { get; init; }
    public int? ReglesMoyenne { get; init; }
    /// <summary>Flux et caillots notés sur les jours de règles de la période (jours, jamais une moyenne).</summary>
    public required SyntheseFluxViewModel Flux { get; init; }
}

public class SyntheseFluxViewModel
{
    public int Traces { get; init; }
    public int Leger { get; init; }
    public int Moyen { get; init; }
    public int Abondant { get; init; }
    /// <summary>Jours de règles dont le flux n'est pas précisé.</summary>
    public int NonPrecise { get; init; }
    /// <summary>Jours de règles avec caillots, et jours où la personne a répondu « non » (les autres ne sont pas précisés).</summary>
    public int JoursAvecCaillots { get; init; }
    public int JoursSansCaillots { get; init; }
}

public class SyntheseDouleursViewModel
{
    /// <summary>Jours de la période avec au moins une douleur notée.</summary>
    public int Jours { get; init; }
    /// <summary>Jours avec une douleur à 6/10 ou plus, et ceux d'entre eux qui sont des jours de règles.</summary>
    public int JoursDouleurForte { get; init; }
    public int JoursDouleurFortePendantRegles { get; init; }
    public required IReadOnlyList<SyntheseTypeDouleurViewModel> ParType { get; init; }
    public required IReadOnlyList<SyntheseEntreeIntensiteViewModel> Entrees { get; init; }
}

public class SyntheseTypeDouleurViewModel
{
    public required string Type { get; init; }
    public int Jours { get; init; }
    public double IntensiteMoyenne { get; init; }
    public int IntensiteMax { get; init; }
    public int JoursPendantRegles { get; init; }
}

/// <summary>Une douleur ou un symptôme noté : jour, type, intensité (0 à 10).</summary>
public class SyntheseEntreeIntensiteViewModel
{
    public required string Jour { get; init; }
    public required string Type { get; init; }
    public int Intensite { get; init; }
}

public class SyntheseSymptomesViewModel
{
    public required IReadOnlyList<SyntheseTypeSymptomeViewModel> ParType { get; init; }
    public required IReadOnlyList<SyntheseEntreeIntensiteViewModel> Entrees { get; init; }
}

public class SyntheseTypeSymptomeViewModel
{
    public required string Type { get; init; }
    public int Jours { get; init; }
    public double IntensiteMoyenne { get; init; }
    public int JoursPendantRegles { get; init; }
}

public class SyntheseTraitementViewModel
{
    public required TraitementViewModel Traitement { get; init; }
    public bool EnCours { get; init; }
    /// <summary>Prises prévues dans la période, d'après la fréquence et les horaires actuels (0 pour « au besoin » et les soins).</summary>
    public int Prevues { get; init; }
    /// <summary>Prises faites (prévues ou au besoin), ou séances pour un soin.</summary>
    public int Faites { get; init; }
    public int Ignorees { get; init; }
    /// <summary>Jours de la période avec au moins une prise faite ou une séance.</summary>
    public required IReadOnlyList<string> Jours { get; init; }
}

public class SyntheseBilansViewModel
{
    public int Nombre { get; init; }
    public double? DouleurMoyenne { get; init; }
    public double? FatigueMoyenne { get; init; }
    public double? StressMoyen { get; init; }
    public int JoursBallonnements { get; init; }
    public int JoursCrampes { get; init; }
    /// <summary>Émotions les plus souvent choisies, de la plus fréquente à la moins fréquente (5 au plus).</summary>
    public required IReadOnlyList<SyntheseEmotionViewModel> Emotions { get; init; }
    public required IReadOnlyList<SyntheseBilanDuJourViewModel> Jours { get; init; }
    /// <summary>Catégories facultatives du bilan : des comptes de jours, toujours avec le nombre de jours où la personne a répondu.</summary>
    public required SyntheseCategoriesViewModel Categories { get; init; }
}

public class SyntheseCategoriesViewModel
{
    public required SyntheseDigestifViewModel Digestif { get; init; }
    public required SyntheseUrinaireViewModel Urinaire { get; init; }
    public required SyntheseSaignementsHorsReglesViewModel SaignementsHorsRegles { get; init; }
    public required SyntheseNuitEtJourneeViewModel NuitEtJournee { get; init; }
    /// <summary>Catégorie discrète : le client ne l'imprime que si la personne coche la rubrique.</summary>
    public required SyntheseRapportsViewModel Rapports { get; init; }
}

/// <summary>Suite du transit (douleur en allant à la selle, nausées, traces de sang) ; crampes et ballonnements restent au niveau du bilan.</summary>
public class SyntheseDigestifViewModel
{
    public int JoursNotes { get; init; }
    public int JoursDouleurSelle { get; init; }
    public int JoursNausees { get; init; }
    public int JoursSangSelles { get; init; }
}

public class SyntheseUrinaireViewModel
{
    public int JoursNotes { get; init; }
    public int JoursDouleurUriner { get; init; }
    public int JoursEnvies { get; init; }
    public int JoursDifficulteVider { get; init; }
    /// <summary>Jours avec du sang visible dans les urines, et pour chacun s'il tombe un jour de règles (un fait, sans interprétation).</summary>
    public required IReadOnlyList<SyntheseJourSangViewModel> JoursSangVisible { get; init; }
}

public class SyntheseJourSangViewModel
{
    public required string Jour { get; init; }
    public bool PendantRegles { get; init; }
}

public class SyntheseSaignementsHorsReglesViewModel
{
    public int JoursNotes { get; init; }
    public int Jours { get; init; }
    public int Traces { get; init; }
    public int Legers { get; init; }
    public int Abondants { get; init; }
}

public class SyntheseNuitEtJourneeViewModel
{
    public int NuitsNotees { get; init; }
    public int NuitsBonnes { get; init; }
    public int NuitsMoyennes { get; init; }
    public int NuitsDifficiles { get; init; }
    public int NuitsReveilsDouleur { get; init; }
    public int JourneesNotees { get; init; }
    public int JourneesPasLimitees { get; init; }
    public int JourneesPeuLimitees { get; init; }
    public int JourneesTresLimitees { get; init; }
    public int JoursAbsence { get; init; }
    public int JoursActiviteAnnulee { get; init; }
}

public class SyntheseRapportsViewModel
{
    public int JoursNotes { get; init; }
    public int AvecDouleur { get; init; }
    public int SansDouleur { get; init; }
    public int PasDeRapport { get; init; }
}

public class SyntheseEmotionViewModel
{
    public required string Emotion { get; init; }
    public int Jours { get; init; }
}

public class SyntheseBilanDuJourViewModel
{
    public required string Jour { get; init; }
    public int Douleur { get; init; }
    public int? Fatigue { get; init; }
    /// <summary>Moyenne des stress renseignés (vie pro, vie perso) ; null si aucun.</summary>
    public double? Stress { get; init; }
    public bool? Selles { get; init; }
    public int? TypeBristol { get; init; }
    public bool? Ballonnements { get; init; }
    public bool? Crampes { get; init; }
    public string? Notes { get; init; }
}

public class SyntheseActiviteViewModel
{
    public int Seances { get; init; }
    public int Minutes { get; init; }
    /// <summary>Effet noté sur la douleur : nombre de séances par réponse (les séances sans réponse ne sont pas comptées).</summary>
    public int Soulagee { get; init; }
    public int Pareille { get; init; }
    public int PlusForte { get; init; }
    public required IReadOnlyList<SyntheseTypeActiviteViewModel> ParType { get; init; }
    public required IReadOnlyList<SyntheseSeanceViewModel> Entrees { get; init; }
}

public class SyntheseTypeActiviteViewModel
{
    public required string Type { get; init; }
    public int Seances { get; init; }
    public int Minutes { get; init; }
}

public class SyntheseSeanceViewModel
{
    public required string Jour { get; init; }
    public required string Type { get; init; }
    /// <summary>1 douce, 2 modérée, 3 soutenue.</summary>
    public int Niveau { get; init; }
}

public class SyntheseEvenementViewModel
{
    public required string Jour { get; init; }
    public required string Type { get; init; }
}
