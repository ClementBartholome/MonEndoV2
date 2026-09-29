using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services.Traitements;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Prises prévues d'un jour selon la fréquence, et validation d'un traitement saisi.</summary>
public class PlanningTraitementTests
{
    // Lundi 28 septembre 2026.
    private static readonly DateOnly Lundi = new(2026, 9, 28);

    private static Medicament Traitement(FrequencePrise frequence, JoursSemaine jours = JoursSemaine.Aucun, int? intervalle = null,
        bool enCours = true, DateOnly? debut = null, DateOnly? fin = null, TypeTraitement type = TypeTraitement.Medicamenteux) => new()
    {
        Nom = "Traitement",
        Type = type,
        TraitementEnCours = enCours,
        Frequence = frequence,
        JoursSemaine = jours,
        IntervalleJours = intervalle,
        DateDebutTraitement = (debut ?? Lundi.AddDays(-10)).ToDateTime(TimeOnly.MinValue),
        DateFinTraitement = fin?.ToDateTime(TimeOnly.MinValue),
        Horaires = [new HorairePrise { Heure = new TimeOnly(21, 0) }, new HorairePrise { Heure = new TimeOnly(8, 0) }],
    };

    [Fact]
    public void HorairesDuJour_ChaqueJour_TousLesHorairesDansLOrdre()
    {
        Assert.Equal([new TimeOnly(8, 0), new TimeOnly(21, 0)], PlanningTraitement.HorairesDuJour(Traitement(FrequencePrise.ChaqueJour), Lundi));
    }

    [Theory]
    [InlineData(0, true)]   // lundi
    [InlineData(1, false)]  // mardi
    [InlineData(2, true)]   // mercredi
    public void EstPrevu_CertainsJours(int decalage, bool attendu)
    {
        var traitement = Traitement(FrequencePrise.CertainsJours, JoursSemaine.Lundi | JoursSemaine.Mercredi);

        Assert.Equal(attendu, PlanningTraitement.EstPrevu(traitement, Lundi.AddDays(decalage)));
    }

    [Theory]
    [InlineData(-10, true)]  // jour du début
    [InlineData(-9, false)]
    [InlineData(-7, true)]   // début + 3
    [InlineData(-1, true)]   // début + 9
    public void EstPrevu_TousLesNJours_CompteDepuisLeDebut(int decalage, bool attendu)
    {
        Assert.Equal(attendu, PlanningTraitement.EstPrevu(Traitement(FrequencePrise.TousLesNJours, intervalle: 3), Lundi.AddDays(decalage)));
    }

    [Fact]
    public void EstPrevu_AuBesoin_ArreteHorsDatesOuSoin_Jamais()
    {
        Assert.False(PlanningTraitement.EstPrevu(Traitement(FrequencePrise.AuBesoin), Lundi));
        Assert.False(PlanningTraitement.EstPrevu(Traitement(FrequencePrise.ChaqueJour, enCours: false), Lundi));
        Assert.False(PlanningTraitement.EstPrevu(Traitement(FrequencePrise.ChaqueJour, debut: Lundi.AddDays(1)), Lundi));
        Assert.False(PlanningTraitement.EstPrevu(Traitement(FrequencePrise.ChaqueJour, fin: Lundi.AddDays(-1)), Lundi));
        Assert.False(PlanningTraitement.EstPrevu(Traitement(FrequencePrise.ChaqueJour, type: TypeTraitement.NonMedicamenteux), Lundi));
        Assert.Empty(PlanningTraitement.HorairesDuJour(Traitement(FrequencePrise.AuBesoin), Lundi));
    }

    [Fact]
    public void EstPrevu_JourDeFin_Compris()
    {
        Assert.True(PlanningTraitement.EstPrevu(Traitement(FrequencePrise.ChaqueJour, fin: Lundi), Lundi));
    }

    private static TraitementDto Dto(FrequencePrise frequence, params string[] horaires) => new()
    {
        Nom = "Magnésium",
        Type = TypeTraitement.Medicamenteux,
        Frequence = frequence,
        Horaires = horaires.Select(TimeOnly.Parse).ToList(),
        DateDebut = Lundi,
    };

    [Fact]
    public void Valider_TraitementsCorrects_AucuneErreur()
    {
        Assert.Null(TraitementValidator.Valider(Dto(FrequencePrise.AuBesoin)));
        Assert.Null(TraitementValidator.Valider(Dto(FrequencePrise.ChaqueJour, "08:00", "20:00")));
        var certainsJours = Dto(FrequencePrise.CertainsJours, "21:00");
        certainsJours.JoursSemaine = ["Lundi", "Vendredi"];
        Assert.Null(TraitementValidator.Valider(certainsJours));
        var tousLesN = Dto(FrequencePrise.TousLesNJours, "08:00");
        tousLesN.IntervalleJours = 3;
        Assert.Null(TraitementValidator.Valider(tousLesN));
    }

    [Fact]
    public void Valider_RefuseLesSaisiesIncompletesOuIncoherentes()
    {
        var sansNom = Dto(FrequencePrise.AuBesoin);
        sansNom.Nom = " ";
        Assert.NotNull(TraitementValidator.Valider(sansNom));
        Assert.NotNull(TraitementValidator.Valider(Dto(FrequencePrise.ChaqueJour)));
        Assert.NotNull(TraitementValidator.Valider(Dto(FrequencePrise.ChaqueJour, "08:00", "08:00")));
        Assert.NotNull(TraitementValidator.Valider(Dto(FrequencePrise.ChaqueJour, "01:00", "02:00", "03:00", "04:00", "05:00", "06:00", "07:00")));
        Assert.NotNull(TraitementValidator.Valider(Dto(FrequencePrise.CertainsJours, "08:00")));
        var jourInconnu = Dto(FrequencePrise.CertainsJours, "08:00");
        jourInconnu.JoursSemaine = ["Funday"];
        Assert.NotNull(TraitementValidator.Valider(jourInconnu));
        var intervalle = Dto(FrequencePrise.TousLesNJours, "08:00");
        intervalle.IntervalleJours = 1;
        Assert.NotNull(TraitementValidator.Valider(intervalle));
        var finAvantDebut = Dto(FrequencePrise.AuBesoin);
        finAvantDebut.DateFin = Lundi.AddDays(-1);
        Assert.NotNull(TraitementValidator.Valider(finAvantDebut));
        var tropLong = Dto(FrequencePrise.AuBesoin);
        tropLong.Dose = new string('x', TraitementValidator.DoseMax + 1);
        Assert.NotNull(TraitementValidator.Valider(tropLong));
        var frequenceInconnue = Dto((FrequencePrise)42);
        Assert.NotNull(TraitementValidator.Valider(frequenceInconnue));
    }
}
