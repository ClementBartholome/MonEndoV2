using System.Threading.Channels;

namespace MonEndoVue.Server.Services.Email;

/// <summary>
/// Messages de compte à envoyer en arrière-plan. La requête ne les attend pas : une adresse inscrite et une adresse inconnue
/// répondent dans le même délai (le temps d'un envoi SMTP trahirait l'existence d'un compte). Une abstraction pour la remplacer
/// en test (<c>FauxFileEmails</c>).
/// </summary>
public interface IFileEmails
{
    /// <summary>Faux si aucun relais n'est configuré : l'opération qui dépend d'un e-mail est alors refusée plutôt que perdue.</summary>
    bool Disponible { get; }

    void Planifier(MessageEmail message);
}

/// <summary>File en mémoire, vidée par un service d'arrière-plan qui envoie par <see cref="IEnvoiEmail"/>.</summary>
public sealed class FileEmails(IEnvoiEmail envoi, ILogger<FileEmails> logger) : BackgroundService, IFileEmails
{
    /// <summary>Au-delà, un message est abandonné (et journalisé sans détail) : une rafale ne doit pas saturer la mémoire.</summary>
    private const int Capacite = 200;

    private readonly Channel<MessageEmail> _file = Channel.CreateBounded<MessageEmail>(
        new BoundedChannelOptions(Capacite) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

    public bool Disponible => envoi.Disponible;

    public void Planifier(MessageEmail message)
    {
        if (!_file.Writer.TryWrite(message)) logger.LogWarning("File des e-mails pleine : un message n'a pas été envoyé");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _file.Reader.ReadAllAsync(stoppingToken))
        {
            // EnvoiEmailSmtp ne lève pas (il journalise le type d'erreur) ; la boucle continue quoi qu'il arrive.
            await envoi.EnvoyerAsync(message, stoppingToken);
        }
    }
}
