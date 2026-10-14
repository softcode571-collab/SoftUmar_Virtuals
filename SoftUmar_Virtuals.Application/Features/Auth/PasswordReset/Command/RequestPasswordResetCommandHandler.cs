using FluentResults;
using MediatR;
using Microsoft.Extensions.Configuration;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;


namespace SoftUmar_Virtuals.Application.Features.Auth.PasswordReset.Command
{
    public record RequestPasswordResetCommand(string Email) : IRequest<Result>;

    public class RequestPasswordResetCommandHandler(
        IRepository<User> userRepository,
        IOtpService otpService,
        IConfiguration config,
        IEmailService emailService,
        ILoggerService logger) : IRequestHandler<RequestPasswordResetCommand, Result>
    {
        public async Task<Result> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
        {
            var traceId = Guid.NewGuid().ToString();

            try
            {
                var user = await userRepository.GetSingleAsync(u => u.Email == request.Email);

                // Same response either way — don't reveal whether the email
                // is registered. Same reasoning as before, just no link now.
                if (user == null)
                {
                    await logger.LogWarning(
                        $"Password reset requested for non-existent email {request.Email}",
                        nameof(RequestPasswordResetCommandHandler), traceId);
                    return Result.Ok();
                }

                // 6-digit numeric code, easy to type on a phone. Swap for
                // longer/alphanumeric if you want more entropy.
                var code = Random.Shared.Next(100000, 999999).ToString();

                // NEW fields on User: same pattern as EmailVerificationToken,
                // but storing a HASHED code, not the raw code - same reason
                // your OTP handler in LightCap hashes it with BCrypt.
                user.PasswordResetToken = BCrypt.Net.BCrypt.HashPassword(code);
                user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15); // short-lived,
                                                                                // shorter than the link version's 1hr since a 6-digit code
                                                                                // is more brute-forceable than a long random token - see
                                                                                // the attempt-count check in ResetPasswordCommand below.
                user.PasswordResetAttemptCount = 0; // NEW field, resets each time
                                                    // a fresh code is requested

                await userRepository.SaveChanges(cancellationToken);

                var generatedOtp = otpService.GenerateOtp(); 
                var expiryMinutes = int.Parse(config["OTP:ExpiryMinutes"]!);
                var emailSubject = "Password Reset OTP";
                var emailContent =
                    $"Your OTP for email verification is: {generatedOtp}. " +
                    $"It will expire in {expiryMinutes} minutes.";

                await emailService.SendEmailWithFallback(user.Email, emailContent, emailSubject);

                await logger.LogInfo($"Password reset OTP sent to {user.Email}",
                    nameof(RequestPasswordResetCommandHandler), traceId);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                await logger.LogError("Failed to send password reset OTP.",
                    nameof(RequestPasswordResetCommandHandler), ex, traceId);
                return Result.Ok(); // still don't leak anything specific
            }
        }
    }
}
