
using SoftUmar_Virtuals.Application.Common.Email;

namespace SoftUmar_Virtuals.Application.Interfaces;

public interface IEmailService
    {
    Task<EmailResponse> SendEmailWithFallback(string to, string subject, string body, bool isHtml = true);
}

