using SoftUmar_Virtuals.Infrastructure.Services.FiveSim;

namespace SoftUmar_Virtuals.Application.Interfaces
{
    // The CONTRACT. Your Application-layer handlers (BuyActivationCommand,
    // CheckActivationQuery, etc.) depend on THIS, never on the concrete
    // FiveSimClient class in Infrastructure. Same pattern as IJwtService,
    // IEmailService, IMonnifyReservedAccountService.
    public interface IFiveSimClient
    {
        Task<List<FiveSimProductAvailability>> GetAvailableProductsAsync(
            string country, string @operator, CancellationToken cancellationToken);

        Task<decimal?> GetCheapestPriceAsync(
            string country, string product, CancellationToken cancellationToken);

        Task<List<FiveSimCountry>> GetCountriesAsync(CancellationToken cancellationToken);

        Task<FiveSimOrderResult> BuyActivationNumberAsync(
            string country, string @operator, string product, CancellationToken cancellationToken);

        Task<FiveSimOrderResult> CheckOrderAsync(long orderId, CancellationToken cancellationToken);

        Task<FiveSimOrderResult> FinishOrderAsync(long orderId, CancellationToken cancellationToken);

        Task<FiveSimOrderResult> CancelOrderAsync(long orderId, CancellationToken cancellationToken);
    }

    // DTOs moved here too - they're shared VOCABULARY between Application
    // and Infrastructure, not infrastructure-specific implementation
    // details, so they belong at this boundary.
    

    public class FiveSimProductAvailability
    {
        public required string Product { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class FiveSimApiException(string code, string message) : Exception(message)
    {
        public string Code { get; } = code;
    }

    public class FiveSimCountry
    {
        public required string Code { get; set; }
        public required string NameEnglish { get; set; }
        public string? Iso { get; set; }
        public string? Prefix { get; set; }
    }
}