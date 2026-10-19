using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services.Comptes;

/// <summary>
/// Codes d'invitation : création (commande en ligne sur le serveur, jamais par l'API), vérification et décompte à l'inscription.
/// Un code fait 12 caractères tirés au hasard (environ 60 bits), sans caractères ambigus ; la limite de débit de l'inscription
/// rend leur devinette impraticable.
/// </summary>
public class InvitationsService(AppDbContext context, TimeProvider horloge, ILogger<InvitationsService> logger)
{
    public const string MessageCodeInvalide = "Ce code d'invitation n'est pas valable (inconnu, expiré ou déjà utilisé). Demande un nouveau code à la personne qui t'a invitée.";

    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Majuscules, sans espaces ni tirets : « abcd-efgh ijkl » et « ABCDEFGHIJKL » sont le même code.</summary>
    public static string Normaliser(string code) =>
        new(code.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    public static string Hacher(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Normaliser(code))));

    /// <summary>Crée un code et le renvoie <b>une seule fois</b>, groupé par quatre (« ABCD-EFGH-JKLM ») ; seule son empreinte est gardée.</summary>
    public async Task<string> CreerAsync(int usagesMax, int? joursDeValidite, string? libelle, CancellationToken ct = default)
    {
        if (usagesMax < 1) throw new ArgumentOutOfRangeException(nameof(usagesMax), "Au moins un usage.");
        var caracteres = RandomNumberGenerator.GetItems<char>(Alphabet, 12);
        var code = $"{new string(caracteres, 0, 4)}-{new string(caracteres, 4, 4)}-{new string(caracteres, 8, 4)}";

        var maintenant = horloge.GetUtcNow().UtcDateTime;
        context.CodesInvitation.Add(new CodeInvitation
        {
            CodeHache = Hacher(code),
            Libelle = string.IsNullOrWhiteSpace(libelle) ? null : libelle.Trim(),
            UsagesMax = usagesMax,
            CreeLe = maintenant,
            ExpireLe = joursDeValidite is { } jours ? maintenant.AddDays(jours) : null,
        });
        await context.SaveChangesAsync(ct);
        return code;
    }

    /// <summary>Le code existe, n'a pas expiré et a encore des usages.</summary>
    public async Task<bool> EstValableAsync(string? code, CancellationToken ct = default) => await TrouverValableAsync(code, ct) is not null;

    /// <summary>
    /// Compte un usage, une fois le compte réellement créé. Deux inscriptions simultanées sur le dernier usage peuvent le dépasser
    /// d'une unité (accepté : ouvrir un compte de plus n'a pas de conséquence).
    /// </summary>
    public async Task ConsommerAsync(string code, CancellationToken ct = default)
    {
        for (var essai = 0; essai < 3; essai++)
        {
            var invitation = await context.CodesInvitation.SingleOrDefaultAsync(c => c.CodeHache == Hacher(code), ct);
            if (invitation is null) return;
            invitation.Usages++;
            try
            {
                await context.SaveChangesAsync(ct);
                logger.LogInformation("Code d'invitation {CodeId} utilisé ({Usages}/{UsagesMax})", invitation.Id, invitation.Usages, invitation.UsagesMax);
                return;
            }
            catch (DbUpdateConcurrencyException)
            {
                context.ChangeTracker.Clear();
            }
        }
    }

    private async Task<CodeInvitation?> TrouverValableAsync(string? code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var empreinte = Hacher(code);
        var invitation = await context.CodesInvitation.AsNoTracking().SingleOrDefaultAsync(c => c.CodeHache == empreinte, ct);
        var maintenant = horloge.GetUtcNow().UtcDateTime;
        return invitation is not null && invitation.Usages < invitation.UsagesMax && (invitation.ExpireLe is null || invitation.ExpireLe > maintenant)
            ? invitation
            : null;
    }
}
