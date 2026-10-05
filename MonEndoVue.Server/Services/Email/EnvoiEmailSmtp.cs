using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MonEndoVue.Server.Services.Email;

/// <summary>
/// Envoi par un relais SMTP (STARTTLS). Sans configuration valide, <see cref="Disponible"/> est faux et rien n'est tenté.
/// Les journaux ne contiennent que le type d'erreur : ni adresse, ni objet, ni contenu, ni identifiants.
/// </summary>
public class EnvoiEmailSmtp(IOptions<EmailOptions> options, ILogger<EnvoiEmailSmtp> logger) : IEnvoiEmail
{
    private static readonly TimeSpan Delai = TimeSpan.FromSeconds(15);

    private readonly EmailOptions _options = options.Value;
    private readonly bool _configure = options.Value.Erreur() is null;

    public bool Disponible => _configure;

    public async Task<IssueEnvoiEmail> EnvoyerAsync(MessageEmail message, CancellationToken ct = default)
    {
        if (!_configure) return IssueEnvoiEmail.Desactive;

        try
        {
            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(_options.NomExpediteur, _options.Expediteur));
            mime.To.Add(MailboxAddress.Parse(message.A));
            mime.Subject = message.Sujet;
            mime.Body = new BodyBuilder { TextBody = message.Texte, HtmlBody = message.Html }.ToMessageBody();

            using var client = new SmtpClient { Timeout = (int)Delai.TotalMilliseconds };
            await client.ConnectAsync(_options.Hote, _options.Port, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_options.Utilisateur, _options.MotDePasse, ct);
            await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);
            return IssueEnvoiEmail.Envoye;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Jamais ex.Message : il peut citer l'adresse du destinataire ou une réponse du relais.
            logger.LogWarning("Envoi d'un e-mail impossible : {TypeErreur}", ex.GetType().Name);
            return IssueEnvoiEmail.Echec;
        }
    }
}
