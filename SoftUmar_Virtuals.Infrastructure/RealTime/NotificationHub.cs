using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

// IMPORTANT PLACEMENT NOTE: this lives in Infrastructure, NOT your API
// project's Controllers/Hubs folder. Why: NotificationService (next file)
// needs to reference this Hub class directly (IHubContext<NotificationHub>),
// and NotificationService needs to live somewhere Application-layer
// handlers CAN see (via its interface). Same reasoning as FiveSimClient/
// MonnifyClient living in Infrastructure, not API - keeps the dependency
// direction consistent and avoids the exact redline problem we spent a
// long time fixing earlier.
namespace SoftUmar_Virtuals.Infrastructure.Realtime
{
    [Authorize] // only a valid JWT can even establish this connection -
                // same protection as your REST endpoints, for the
                // WebSocket connection instead
    public class NotificationHub : Hub
    {
        // Runs the moment a client connects (after authenticating). Adds
        // this connection to a GROUP named after their user id - a user
        // with 2 browser tabs open gets 2 connections, but both join the
        // SAME group, so a notification sent to "their group" reaches
        // both tabs at once.
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId));
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(userId));
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Shared naming convention - NotificationService sends to the EXACT
        // same group name this hub creates, so they have to agree.
        public static string GroupName(string userId) => $"user-{userId}";
    }
}