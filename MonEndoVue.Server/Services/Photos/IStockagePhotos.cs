namespace MonEndoVue.Server.Services.Photos;

/// <summary>
/// Accès aux photos de suivi stockées hors du serveur (Azure Blob Storage), remplaçable dans les tests.
/// Les adresses viennent toujours de la base, jamais du client.
/// </summary>
public interface IStockagePhotos
{
    /// <summary>Contenu de la photo, ou null si elle n'existe plus.</summary>
    Task<Stream?> OuvrirAsync(string url, CancellationToken ct);
}
