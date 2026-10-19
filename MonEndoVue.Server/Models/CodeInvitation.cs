using System.ComponentModel.DataAnnotations;

namespace MonEndoVue.Server.Models;

/// <summary>
/// Code d'invitation à l'inscription (ouverture progressive de MonEndo). Seule l'empreinte SHA-256 du code est stockée ; le code
/// lui-même n'est affiché qu'une fois, à sa création. Aucune donnée personnelle : le libellé sert à l'éditeur (« Bouche-à-oreille »).
/// </summary>
public class CodeInvitation
{
    public int Id { get; set; }

    [MaxLength(64)]
    public required string CodeHache { get; set; }

    [MaxLength(100)]
    public string? Libelle { get; set; }

    public int UsagesMax { get; set; }

    /// <summary>Comptes créés avec ce code (jeton de concurrence : deux inscriptions simultanées ne comptent pas pour une).</summary>
    public int Usages { get; set; }

    public DateTime CreeLe { get; set; }

    /// <summary>Null = sans date limite.</summary>
    public DateTime? ExpireLe { get; set; }
}
