namespace MonEndoVue.Server.Services.Comptes;

/// <summary>Ouverture des inscriptions (section « Inscriptions »). Par défaut, un code d'invitation est exigé.</summary>
public class InscriptionsOptions
{
    public const string Section = "Inscriptions";

    /// <summary>Vrai : une inscription exige un code d'invitation valable (ouverture par bouche-à-oreille contrôlé).</summary>
    public bool SurInvitation { get; set; } = true;
}
