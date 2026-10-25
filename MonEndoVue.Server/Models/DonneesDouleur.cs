namespace MonEndoVue.Server.Models;

public class DonneesDouleur
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    public int Intensite { get; set; }
    public string TypeDouleur { get; set; }
    public DateTime Date { get; set; }
    public string? Commentaire { get; set; }

    /// <summary>
    /// Zone notée dans le bilan du jour (« Où as-tu eu mal ? ») plutôt qu'une crise notée sur la page Douleurs : remplacée à chaque
    /// enregistrement des zones du bilan et supprimée avec lui. Une crise n'est jamais touchée par le bilan.
    /// </summary>
    public bool DepuisBilan { get; set; }
}