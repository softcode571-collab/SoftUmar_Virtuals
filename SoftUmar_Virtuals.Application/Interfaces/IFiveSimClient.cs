using SoftUmar_Virtuals.Infrastructure.Services.FiveSim;

namespace SoftUmar_Virtuals.Application.Interfaces
{
    public interface IFiveSimClient
    {
        // --- Activation (one-time SMS verification) ---
        Task<List<FiveSimProductAvailability>> GetAvailableProductsAsync(
            string country, string @operator, CancellationToken cancellationToken);

        Task<decimal?> GetCheapestPriceAsync(
            string country, string product, CancellationToken cancellationToken);

        Task<FiveSimOrderResult> BuyActivationNumberAsync(
            string country, string @operator, string product, CancellationToken cancellationToken);

        Task<FiveSimOrderResult> CheckOrderAsync(long orderId, CancellationToken cancellationToken);

        Task<FiveSimOrderResult> FinishOrderAsync(long orderId, CancellationToken cancellationToken);

        Task<FiveSimOrderResult> CancelOrderAsync(long orderId, CancellationToken cancellationToken);

        // --- Countries ---
        Task<List<FiveSimCountry>> GetCountriesAsync(CancellationToken cancellationToken);

        // --- Hosting (duration-based rental numbers) ---
        Task<List<FiveSimProductAvailability>> GetAvailableHostingDurationsAsync(
            string country, string @operator, CancellationToken cancellationToken);

        Task<FiveSimOrderResult> BuyHostingNumberAsync(
            string country, string @operator, string duration, CancellationToken cancellationToken);

        Task<List<FiveSimInboxMessage>> GetSmsInboxAsync(long orderId, CancellationToken cancellationToken);
    }

    // --- Shared DTOs ---

    


    public class FiveSimProductAvailability
    {
        public required string Product { get; set; } // for hosting, this is
                                                     // a DURATION value
                                                     // like "1month", not
                                                     // a service name
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

    // NEW: one message from a hosting number's inbox
    public class FiveSimInboxMessage
    {
        public long Id { get; set; }
        public string? Sender { get; set; }
        public string? Text { get; set; }
        public string? Code { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}