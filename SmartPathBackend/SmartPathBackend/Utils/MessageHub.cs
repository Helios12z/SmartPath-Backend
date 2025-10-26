using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace SmartPathBackend.Utils
{
    [Authorize] 
    public class MessageHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var uid = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "UNKNOWN";
            Console.WriteLine($"[HUB] Connected: {Context.ConnectionId} user={uid}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"[HUB] Disconnected: {Context.ConnectionId} err={exception?.Message}");
            await base.OnDisconnectedAsync(exception);
        }

        public Task JoinChat(string chatId) =>
            Groups.AddToGroupAsync(Context.ConnectionId, $"chat-{chatId}");

        public Task LeaveChat(string chatId) =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat-{chatId}");

        public Task<string> Ping() => Task.FromResult("pong");
    }
}
