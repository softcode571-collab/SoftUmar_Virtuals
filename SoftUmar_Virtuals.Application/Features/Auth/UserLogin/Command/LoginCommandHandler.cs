
using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SoftUmar_Virtuals.Application.Features.Auth.UserLogin.Command
{
    public record LoginCommand(LoginDTO login) : IRequest<Result<LoginCommandOutput>>;
    public class LoginCommandHandler(IRepository<User> UserRepository, IRepository<Login> loginRepository, IJwtService jwtService) : IRequestHandler<LoginCommand, Result<LoginCommandOutput>>
    {
        public async Task<Result<LoginCommandOutput>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var dto = request.login;

            var user = await UserRepository.GetSingleAsync(u => u.Email == dto.Email);

            if (user == null)
                return Result.Fail("Invalid email or password.");

            var passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

            if (!passwordValid)
                return Result.Fail("Invalid email or password.");

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
        }
    }

