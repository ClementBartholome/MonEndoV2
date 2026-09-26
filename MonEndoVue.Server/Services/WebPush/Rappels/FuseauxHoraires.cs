namespace MonEndoVue.Server.Services.WebPush.Rappels;

/// <summary>Conversions entre journée locale de l'utilisatrice et instants UTC stockés en base.</summary>
public static class FuseauxHoraires
{
    public static bool EstValide(string? fuseau) =>
        !string.IsNullOrWhiteSpace(fuseau) && TimeZoneInfo.TryFindSystemTimeZoneById(fuseau, out _);

    /// <summary>Bornes UTC [début, fin[ d'une journée locale dans le fuseau donné.</summary>
    public static (DateTime Debut, DateTime Fin) JourneeEnUtc(DateOnly jour, TimeZoneInfo fuseau)
    {
        var debutLocal = jour.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return (TimeZoneInfo.ConvertTimeToUtc(debutLocal, fuseau),
            TimeZoneInfo.ConvertTimeToUtc(debutLocal.AddDays(1), fuseau));
    }
}
