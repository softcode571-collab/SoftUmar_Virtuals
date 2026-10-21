namespace SoftUmar_Virtuals.Application.Interfaces
{
    // What the frontend actually sees: your markup is already baked into
    // BOTH figures. UsdPrice is NOT 5SIM's raw cost - it's your raw cost
    // PLUS your markup, just still expressed in dollars. NgnPrice is that
    // same marked-up USD figure, converted at your set rate.
    public class PriceBreakdown
    {
        public decimal UsdPrice { get; set; }
        public decimal NgnPrice { get; set; }
    }

    public interface IPricingService
    {
        Task<PriceBreakdown> GetSellingPriceAsync(decimal fiveSimRawUsdCost, CancellationToken cancellationToken);
    }
}