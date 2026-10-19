using MonEndoVue.Server.Services;

namespace MonEndoVue.Server.Tests.Services;

public sealed class DateSaisieValidatorTests
{
    private static readonly DateTimeOffset Maintenant = new(2026, 9, 28, 23, 30, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("2026-09-28")]
    [InlineData("2026-09-29")] // lendemain UTC toléré : la personne est peut-être déjà au jour suivant
    [InlineData("2026-01-01")]
    [InlineData("2000-01-01")]
    public void EstValide_DateRaisonnable_Accepte(string date) =>
        Assert.True(DateSaisieValidator.EstValide(DateTime.Parse(date), Maintenant));

    [Theory]
    [InlineData("0001-01-01")]
    [InlineData("1999-12-31")]
    [InlineData("2026-09-30")]
    [InlineData("9999-12-31")]
    public void EstValide_DateAbsurdeOuLointaine_Refuse(string date) =>
        Assert.False(DateSaisieValidator.EstValide(DateTime.Parse(date), Maintenant));
}
