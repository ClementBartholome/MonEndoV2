using MonEndoVue.Server.Services.Photos;

namespace MonEndoVue.Server.Tests.Support;

/// <summary>Stockage de photos en mémoire ; <see cref="Indisponible"/> simule une panne du stockage (Azure).</summary>
public sealed class FauxStockagePhotos : IStockagePhotos
{
    public Dictionary<string, byte[]> Contenus { get; } = [];

    public bool Indisponible { get; set; }

    public Task<Stream?> OuvrirAsync(string url, CancellationToken ct) =>
        Task.FromResult<Stream?>(Contenus.TryGetValue(url, out var octets) ? new MemoryStream(octets) : null);

    public Task SupprimerAsync(string url, CancellationToken ct)
    {
        if (Indisponible) throw new IOException("Stockage indisponible");
        Contenus.Remove(url);
        return Task.CompletedTask;
    }
}
