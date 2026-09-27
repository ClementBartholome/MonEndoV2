namespace MonEndoVue.Server.Services;

/// <summary>
/// Un bilan ne peut pas porter sur un jour futur. Le jour est celui de l'utilisatrice (heure locale envoyée par le
/// client) alors que le serveur raisonne en UTC : on tolère donc le lendemain UTC, pour qu'un bilan saisi juste après
/// minuit (UTC+1 ou +2) ne soit pas refusé.
/// </summary>
public static class BilanDateValidator
{
    public static (bool EstValide, string? Erreur) Valider(DateTime date, DateTimeOffset maintenant)
    {
        var dernierJourAccepte = maintenant.UtcDateTime.Date.AddDays(1);
        return date.Date > dernierJourAccepte
            ? (false, "Le bilan ne peut pas porter sur un jour à venir.")
            : (true, null);
    }
}
