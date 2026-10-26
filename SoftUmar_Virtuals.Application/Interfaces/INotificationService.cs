using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Application.Interfaces
{
    public interface INotificationService
    {
        Task NotifyOtpReceivedAsync(Guid userId, Guid activationId, string phoneNumber, string code);
        Task NotifyActivationUpdatedAsync(Guid userId, Guid activationId, string status);
        Task NotifyWalletUpdatedAsync(Guid userId, decimal newBalance);
        Task NotifyAsync(Guid userId, string title, string message);
    }
}
