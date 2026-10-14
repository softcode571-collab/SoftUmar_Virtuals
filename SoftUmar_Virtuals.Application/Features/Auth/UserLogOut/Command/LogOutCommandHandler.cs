using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;

namespace SoftUmar_Virtuals.Application.Features.Auth.UserLogOut.Command
{
    // Identify the session by refresh token, not email — a user can have
    // multiple active sessions (multiple devices), so email alone can't tell
    // you WHICH one to log out, and letting a client just pass any email
    // would let them log out someone else's session entirely.
    public record LogOutCommand(string RefreshToken) : IRequest<Result<LogOutCommandResponse>>;

    public class LogOutCommandHandler(
        IRepository<Login> loginRepository,
        ILoggerService logger) : IRequestHandler<LogOutCommand, Result<LogOutCommandResponse>>
    {
        public async Task<Result<LogOutCommandResponse>> Handle(LogOutCommand request, CancellationToken cancellationToken)
        {
            var traceId = Guid.NewGuid().ToString();

            try
            {
                var login = await loginRepository.GetSingleAsync(
                    x => x.RefreshToken == request.RefreshToken && x.IsActive);

                // Logout is idempotent - if there's no matching active session
                // (already logged out, token expired, whatever), still return
                // success. A retried logout call shouldn't surface as an error.
                if (login == null)
                {
                    return Result.Ok(new LogOutCommandResponse
                    {
                        Message = "Logged out."
                    });
                }

                login.IsActive = false;
                login.LogoutTime = DateTime.UtcNow; // kept from your version
                login.RefreshToken = null;           // kept from your version
                login.RefreshTokenExpiryTime = null;

                // FIX: no AddAsync here. `login` is already tracked by EF Core
                // from GetSingleAsync above - just changing its properties and
                // calling SaveChanges is enough. Calling AddAsync on an
                // already-existing, already-tracked entity tells EF to INSERT
                // it again, which is wrong and will likely throw or duplicate.
                await loginRepository.SaveChanges(cancellationToken);

                await logger.LogInfo($"Logout for {login.Email}",
                    nameof(LogOutCommandHandler), traceId);

                return Result.Ok(new LogOutCommandResponse
                {
                    Message = "Logged out."
                });
            }
            catch (Exception ex)
            {
                // FIX: was returning Result.Ok(ex.Message) here before -
                // silently telling the client "success" while leaking the raw
                // exception text. Real failures should be Result.Fail with a
                // generic message, and the real exception goes to your logs
                // only, not the response body.
                await logger.LogError("Logout failed.",
                    nameof(LogOutCommandHandler), ex, traceId);

                return Result.Fail("An error occurred logging out.");
            }
        }
    }
}

   