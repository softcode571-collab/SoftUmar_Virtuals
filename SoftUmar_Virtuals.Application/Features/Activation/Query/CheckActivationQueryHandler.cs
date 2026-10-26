using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Features.FiveSim.Activation.Query;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;

namespace SoftUmar_Virtuals.Application.Features.Activation.Query
{
    public record CheckActivationQuery(Guid UserId, Guid ActivationId) : IRequest<Result<ActivationStatusDTO>>;

    public class CheckActivationQueryHandler(
        IRepository<FiveSimOrder> orderRepo,
        IFiveSimClient fiveSimClient,
        INotificationService notificationService, 
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
                var statusChanged = fiveSimResult.Status != null && fiveSimResult.Status != order.Status;

                order.Status = fiveSimResult.Status ?? order.Status;

                var code = fiveSimResult.Sms?.FirstOrDefault()?.Code;
                var isNewCode = !string.IsNullOrEmpty(code) && order.SmsCode != code;

                if (!string.IsNullOrEmpty(code))
                {
                    order.SmsCode = code;
                }

                if (isNewCode)
                {
                    try
                    {
                        var finishResult = await fiveSimClient.FinishOrderAsync(providerOrderId, cancellationToken);
                        order.Status = finishResult.Status ?? "FINISHED";
                        statusChanged = true;

                        await logger.LogInfo(
                            $"Order {order.ActivationId} auto-finished after SMS received.",
                            nameof(CheckActivationQueryHandler), traceId);
                    }
                    catch (FiveSimApiException ex)
                    {
                        await logger.LogWarning(
                            $"Auto-finish failed for order {order.ActivationId}: {ex.Message}",
                            nameof(CheckActivationQueryHandler), traceId);
                    }

                    // NEW: push the code to the frontend live, the instant
                    // it's known - the user doesn't have to have been the
                    // one who triggered this specific Check call (e.g. a
                    // background polling job could trigger this same code
                    // path later - module not built yet, but this event
                    // push works regardless of what triggers it).
                    await notificationService.NotifyOtpReceivedAsync(
                        order.UserId, order.Id, order.PhoneNumber, code!);
                }

                if (statusChanged)
                {
                    // NEW: separate event for status changes in general
                    // (PENDING -> RECEIVED -> FINISHED etc), even on turns
                    // where no new code arrived.
                    await notificationService.NotifyActivationUpdatedAsync(
                        order.UserId, order.Id, order.Status);
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
}