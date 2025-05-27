namespace MonEndoVue.Server.Models;

public class DeviceToken
{
    public int Id { get; set; }
    public string Token { get; set; }
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
}