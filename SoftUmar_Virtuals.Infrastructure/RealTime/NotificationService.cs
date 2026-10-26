using Microsoft.AspNetCore.SignalR;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Infrastructure.Realtime;


public class NotificationService(IHubContext<NotificationHub> hubContext) : INotificationService
{
    public Task NotifyOtpReceivedAsync(Guid userId, Guid activationId, string phoneNumber, string code)
    {
        // "OtpReceived" here is the EVENT NAME the frontend listens for
        // (connection.on("OtpReceived", ...) in JavaScript). Matches
        // your requirements doc's SignalR Events list exactly.
        return hubContext.Clients
            .Group(NotificationHub.GroupName(userId.ToString()))
            .SendAsync("OtpReceived", new { activationId, phoneNumber, code });
    }

    public Task NotifyActivationUpdatedAsync(Guid userId, Guid activationId, string status)
    {
        return hubContext.Clients
            .Group(NotificationHub.GroupName(userId.ToString()))
            .SendAsync("ActivationUpdated", new { activationId, status });
    }

    public Task NotifyWalletUpdatedAsync(Guid userId, decimal newBalance)
    {
        return hubContext.Clients
            .Group(NotificationHub.GroupName(userId.ToString()))
            .SendAsync("WalletUpdated", new { balance = newBalance });
    }

    public Task NotifyAsync(Guid userId, string title, string message)
    {
        return hubContext.Clients
            .Group(NotificationHub.GroupName(userId.ToString()))
            .SendAsync("NotificationReceived", new { title, message });
    }
}
