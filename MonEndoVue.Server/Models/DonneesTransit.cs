using TypeGen.Core.TypeAnnotations;

namespace MonEndoVue.Server.Models;

[ExportTsInterface]
public class DonneesTransit
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    public DateTime Date { get; set; }

    // Selles du jour : oui/non
    public bool Selles { get; set; }

    // Type selon l'échelle de Bristol (1 à 7). Null si non renseigné.
    [TsOptional]
    public int? TypeBristol { get; set; }

    // Crampes d'estomac
    public bool CrampesEstomac { get; set; }

    // Légère, Modérée, Forte. Renseigné uniquement si CrampesEstomac == true.
    [TsOptional]
    public string? IntensiteCrampes { get; set; }

    // Ballonnements / ventre gonflé
    public bool Ballonnements { get; set; }

    // Légère, Modérée, Forte. Renseigné uniquement si Ballonnements == true.
    [TsOptional]
    public string? IntensiteBallonnements { get; set; }

    [TsOptional]
    public string? Commentaires { get; set; }
}
