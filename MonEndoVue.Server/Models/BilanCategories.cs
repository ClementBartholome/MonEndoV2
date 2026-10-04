namespace MonEndoVue.Server.Models;

/// <summary>Copie des réponses des catégories facultatives d'un bilan vers un autre (modification d'un bilan existant).</summary>
public static class BilanCategories
{
    public static void Copier(BilanQuotidien source, BilanQuotidien cible)
    {
        cible.DouleurSelle = source.DouleurSelle;
        cible.IntensiteDouleurSelle = source.IntensiteDouleurSelle;
        cible.Nausees = source.Nausees;
        cible.SangSelles = source.SangSelles;
        cible.DouleurUriner = source.DouleurUriner;
        cible.IntensiteDouleurUriner = source.IntensiteDouleurUriner;
        cible.EnviesUrinaires = source.EnviesUrinaires;
        cible.DifficulteVider = source.DifficulteVider;
        cible.SangUrines = source.SangUrines;
        cible.SaignementsHorsRegles = source.SaignementsHorsRegles;
        cible.AbondanceSaignementsHorsRegles = source.AbondanceSaignementsHorsRegles;
        cible.Nuit = source.Nuit;
        cible.ReveilsDouleur = source.ReveilsDouleur;
        cible.LimitationJournee = source.LimitationJournee;
        cible.AbsenceTravail = source.AbsenceTravail;
        cible.ActiviteAnnulee = source.ActiviteAnnulee;
        cible.DouleurRapport = source.DouleurRapport;
    }
}
