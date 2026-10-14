using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;

namespace SoftUmar_Virtuals.Application.Features.Auth.EmailVerification.Command
{
    public record SendEmailVerificationCommand(string Email) : IRequest<Result>;

    public class SendEmailVerificationCommandHandler(
        IRepository<User> userRepository,
        IEmailService emailService,
        ILoggerService logger) : IRequestHandler<SendEmailVerificationCommand, Result>
    {
        public async Task<Result> Handle(SendEmailVerificationCommand request, CancellationToken cancellationToken)
        {
            var traceId = Guid.NewGuid().ToString();

            try
            {
                var user = await userRepository.GetSingleAsync(u => u.Email == request.Email);

                if (user == null)
                {
                    await logger.LogWarning(
                        $"Verification OTP requested for non-existent email {request.Email}",
                        nameof(SendEmailVerificationCommandHandler), traceId);
                    return Result.Ok();
                }

                if (user.EmailVerified)
                    return Result.Ok();

                // Same OTP pattern as password reset: 6-digit code, hashed
                // before storing, short expiry, attempt counter reset fresh.
                var code = Random.Shared.Next(100000, 999999).ToString();

                user.EmailVerificationToken = BCrypt.Net.BCrypt.HashPassword(code);
                user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddMinutes(15);
                user.EmailVerificationAttemptCount = 0; // NEW field on User

                await userRepository.SaveChanges(cancellationToken);

                var subject = "Verify your email";
                var body = $"""
                    <p>Your email verification code is:</p>
                    <p><strong style="font-size: 20px; letter-spacing: 2px;">{code}</strong></p>
                    <p>This code expires in 15 minutes.</p>
                    """;

                var emailResult = await emailService.SendEmailWithFallback(user.Email, subject, body);

                if (!emailResult.Success)
                {
                    await logger.LogWarning(
                        $"Verification OTP failed to send to {user.Email}: {emailResult.Message}",
                        nameof(SendEmailVerificationCommandHandler), traceId);
                    return Result.Fail(emailResult.Message);
                }

                await logger.LogInfo($"Verification OTP sent to {user.Email}",
                    nameof(SendEmailVerificationCommandHandler), traceId);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                await logger.LogError("Failed to send verification OTP.",
                    nameof(SendEmailVerificationCommandHandler), ex, traceId);
                return Result.Fail("Could not send verification code.");
            }
        }
    }
}