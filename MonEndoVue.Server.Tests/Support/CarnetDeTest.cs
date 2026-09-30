using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;

namespace MonEndoVue.Server.Tests.Support;

/// <summary>
/// Base EF en mémoire isolée, contenant l'utilisatrice connectée et son carnet de santé,
/// ainsi qu'une autre utilisatrice et son carnet (pour les tests de cloisonnement).
/// </summary>
public sealed class CarnetDeTest : IDisposable
{
    public const string UserId = "utilisatrice-test";
    public const int CarnetSanteId = 1;
    public const string AutreUserId = "autre-utilisatrice";
    public const string AutreUserName = "autre@local";
    public const int AutreCarnetSanteId = 2;

    public AppDbContext Context { get; }
    public CarnetSanteService CarnetSanteService { get; }

    public CarnetDeTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"monendo-tests-{Guid.NewGuid()}")
            .Options;
        Context = new AppDbContext(options);

        Context.Users.Add(new ApplicationUser { Id = UserId, UserName = "test@local" });
        Context.CarnetSantes.Add(new CarnetSante { Id = CarnetSanteId, UserId = UserId });
        Context.Users.Add(new ApplicationUser { Id = AutreUserId, UserName = AutreUserName });
        Context.CarnetSantes.Add(new CarnetSante { Id = AutreCarnetSanteId, UserId = AutreUserId });
        Context.SaveChanges();

        CarnetSanteService = new CarnetSanteService(
            Context,
            new MemoryCache(new MemoryCacheOptions()));
    }

    /// <summary>Contexte HTTP authentifié en tant que l'utilisatrice du carnet.</summary>
    public static ControllerContext ContexteAuthentifie() => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, UserId)], "Test")),
        },
    };

    /// <summary>Contexte HTTP sans utilisatrice authentifiée.</summary>
    public static ControllerContext ContexteAnonyme() => new()
    {
        HttpContext = new DefaultHttpContext(),
    };

    public void Dispose() => Context.Dispose();
}
