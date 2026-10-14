using BCrypt.Net;
using FluentResults;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftUmar_Virtuals.Application.Features.Auth.EmailVerification.Command;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;
using SoftUmar_Virtuals.Domain.Enum;

namespace SoftUmar_Virtuals.Application.Features.Auth.RegisterUser.Command
{
    public record RegisterCommand(RegisterUserDTO register) : IRequest<Result<RegisterCommandOutput>>;

    public class RegisterCommandHandler(
        IRepository<User> userRepository,
        IRepository<Wallets> walletRepo,
        IBackgroundJobClient backgroundJobClient,
        ILoggerService logger) : IRequestHandler<RegisterCommand, Result<RegisterCommandOutput>>
    {
        public async Task<Result<RegisterCommandOutput>> Handle(
            RegisterCommand request,
            CancellationToken cancellationToken)
        {
            var traceId = Guid.NewGuid().ToString();
            var dto = request.register;

            try
            {
                await logger.LogInfo($"Registration attempt for {dto.Email}",
                    nameof(RegisterCommandHandler), traceId);

                if (dto.Password != dto.ConfirmPassword)
                {
                    await logger.LogWarning($"Password mismatch for {dto.Email}",
                        nameof(RegisterCommandHandler), traceId);
                    return Result.Fail("Password and Confirm Password do not match.");
                }

                // NEW: at least one of BVN or NIN required, since your reserved
                // account creation (Monnify) needs one of them to succeed.
                // Basic format check too - both are 11-digit Nigerian
                // identifiers. This doesn't verify they're REAL/valid with
                // Monnify/NIBSS - that check happens when Monnify itself
                // processes the reserved-account request later.
                var hasBvn = !string.IsNullOrWhiteSpace(dto.Bvn);
                var hasNin = !string.IsNullOrWhiteSpace(dto.Nin);

                if (!hasBvn && !hasNin)
                {
                    return Result.Fail("Either BVN or NIN is required to register.");
                }

                if (hasBvn && (dto.Bvn!.Length != 11 || !dto.Bvn.All(char.IsDigit)))
                {
                    return Result.Fail("BVN must be 11 digits.");
                }

                if (hasNin && (dto.Nin!.Length != 11 || !dto.Nin.All(char.IsDigit)))
                {
                    return Result.Fail("NIN must be 11 digits.");
                }

                var userExist = userRepository.Exists(x => x.Email == dto.Email);

                if (userExist)
                {
                    await logger.LogWarning($"User with email {dto.Email} already exists.",
                        nameof(RegisterCommandHandler), traceId);
                    return Result.Fail("User with this email already exists.");
                }

                var encryptedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    PasswordHash = encryptedPassword,
                    PhoneNumber = dto.PhoneNumber,
                    Country = dto.Country,
                    Status = UserStatus.Active,
                    EmailVerified = false,
                    Bvn = dto.Bvn,   // NEW
                    Nin = dto.Nin,   // NEW
                };

                var wallet = new Wallets
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Balance = 0,
                    TotalCredit = 0,
                    TotalDebit = 0,
                    Email = user.Email,
                    ReservedAccountStatus = ReservedAccountStatus.Pending,
                };

                await userRepository.AddAsync(user, cancellationToken);
                await walletRepo.AddAsync(wallet, cancellationToken);

                try
                {
                    await userRepository.SaveChanges(cancellationToken);
                }
                catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                {
                    await logger.LogWarning($"Concurrent registration collision for {dto.Email}",
                        nameof(RegisterCommandHandler), traceId);
                    return Result.Fail("User with this email already exists.");
                }

                backgroundJobClient.Enqueue<IMonnifyReservedAccountService>(
                    svc => svc.CreateReservedAccount(user.Id));

                backgroundJobClient.Enqueue<IMediator>(
                    m => m.Send(new SendEmailVerificationCommand(user.Email), CancellationToken.None));

                await logger.LogInfo($"Registration completed successfully for {user.Email}",
                    nameof(RegisterCommandHandler), traceId);

                return Result.Ok(new RegisterCommandOutput
                {
                    UserId = user.Id,
                    Message = "Registration Successful. Please check your email to verify your account."
                });
            }
            catch (Exception ex)
            {
                await logger.LogError("Registration failed.",
                    nameof(RegisterCommandHandler), ex, traceId);
                return Result.Fail("An error occurred during registration.");
            }
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
            ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;
    }
}