namespace MonEndoVue.Server.Models;

/// <summary>
/// Flux d'un jour de règles, tel que la personne le décrit. Stocké en texte (comme les autres énumérations du carnet) :
/// ajouter une valeur ne décale rien. « Traces » : quelques gouttes seulement.
/// </summary>
public enum FluxRegles
{
    Traces,
    Leger,
    Moyen,
    Abondant,
}
