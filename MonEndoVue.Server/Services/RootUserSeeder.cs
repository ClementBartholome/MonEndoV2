// MonEndoVue.Server/Services/RootUserSeeder.cs

using Microsoft.AspNetCore.Identity;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services;

public class RootUserSeeder
{
    public static async Task Seed(IServiceScope scope, IConfiguration configuration, AppDbContext dbContext)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var rootUserSection = configuration.GetSection("RootUser");
        var rootUser = new ApplicationUser
        {
            UserName = rootUserSection["UserName"],
            Email = rootUserSection["Email"],
            EmailConfirmed = true
        };
        var rootPassword = rootUserSection["Password"];

        if (rootUser.UserName != null && await userManager.FindByNameAsync(rootUser.UserName) == null)
        {
            if (rootPassword != null) await userManager.CreateAsync(rootUser, rootPassword);
        }

        if (rootUser.UserName != null)
        {
            var user = await userManager.FindByNameAsync(rootUser.UserName);
            if (user != null && dbContext.CarnetSantes.All(c => c.UserId != user.Id))
            {
                var carnet = new CarnetSante { UserId = user.Id };
                dbContext.CarnetSantes.Add(carnet);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}