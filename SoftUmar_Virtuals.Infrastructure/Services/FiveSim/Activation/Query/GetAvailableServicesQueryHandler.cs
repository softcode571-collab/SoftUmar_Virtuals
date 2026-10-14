using FluentResults;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Infrastructure.Services.FiveSim.Activation.Query
{
    public record GetAvailableServicesQuery(string Country, string Operator = "any") : IRequest<Result<List<FiveSimProductAvailability>>>;

    public class GetAvailableServicesQueryHandler(
        FiveSimClient fiveSimClient) : IRequestHandler<GetAvailableServicesQuery, Result<List<FiveSimProductAvailability>>>
    {
        public async Task<Result<List<FiveSimProductAvailability>>> Handle(
            GetAvailableServicesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var products = await fiveSimClient.GetAvailableProductsAsync(
                    request.Country, request.Operator, cancellationToken);

                return Result.Ok(products);
            }
            catch (FiveSimApiException)
            {
                return Result.Fail("Could not load available services right now.");
            }
        }
    }
}
