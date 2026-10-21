using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;

namespace SoftUmar_Virtuals.Application.Features.Activation.Query
{
    public record GetAvailableServicesQuery(
        string Country,
        string Operator = "any") : IRequest<Result<List<AvailableServiceDTO>>>;

    public class AvailableServiceDTO
    {
        public required string Product { get; set; }
        public int Quantity { get; set; }
        public decimal PriceUsd { get; set; } // your marked-up USD price
        public decimal PriceNgn { get; set; } // same price, converted
    }

    public class GetAvailableServicesQueryHandler(
        IFiveSimClient fiveSimClient,
        IPricingService pricingService) : IRequestHandler<GetAvailableServicesQuery, Result<List<AvailableServiceDTO>>>
    {
        public async Task<Result<List<AvailableServiceDTO>>> Handle(
            GetAvailableServicesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var products = await fiveSimClient.GetAvailableProductsAsync(
                    request.Country, request.Operator, cancellationToken);

                var result = new List<AvailableServiceDTO>();

                foreach (var product in products)
                {
                    var pricing = await pricingService.GetSellingPriceAsync(
                        product.Price, cancellationToken);

                    result.Add(new AvailableServiceDTO
                    {
                        Product = product.Product,
                        Quantity = product.Quantity,
                        PriceUsd = pricing.UsdPrice,
                        PriceNgn = pricing.NgnPrice,
                    });
                }

                return Result.Ok(result);
            }
            catch (FiveSimApiException)
            {
                return Result.Fail("Could not load available services right now.");
            }
        }
    }
}