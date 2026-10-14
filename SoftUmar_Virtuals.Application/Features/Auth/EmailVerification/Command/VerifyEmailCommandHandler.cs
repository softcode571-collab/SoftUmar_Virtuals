using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;

namespace SoftUmar_Virtuals.Application.Features.Auth.EmailVerification.Command
{
    public record VerifyEmailCommand(string Email, string Code) : IRequest<Result>;

    public class VerifyEmailCommandHandler(
        IRepository<User> userRepository,
        ILoggerService logger) : IRequestHandler<VerifyEmailCommand, Result>
    {
        private const int MaxAttempts = 5;

        public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
        {
            var traceId = Guid.NewGuid().ToString();

            try
            {
                var user = await userRepository.GetSingleAsync(u => u.Email == request.Email);

                if (user == null)
                    return Result.Fail("Invalid or expired code.");

                if (user.EmailVerified)
                    return Result.Ok(); // idempotent - verifying twice is fine

                if (user.EmailVerificationToken == null ||
                    user.EmailVerificationTokenExpiry is null ||
                    user.EmailVerificationTokenExpiry < DateTime.UtcNow)
                {
                    return Result.Fail("Invalid or expired code.");
                }

                if (user.EmailVerificationAttemptCount >= MaxAttempts)
                {
                    return Result.Fail("Too many incorrect attempts. Please request a new code.");
                }

                var isValid = BCrypt.Net.BCrypt.Verify(request.Code, user.EmailVerificationToken);

                if (!isValid)
                {
                    user.EmailVerificationAttemptCount++;
                    await userRepository.SaveChanges(cancellationToken);

                    var attemptsLeft = MaxAttempts - user.EmailVerificationAttemptCount;
                    await logger.LogWarning(
                        $"Invalid verification OTP attempt for {request.Email}",
                        nameof(VerifyEmailCommandHandler), traceId);

                    return Result.Fail(attemptsLeft > 0
                        ? $"Incorrect code. {attemptsLeft} attempt(s) remaining."
                        : "Incorrect code. Please request a new one.");
                }

                user.EmailVerified = true;
                user.EmailVerificationToken = null;
                user.EmailVerificationTokenExpiry = null;
                user.EmailVerificationAttemptCount = 0;

                await userRepository.SaveChanges(cancellationToken);

                await logger.LogInfo($"Email verified for {request.Email}",
                    nameof(VerifyEmailCommandHandler), traceId);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                await logger.LogError("Email verification failed.",
                    nameof(VerifyEmailCommandHandler), ex, traceId);
                return Result.Fail("An error occurred verifying your email.");
            }
        }
    }
}