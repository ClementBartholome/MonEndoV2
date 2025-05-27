using Microsoft.AspNetCore.Identity;

namespace MonEndoVue.Server.Models;

public class ApplicationUser : IdentityUser
{
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public ICollection<DeviceToken> DeviceToken { get; set; } = new List<DeviceToken>();
    public CarnetSante? CarnetSante { get; set; }
}