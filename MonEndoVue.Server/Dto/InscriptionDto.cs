namespace MonEndoVue.Server.Dto;

/// <summary>Inscription : identifiants et consentement explicite au traitement des données de santé (case non cochée par défaut).</summary>
public class InscriptionDto : IdentifiantsDto
{
    public bool ConsentementDonneesSante { get; set; }

    /// <summary>Code d'invitation, exigé tant que les inscriptions sont sur invitation (<c>Inscriptions:SurInvitation</c>).</summary>
    public string? CodeInvitation { get; set; }
}
