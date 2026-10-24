using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;

namespace SoftUmar_Virtuals.Application.Features.Activation.Query
{
    public record GetHostingInboxQuery(Guid UserId, Guid ActivationId) : IRequest<Result<List<FiveSimInboxMessage>>>;

    public class GetHostingInboxQueryHandler(
        IRepository<SoftUmar_Virtuals.Domain.Entities.FiveSimOrder> orderRepo,
        IFiveSimClient fiveSimClient,
        ILoggerService logger) : IRequestHandler<GetHostingInboxQuery, Result<List<FiveSimInboxMessage>>>
    {
        public async Task<Result<List<FiveSimInboxMessage>>> Handle(
            GetHostingInboxQuery request, CancellationToken cancellationToken)
        {
            var order = await orderRepo.GetSingleAsync(
                o => o.Id == request.ActivationId && o.UserId == request.UserId);

            if (order == null)
                return Result.Fail("Hosting order not found.");

            if (order.Category != "Hosting")
                return Result.Fail("This is not a hosting order - use the regular Check endpoint instead.");

            if (!long.TryParse(order.ActivationId, out var providerOrderId))
                return Result.Fail("This order has a data error and cannot be checked.");

            try
            {
                var messages = await fiveSimClient.GetSmsInboxAsync(providerOrderId, cancellationToken);
                return Result.Ok(messages);
            }
            catch (FiveSimApiException ex)
            {
                await logger.LogWarning($"5SIM inbox check failed for order {order.ActivationId}: {ex.Message}",
                    nameof(GetHostingInboxQueryHandler), Guid.NewGuid().ToString());
                return Result.Fail("Could not load inbox right now.");
            }
        }
    }
}