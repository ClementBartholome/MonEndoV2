namespace MonEndoVue.Server.Services;

/// <summary>
/// Date d'une prise ou d'une séance : ni avant 2000 (année 0001 envoyée à la main), ni après demain. Le jour est celui de
/// l'utilisatrice (heure locale envoyée par le client) alors que le serveur raisonne en UTC : le lendemain UTC est toléré
/// (même règle que <see cref="BilanDateValidator"/>).
/// </summary>
public static class DateSaisieValidator
{
    public const string Message = "La date n'est pas valide.";

    private static readonly DateTime PremierJourAccepte = new(2000, 1, 1);

    public static bool EstValide(DateTime date, DateTimeOffset maintenant) =>
        date.Date >= PremierJourAccepte && date.Date <= maintenant.UtcDateTime.Date.AddDays(1);
}
