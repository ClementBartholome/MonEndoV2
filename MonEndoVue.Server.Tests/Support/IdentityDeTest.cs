using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;

namespace MonEndoVue.Server.Tests.Support;

/// <summary>
/// Pile Identity réelle (UserManager, SignInManager) sur une base EF en mémoire isolée,
/// pour tester AccountController sans simuler les services d'authentification.
/// </summary>
public sealed class IdentityDeTest : IDisposable
{
    public const string MotDePasseValide = "MotDePasse1!";

    /// <summary>Heure fixe injectée dans le contrôleur (date du consentement).</summary>
    public static readonly DateTimeOffset Maintenant = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    public AppDbContext Context { get; }
    public UserManager<ApplicationUser> UserManager { get; }

    public IdentityDeTest()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "cle-de-test-uniquement-pour-les-tests-unitaires",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase($"monendo-identity-{Guid.NewGuid()}"));
        services.AddAuthentication();
        services.AddIdentityCore<ApplicationUser>(OptionsIdentite.Appliquer)
            .AddSignInManager()
            .AddEntityFrameworkStores<AppDbContext>();
        services.AddMemoryCache();

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        Context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        UserManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    }

    public AccountController CreerController(string? userId = null)
    {
        var sp = _scope.ServiceProvider;
        var carnetSanteService = new CarnetSanteService(
            Context, NullLogger<CarnetSanteService>.Instance, sp.GetRequiredService<IMemoryCache>());

        var httpContext = new DefaultHttpContext { RequestServices = sp };
        if (userId != null)
        {
            httpContext.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId)], "Test"));
        }

        return new AccountController(
            UserManager,
            sp.GetRequiredService<SignInManager<ApplicationUser>>(),
            carnetSanteService,
            new TokenService(sp.GetRequiredService<IConfiguration>()),
            new HorlogeFixe(Maintenant),
            NullLogger<AccountController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
    }

    /// <summary>Crée une utilisatrice ; avec son carnet de santé sauf si <paramref name="avecCarnet"/> vaut false.</summary>
    public async Task<ApplicationUser> CreerUtilisatrice(string email, bool avecCarnet = true)
    {
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var resultat = await UserManager.CreateAsync(user, MotDePasseValide);
        Assert.True(resultat.Succeeded);

        if (avecCarnet)
        {
            Context.CarnetSantes.Add(new CarnetSante { UserId = user.Id });
            await Context.SaveChangesAsync();
        }

        return user;
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }
}
