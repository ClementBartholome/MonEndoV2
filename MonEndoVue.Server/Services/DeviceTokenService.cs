using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace MonEndoVue.Server.Services;

public class DeviceTokenService(AppDbContext dbContext)
{
    public async Task<DeviceToken?> GetExistingTokenAsync(string deviceToken, string userId)
    {
        return await dbContext.DeviceTokens
            .FirstOrDefaultAsync(dt => dt.Token == deviceToken && dt.UserId == userId);
    }

    public async Task SaveDeviceTokenAsync(string deviceToken, string userId)
    {
        var newDeviceToken = new DeviceToken
        {
            Token = deviceToken,
            UserId = userId
        };

        dbContext.DeviceTokens.Add(newDeviceToken);
        await dbContext.SaveChangesAsync();
    }
}