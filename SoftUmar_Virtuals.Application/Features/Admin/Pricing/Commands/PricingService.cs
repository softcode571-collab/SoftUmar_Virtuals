using SoftUmar_Virtuals.Domain.Entities;

namespace SoftUmar_Virtuals.Application.Interfaces
{
    public class PricingService(IRepository<PlatformSettings> settingsRepo) : IPricingService
    {
        public async Task<PriceBreakdown> GetSellingPriceAsync(decimal fiveSimRawUsdCost, CancellationToken cancellationToken)
        {
            var settings = await GetCurrentSettingsAsync(cancellationToken);

            // Markup applied ONCE, in USD - then the SAME marked-up figure
            // is what gets shown in dollars AND what gets converted to
            // Naira. This matters: it means your margin is identical no
            // matter which currency the user is looking at, rather than
            // accidentally marking up twice or inconsistently.
            var markedUpUsd = fiveSimRawUsdCost * (1 + (settings.MarkupPercentage / 100m));
            var ngnPrice = markedUpUsd * settings.UsdToNgnRate;

            return new PriceBreakdown
            {
                UsdPrice = Math.Round(markedUpUsd, 2, MidpointRounding.AwayFromZero),
                NgnPrice = Math.Round(ngnPrice, 2, MidpointRounding.AwayFromZero),
            };
        }

        private async Task<PlatformSettings> GetCurrentSettingsAsync(CancellationToken cancellationToken)
        {
            var settings = await settingsRepo.GetSingleAsync(s => true);

            if (settings == null)
            {
                return new PlatformSettings
                {
                    UsdToNgnRate = 1650m,
                    MarkupPercentage = 0m,
                };
            }

            return settings;
        }
    }
}