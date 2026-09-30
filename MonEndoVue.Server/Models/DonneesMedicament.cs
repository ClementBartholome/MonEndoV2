namespace MonEndoVue.Server.Models;

public class DonneesMedicament
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    public int MedicamentId { get; set; }
    public Medicament? Medicament { get; set; }
    public int NombreComprimes { get; set; }
    public DateTime Date { get; set; }
    public string? Commentaire { get; set; }

    /// <summary>Prise faite ou ignorée (les anciennes entrées sont des prises faites).</summary>
    public StatutPrise Statut { get; set; }

    /// <summary>Horaire prévu auquel cette entrée répond ; null pour une prise « au besoin » ou hors horaire.</summary>
    public TimeOnly? HeurePrevue { get; set; }
}