using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;
using SoftUmar_Virtuals.Domain.Enum;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Application.Features.Auth.UserLogin.Command
{
    public record LoginCommand(LoginDTO login) : IRequest<Result<LoginCommandOutput>>;

    public class LoginCommandHandler(
        IRepository<User> UserRepository,
        IRepository<Login> loginRepository,
        IJwtService jwtService,
        ILoggerService logger) : IRequestHandler<LoginCommand, Result<LoginCommandOutput>>
    {
        // Precomputed BCrypt hash of a dummy value, used only to burn roughly the
        // same amount of time as a real Verify() call when the user doesn't exist,
        // so "email not found" and "wrong password" aren't distinguishable by
        // response time. Generate your own once (BCrypt.HashPassword("anything"))
        // and hardcode the result — don't compute it per-request.
        private const string DummyHash =
            "$2a$11$CwTycUXWue0Thq9StjUM0uJ8f9ZiXNQ.Ik2q3kL0f5b6s6D3a3d3C";

        public async Task<Result<LoginCommandOutput>> Handle(
            LoginCommand request, CancellationToken cancellationToken)
        {
            var traceId = Guid.NewGuid().ToString();
            var dto = request.login;

            try
            {
                await logger.LogInfo($"Login attempt for {dto.Email}",
                    nameof(LoginCommandHandler), traceId);

                var user = await UserRepository.GetSingleAsync(u => u.Email == dto.Email);

                if (user == null)
                {
                    BCrypt.Net.BCrypt.Verify(dto.Password, DummyHash);

                    await logger.LogWarning($"Login failed - no user with email {dto.Email}",
                        nameof(LoginCommandHandler), traceId);

                    return Result.Fail("Invalid email or password.");
                }

                var passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

                if (!passwordValid)
                {
                    await logger.LogWarning($"Login failed - bad password for {dto.Email}",
                        nameof(LoginCommandHandler), traceId);

                    return Result.Fail("Invalid email or password.");
                }

                // NEW: reject banned/suspended accounts before issuing tokens.
                if (user.Status == UserStatus.Banned)
                {
                    await logger.LogWarning($"Login blocked - banned account {dto.Email}",
                        nameof(LoginCommandHandler), traceId);

                    return Result.Fail("This account has been suspended.");
                }

                // NEW: reject unverified accounts. Drop this if you want to allow
                // pre-verification login with restricted access instead — but then
                // gate wallet funding / activation purchase on EmailVerified elsewhere.
                if (!user.EmailVerified)
                {
                    await logger.LogWarning($"Login blocked - unverified email {dto.Email}",
                        nameof(LoginCommandHandler), traceId);

                    return Result.Fail("Please verify your email before logging in.");
                }

                var accessToken = await jwtService.GenerateAccessToken(user);
                var refreshToken = jwtService.GenerateRefreshToken();

                var login = new Login
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Email = user.Email,
                    LoginTime = DateTime.UtcNow,
                    IsActive = true,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7),
                };

                await loginRepository.AddAsync(login, cancellationToken);
                await loginRepository.SaveChanges(cancellationToken);

                await logger.LogInfo($"Login successful for {dto.Email}",
                    nameof(LoginCommandHandler), traceId);

                return Result.Ok(new LoginCommandOutput
                {
                    UserId = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Email = user.Email,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiry = login.RefreshTokenExpiryTime ?? DateTime.UtcNow.AddDays(7)
                });
            }
            catch (Exception ex)
            {
                await logger.LogError("Login failed.",
                    nameof(LoginCommandHandler), ex, traceId);

                return Result.Fail("An error occurred during login.");
            }
        }
    }
}