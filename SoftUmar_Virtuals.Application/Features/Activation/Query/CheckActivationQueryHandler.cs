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

        var order = await orderRepo.GetSingleAsync(o => o.Id == request.ActivationId && o.UserId == request.UserId);

        if (order == null)
            return Result.Fail("Activation not found.");

        var finalStates = new[] { "FINISHED", "CANCELED", "CANCELLED", "TIMEOUT", "BANNED" };
        if (finalStates.Contains(order.Status) && !string.IsNullOrEmpty(order.SmsCode))
        {
            return Result.Ok(ToDto(order));
        }

        if (!long.TryParse(order.ActivationId, out var providerOrderId))
        {
            await logger.LogError(
                $"ActivationId '{order.ActivationId}' is not a valid number for order {order.Id}.",
                nameof(CheckActivationQueryHandler), null, traceId);
            return Result.Ok(ToDto(order));
        }

        try
        {
            var fiveSimResult = await fiveSimClient.CheckOrderAsync(providerOrderId, cancellationToken);

            order.Status = fiveSimResult.Status ?? order.Status;

            var code = fiveSimResult.Sms?.FirstOrDefault()?.Code;
            var isNewCode = !string.IsNullOrEmpty(code) && order.SmsCode != code;

            if (!string.IsNullOrEmpty(code))
            {
                order.SmsCode = code;

                // FUTURE (separate module): push a SignalR "OtpReceived"
                // event here.
            }

            // NEW: automatically call Finish the first time a code
            // arrives - not on every subsequent check of an
            // already-finished order (isNewCode guards against calling
            // Finish repeatedly). This improves your 5SIM account
            // rating and frees the number up faster for other buyers,
            // per their docs' rating guidance.
            if (isNewCode && order.Status != "FINISHED")
            {
                try
                {
                    var finishResult = await fiveSimClient.FinishOrderAsync(providerOrderId, cancellationToken);
                    order.Status = finishResult.Status ?? "FINISHED";

                    await logger.LogInfo(
                        $"Order {order.ActivationId} auto-finished after SMS received.",
                        nameof(CheckActivationQueryHandler), traceId);
                }
                catch (FiveSimApiException ex)
                {
                    // Don't fail the whole check just because Finish
                    // failed - the user still got their code, which is
                    // what actually matters to them. Finish failing is
                    // a minor issue (affects your rating, not the user's
                    // experience) - log it and move on.
                    await logger.LogWarning(
                        $"Auto-finish failed for order {order.ActivationId}: {ex.Message}",
                        nameof(CheckActivationQueryHandler), traceId);
                }
            }

            order.UpdatedAt = DateTime.UtcNow;
            await orderRepo.SaveChanges(cancellationToken);
        }
        catch (FiveSimApiException ex)
        {
            await logger.LogWarning(
                $"5SIM check failed for order {order.ActivationId}: {ex.Message}",
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