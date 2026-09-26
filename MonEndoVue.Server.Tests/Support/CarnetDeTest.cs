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
/// Base EF en mémoire isolée, contenant une utilisatrice et son carnet de santé.
/// </summary>
public sealed class CarnetDeTest : IDisposable
{
    public const string UserId = "utilisatrice-test";
    public const int CarnetSanteId = 1;

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
        Context.SaveChanges();

        CarnetSanteService = new CarnetSanteService(
            Context,
            NullLogger<CarnetSanteService>.Instance,
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

    public void Dispose() => Context.Dispose();
}
