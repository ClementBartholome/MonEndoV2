using System.Globalization;
using Microsoft.Extensions.Options;
using MonEndoVue.Server.Services.Email;

namespace MonEndoVue.Server.Services.Comptes;

/// <summary>
/// Création d'un code d'invitation depuis le serveur (aucun point d'entrée HTTP) :
/// <c>dotnet MonEndoVue.Server.dll creer-code-invitation [usages=1] [jours=30] [libellé]</c>.
/// Le code et le lien d'inscription s'affichent une seule fois ; seule leur empreinte est enregistrée.
/// </summary>
public static class CommandeCodeInvitation
{
    public static async Task ExecuterAsync(IServiceProvider services, string[] args)
    {
        var usages = args.Length > 0 && int.TryParse(args[0], NumberStyles.None, CultureInfo.InvariantCulture, out var u) ? u : 1;
        int? jours = args.Length > 1 && int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out var j) ? (j == 0 ? null : j) : 30;
        var libelle = args.Length > 2 ? string.Join(' ', args[2..]) : null;

        using var scope = services.CreateScope();
        var code = await scope.ServiceProvider.GetRequiredService<InvitationsService>().CreerAsync(usages, jours, libelle);
        var adresse = scope.ServiceProvider.GetRequiredService<IOptions<EmailOptions>>().Value.AdresseApplication?.TrimEnd('/') ?? "https://monendoapp.fr";

        Console.WriteLine($"Code d'invitation : {code}");
        Console.WriteLine($"Usages : {usages}, validité : {(jours is null ? "sans limite" : $"{jours} jours")}{(libelle is null ? "" : $", libellé : {libelle}")}");
        Console.WriteLine($"Lien à partager : {adresse}/register#code={code}");
        Console.WriteLine("Ce code ne sera plus affiché : seule son empreinte est enregistrée.");
    }
}
