using Microsoft.Extensions.Logging.Abstractions;
using MonEndoVue.Server.Services.SuppressionCompte;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Durée de conservation : suppression des comptes sans activité depuis 2 ans.</summary>
public sealed class ComptesInactifsServiceTests : IDisposable
{
    private static readonly DateTimeOffset Maintenant = new(2028, 10, 1, 3, 30, 0, TimeSpan.Zero);

    private readonly IdentityDeTest _identity = new();
    private readonly FauxStockagePhotos _photos = new();

    [Fact]
    public async Task SupprimerAsync_SupprimeSeulementLesComptesInactifsDepuisPlusDeDeuxAns()
    {
        var inactive = await CreerAvecActivite("inactive@local", Maintenant.UtcDateTime - ComptesInactifsService.DureeInactivite - TimeSpan.FromDays(1));
        var limite = await CreerAvecActivite("limite@local", Maintenant.UtcDateTime - ComptesInactifsService.DureeInactivite + TimeSpan.FromDays(1));
        var sansDate = await CreerAvecActivite("sans-date@local", null);

        var supprimes = await Service().SupprimerAsync(CancellationToken.None);

        Assert.Equal(1, supprimes);
        Assert.Null(await _identity.UserManager.FindByIdAsync(inactive));
        Assert.NotNull(await _identity.UserManager.FindByIdAsync(limite));
        Assert.NotNull(await _identity.UserManager.FindByIdAsync(sansDate));
        Assert.False(_identity.Context.CarnetSantes.Any(c => c.UserId == inactive));
    }

    [Fact]
    public async Task Login_MetAJourLaDerniereActivite()
    {
        await _identity.CreerUtilisatrice("active@local");

        await _identity.CreerController().Login(new MonEndoVue.Server.Dto.IdentifiantsDto
        {
            Email = "active@local",
            Password = IdentityDeTest.MotDePasseValide,
        });

        var user = await _identity.UserManager.FindByEmailAsync("active@local");
        Assert.Equal(IdentityDeTest.Maintenant.UtcDateTime, user!.DerniereActiviteLe);
    }

    private ComptesInactifsService Service() => new(
        _identity.Context,
        new SuppressionCompteService(_identity.Context, _identity.UserManager, _photos, NullLogger<SuppressionCompteService>.Instance),
        new HorlogeFixe(Maintenant),
        NullLogger<ComptesInactifsService>.Instance);

    private async Task<string> CreerAvecActivite(string email, DateTime? derniereActivite)
    {
        var user = await _identity.CreerUtilisatrice(email);
        user.DerniereActiviteLe = derniereActivite;
        await _identity.UserManager.UpdateAsync(user);
        return user.Id;
    }

    public void Dispose() => _identity.Dispose();
}
