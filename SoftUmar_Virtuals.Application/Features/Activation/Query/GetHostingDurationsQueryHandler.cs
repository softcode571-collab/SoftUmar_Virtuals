using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;

namespace SoftUmar_Virtuals.Application.Features.Activation.Query
{
    public record GetHostingDurationsQuery(
        string Country, string Operator = "any") : IRequest<Result<List<AvailableServiceDTO>>>;

    public class GetHostingDurationsQueryHandler(
        IFiveSimClient fiveSimClient,
        IPricingService pricingService) : IRequestHandler<GetHostingDurationsQuery, Result<List<AvailableServiceDTO>>>
    {
        public async Task<Result<List<AvailableServiceDTO>>> Handle(
            GetHostingDurationsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var durations = await fiveSimClient.GetAvailableHostingDurationsAsync(
                    request.Country, request.Operator, cancellationToken);

                var result = new List<AvailableServiceDTO>();

                foreach (var duration in durations)
                {
                    var pricing = await pricingService.GetSellingPriceAsync(duration.Price, cancellationToken);

                    result.Add(new AvailableServiceDTO
                    {
                        Product = duration.Product, // e.g. "1month" - a
                                                    // duration, not a service
                        Quantity = duration.Quantity,
                        PriceUsd = pricing.UsdPrice,
                        PriceNgn = pricing.NgnPrice,
                    });
                }

                return Result.Ok(result);
            }
            catch (FiveSimApiException)
            {
                return Result.Fail("Could not load rental durations right now.");
            }
        }
    }
}