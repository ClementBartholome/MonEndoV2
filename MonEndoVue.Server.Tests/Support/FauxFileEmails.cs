using MonEndoVue.Server.Services.Email;

namespace MonEndoVue.Server.Tests.Support;

/// <summary>Remplace la file d'envoi : garde les messages planifiés pour les vérifier, sans réseau ni arrière-plan.</summary>
public sealed class FauxFileEmails : IFileEmails
{
    public List<MessageEmail> Planifies { get; } = [];

    public bool Disponible { get; set; } = true;

    public void Planifier(MessageEmail message) => Planifies.Add(message);

    /// <summary>Messages envoyés à une adresse, du plus ancien au plus récent.</summary>
    public List<MessageEmail> Pour(string adresse) => Planifies.Where(m => m.A == adresse).ToList();
}
