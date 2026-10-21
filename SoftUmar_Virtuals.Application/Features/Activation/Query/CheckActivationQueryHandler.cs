using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;
using SoftUmar_Virtuals.Infrastructure.Services.FiveSim;

namespace SoftUmar_Virtuals.Application.Features.FiveSim.Activation.Query;

public record CheckActivationQuery(Guid UserId, Guid ActivationId) : IRequest<Result<ActivationStatusDTO>>;

public class CheckActivationQueryHandler(
    IRepository<FiveSimOrder> orderRepo,
    IFiveSimClient fiveSimClient,
    ILoggerService logger) : IRequestHandler<CheckActivationQuery, Result<ActivationStatusDTO>>
{
    public async Task<Result<ActivationStatusDTO>> Handle(
        CheckActivationQuery request, CancellationToken cancellationToken)
    {
        var traceId = Guid.NewGuid().ToString();

        var order = await orderRepo.GetSingleAsync(
            o => o.Id == request.ActivationId && o.UserId == request.UserId);

        if (order == null)
            return Result.Fail("Activation not found.");

        // Don't bother calling 5SIM again if we already know it's in a
        // final state - saves an API call and avoids pointless polling
        // of a completed/dead order.
        var finalStates = new[] { "FINISHED", "CANCELED", "TIMEOUT", "BANNED" };
        if (finalStates.Contains(order.Status) && !string.IsNullOrEmpty(order.SmsCode))
        {
            return Result.Ok(ToDto(order));
        }

        try
        {
            var fiveSimResult = await fiveSimClient.CheckOrderAsync(order.ProviderOrderId, cancellationToken);

            order.Status = fiveSimResult.Status ?? order.Status;

            var code = fiveSimResult.Sms?.FirstOrDefault()?.Code;
            if (!string.IsNullOrEmpty(code))
            {
                order.SmsCode = code;

                // FUTURE (separate module, not built here): push a
                // SignalR "OtpReceived" event here so the frontend gets
                // it live instead of polling this endpoint repeatedly.
            }

            await orderRepo.SaveChanges(cancellationToken);
        }
        catch (FiveSimApiException ex)
        {
            // Don't fail the whole request just because the live poll
            // failed - return what we have locally instead, since the
            // user still wants to see the last known status.
            await logger.LogWarning(
                $"5SIM check failed for order {order.ProviderOrderId}: {ex.Message}",
                nameof(CheckActivationQueryHandler), traceId);
        }

        return Result.Ok(ToDto(order));
    }

    private static ActivationStatusDTO ToDto(FiveSimOrder order) => new()
    {
        ActivationId = order.Id,
        PhoneNumber = order.PhoneNumber,
        Status = order.Status,
        OtpCode = order.SmsCode,
        ExpiresAt = order.ExpiresAt,
    };
}