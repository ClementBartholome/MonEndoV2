using Microsoft.AspNetCore.SignalR;

namespace MonEndoVue.Server.Hubs;

public class NotificationHub : Hub
{
    public async Task NotifyAllClients(string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", message);
    }
    
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Client connecté : {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"Client déconnecté : {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}