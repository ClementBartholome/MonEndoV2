using System.Text.Json.Serialization;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.ViewModels;

/// <summary>Bilans et jours de règles d'une période de l'historique (semaine ou mois).</summary>
public class HistoriqueBilansViewModel
{
    public List<BilanQuotidienViewModel> Bilans { get; set; } = [];
    /// <summary>Jours de règles de la période, sans doublon, triés (format yyyy-MM-dd).</summary>
    public List<DateOnly> JoursRegles { get; set; } = [];
}

/// <summary>Bilan quotidien renvoyé au client : mêmes champs que le type TS <c>BilanQuotidien</c>.</summary>
public class BilanQuotidienViewModel
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    public DateTime Date { get; set; }
    public string? Mood { get; set; }
    public List<EmotionBilanViewModel> Emotions { get; set; } = [];
    public int DouleurMoyenne { get; set; }
    public int? StressPro { get; set; }
    public int? StressPerso { get; set; }
    public int? Fatigue { get; set; }
    public int? Pas { get; set; }
    public double? Hydratation { get; set; }
    public bool Gluten { get; set; }
    public bool Lactose { get; set; }
    public bool Grignotage { get; set; }
    public string? Commentaire { get; set; }
    public bool? Selles { get; set; }
    public int? TypeBristol { get; set; }
    public bool? CrampesEstomac { get; set; }
    public string? IntensiteCrampes { get; set; }
    public bool? Ballonnements { get; set; }
    public string? IntensiteBallonnements { get; set; }
    public bool? DouleurSelle { get; set; }
    public string? IntensiteDouleurSelle { get; set; }
    public bool? Nausees { get; set; }
    public bool? SangSelles { get; set; }
    public bool? DouleurUriner { get; set; }
    public string? IntensiteDouleurUriner { get; set; }
    public bool? EnviesUrinaires { get; set; }
    public bool? DifficulteVider { get; set; }
    public bool? SangUrines { get; set; }
    public bool? SaignementsHorsRegles { get; set; }
    public string? AbondanceSaignementsHorsRegles { get; set; }
    public string? Nuit { get; set; }
    public bool? ReveilsDouleur { get; set; }
    public string? LimitationJournee { get; set; }
    public bool? AbsenceTravail { get; set; }
    public bool? ActiviteAnnulee { get; set; }
    public string? DouleurRapport { get; set; }

    public static BilanQuotidienViewModel Depuis(BilanQuotidien bilan) => new()
    {
        Id = bilan.Id,
        CarnetSanteId = bilan.CarnetSanteId,
        Date = bilan.Date,
        Mood = bilan.Mood,
        Emotions = bilan.Emotions.Select(e => new EmotionBilanViewModel { Emotion = e.Emotion }).ToList(),
        DouleurMoyenne = bilan.DouleurMoyenne,
        StressPro = bilan.StressPro,
        StressPerso = bilan.StressPerso,
        Fatigue = bilan.Fatigue,
        Pas = bilan.Pas,
        Hydratation = bilan.Hydratation,
        Gluten = bilan.Gluten,
        Lactose = bilan.Lactose,
        Grignotage = bilan.Grignotage,
        Commentaire = bilan.Commentaire,
        Selles = bilan.Selles,
        TypeBristol = bilan.TypeBristol,
        CrampesEstomac = bilan.CrampesEstomac,
        IntensiteCrampes = bilan.IntensiteCrampes,
        Ballonnements = bilan.Ballonnements,
        IntensiteBallonnements = bilan.IntensiteBallonnements,
        DouleurSelle = bilan.DouleurSelle,
        IntensiteDouleurSelle = bilan.IntensiteDouleurSelle,
        Nausees = bilan.Nausees,
        SangSelles = bilan.SangSelles,
        DouleurUriner = bilan.DouleurUriner,
        IntensiteDouleurUriner = bilan.IntensiteDouleurUriner,
        EnviesUrinaires = bilan.EnviesUrinaires,
        DifficulteVider = bilan.DifficulteVider,
        SangUrines = bilan.SangUrines,
        SaignementsHorsRegles = bilan.SaignementsHorsRegles,
        AbondanceSaignementsHorsRegles = bilan.AbondanceSaignementsHorsRegles,
        Nuit = bilan.Nuit,
        ReveilsDouleur = bilan.ReveilsDouleur,
        LimitationJournee = bilan.LimitationJournee,
        AbsenceTravail = bilan.AbsenceTravail,
        ActiviteAnnulee = bilan.ActiviteAnnulee,
        DouleurRapport = bilan.DouleurRapport,
    };
}

public class EmotionBilanViewModel
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Emotion Emotion { get; set; }
}
