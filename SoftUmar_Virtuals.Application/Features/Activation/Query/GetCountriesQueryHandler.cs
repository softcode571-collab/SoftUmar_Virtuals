using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;

namespace SoftUmar_Virtuals.Application.Features.Activation.Query
{
    public record GetCountriesQuery() : IRequest<Result<List<FiveSimCountry>>>;

    public class GetCountriesQueryHandler(
        IFiveSimClient fiveSimClient) : IRequestHandler<GetCountriesQuery, Result<List<FiveSimCountry>>>
    {
        public async Task<Result<List<FiveSimCountry>>> Handle(
            GetCountriesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var countries = await fiveSimClient.GetCountriesAsync(cancellationToken);
                return Result.Ok(countries);
            }
            catch (FiveSimApiException)
            {
                return Result.Fail("Could not load countries list right now.");
            }
        }
    }
}