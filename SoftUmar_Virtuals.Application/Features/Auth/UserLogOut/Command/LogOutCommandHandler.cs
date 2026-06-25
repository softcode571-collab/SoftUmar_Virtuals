using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Application.Features.Auth.UserLogOut.Command
{
    public record LogOutCommand(string email) : IRequest<Result<LogOutCommandResponse>>;

    public class LogOutCommandHandler(IRepository<Login> loginRepository) : IRequestHandler<LogOutCommand, Result<LogOutCommandResponse>>
    {
        public async Task<Result<LogOutCommandResponse>> Handle(LogOutCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await loginRepository.GetSingleAsync(x => x.Email == request.email && x.IsActive);

                if (user == null)
                    return Result.Fail("User not found.");

                user.IsActive = false;
                user.LogoutTime = DateTime.UtcNow;

                //invalidate refresh token
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;

                await loginRepository.AddAsync(user, cancellationToken);
                await loginRepository.SaveChanges(cancellationToken);

                return Result.Ok(new LogOutCommandResponse
                {
                    Message = "User logged out successfully."
                });
            }
            catch (Exception ex)
            {

                return Result.Ok(new LogOutCommandResponse
                {
                    Message = ex.Message
                });
            }
        }
    }
}
