using MonEndoVue.Server.Services.Cycle;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Regroupement des jours de règles en règles puis en cycles, sans prédiction.</summary>
public sealed class HistoriqueCyclesTests
{
    private static DateOnly J(int mois, int jour) => new(2026, mois, jour);

    private static IEnumerable<DateOnly> Du(int mois, int premier, int nombre) =>
        Enumerable.Range(premier, nombre).Select(j => J(mois, j));

    [Fact]
    public void Regrouper_JoursConsecutifs_ToleranceDUnJourOublie()
    {
        var regles = HistoriqueCycles.Regrouper([J(8, 14), J(8, 15), J(8, 17), J(8, 18), J(9, 14), J(9, 14)]);

        Assert.Equal([new Regles(J(8, 14), J(8, 18)), new Regles(J(9, 14), J(9, 14))], regles);
        Assert.Equal(5, regles[0].NombreDeJours);
    }

    [Fact]
    public void Regrouper_DeuxJoursOublies_CoupeLesRegles()
    {
        var regles = HistoriqueCycles.Regrouper([J(8, 14), J(8, 17)]);

        Assert.Equal(2, regles.Count);
    }

    [Fact]
    public void Cycles_DuPlusRecent_AvecDureeEtJoursDeRegles()
    {
        var jours = Du(6, 16, 5).Concat(Du(7, 14, 4)).Concat(Du(8, 14, 5)).Concat(Du(9, 14, 2));

        var cycles = HistoriqueCycles.Cycles(HistoriqueCycles.Regrouper(jours), 6);

        // Les règles en cours (14 septembre) ne ferment aucun cycle : trois cycles terminés.
        Assert.Equal(
            [new CycleTermine(J(8, 14), 5, 31), new CycleTermine(J(7, 14), 4, 31), new CycleTermine(J(6, 16), 5, 28)],
            cycles);
        Assert.Equal(30, HistoriqueCycles.DureeMoyenne(cycles));
    }

    [Fact]
    public void Cycles_EcartTropLong_NiListeNiCompte()
    {
        var jours = Du(1, 1, 3).Concat(Du(5, 1, 3)).Concat(Du(5, 29, 3));

        var cycles = HistoriqueCycles.Cycles(HistoriqueCycles.Regrouper(jours), 6);

        Assert.Equal([new CycleTermine(J(5, 1), 3, 28)], cycles);
    }

    [Fact]
    public void Cycles_LimiteAuNombreDemande()
    {
        var jours = Enumerable.Range(0, 10).Select(i => J(1, 1).AddDays(i * 28));

        Assert.Equal(6, HistoriqueCycles.Cycles(HistoriqueCycles.Regrouper(jours), 6).Count);
    }

    [Fact]
    public void DureeMoyenne_AucuneAvantDeuxCycles()
    {
        Assert.Null(HistoriqueCycles.DureeMoyenne([new CycleTermine(J(8, 14), 5, 31)]));
        Assert.Null(HistoriqueCycles.DureeMoyenne([]));
    }
}
