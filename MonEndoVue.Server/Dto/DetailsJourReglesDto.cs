using System.Text.Json.Serialization;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Dto;

/// <summary>
/// Détails facultatifs d'un jour de règles déjà noté (<c>PUT Cycle/regles/{jour}/details</c>). Chaque champ vaut « non renseigné »
/// quand il est nul : envoyer les deux champs remplace tout, jamais de valeur par défaut qui fausserait un comptage.
/// </summary>
public class DetailsJourReglesDto
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FluxRegles? Flux { get; set; }

    public bool? Caillots { get; set; }
}
