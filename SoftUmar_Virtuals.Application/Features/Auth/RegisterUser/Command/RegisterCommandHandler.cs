using BCrypt.Net;
using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;

namespace SoftUmar_Virtuals.Application.Features.Auth.RegisterUser.Command
{
    public record RegisterCommand(RegisterUserDTO register) : IRequest<Result<RegisterCommandOutput>>;

    public class RegisterCommandHandler(IRepository<User> userRepository, IRepository<Wallets> walletRepo, ILoggerService logger) : IRequestHandler<RegisterCommand, Result<RegisterCommandOutput>>
    {
        public async Task<Result<RegisterCommandOutput>> Handle(
            RegisterCommand request,
            CancellationToken cancellationToken)
        {
            var traceId = Guid.NewGuid().ToString();

            try
            {
                var dto = request.register;

                await logger.LogInfo($"Registration attempt for {dto.Email}",
                    nameof(RegisterCommandHandler),
                    traceId);

                var userExist = userRepository.Exists(x => x.Email == dto.Email);

                if (userExist)
                {
                    await logger.LogWarning(
                        $"User with email {dto.Email} already exists.",
                        nameof(RegisterCommandHandler),
                        traceId);

                    return Result.Fail("User with this email already exists.");
                }

                if (dto.Password != dto.ConfirmPassword)
                {
                    await logger.LogWarning(
                        $"Password mismatch for {dto.Email}",
                        nameof(RegisterCommandHandler),
                        traceId);

                    return Result.Fail("Password and Confirm Password do not match.");
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
                };

                await userRepository.AddAsync(
                    user,
                    cancellationToken);

                await logger.LogInfo($"User account created for {user.Email}",
                    nameof(RegisterCommandHandler),
                    traceId);

                var wallet = new Wallets
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Balance = 0,
                    TotalCredit = 0,
                    TotalDebit = 0,
                    email = user.Email,
                };

                await walletRepo.AddAsync(wallet, cancellationToken);

                await logger.LogInfo(
                    $"Wallet created for {user.Email}",
                    nameof(RegisterCommandHandler),
                    traceId);

                await userRepository.SaveChanges(cancellationToken);
                await walletRepo.SaveChanges(cancellationToken);

                await logger.LogInfo($"Registration completed successfully for {user.Email}",
                    nameof(RegisterCommandHandler),
                    traceId);

                return Result.Ok(new RegisterCommandOutput
                {
                    UserId = user.Id,
                    Message = "Registration Successful."
                });
            }
            catch (Exception ex)
            {
                await logger.LogError(
                    "Registration failed.",
                    nameof(RegisterCommandHandler),
                    ex,
                    traceId);

                return Result.Fail(
                    "An error occurred during registration.");
            }
        }
    }
}