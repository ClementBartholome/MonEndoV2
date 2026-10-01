using System.ComponentModel.DataAnnotations;

namespace MonEndoVue.Server.Dto;

/// <summary>Choix du calendrier lu (identifiant Google) ; vérifié contre la liste de l'utilisatrice avant d'être enregistré.</summary>
public class CalendrierChoisiDto
{
    [Required, MaxLength(1024)]
    public string Id { get; set; } = string.Empty;
}
