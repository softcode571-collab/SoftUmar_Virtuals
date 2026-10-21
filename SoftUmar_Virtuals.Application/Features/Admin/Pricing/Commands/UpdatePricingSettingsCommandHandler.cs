using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;

namespace SoftUmar_Virtuals.Application.Features.Admin.Pricing.Command
{
    public record UpdatePricingSettingsCommand(
        decimal UsdToNgnRate,
        decimal MarkupPercentage) : IRequest<Result>;

    public class UpdatePricingSettingsCommandHandler(
        IRepository<PlatformSettings> settingsRepo,
        ILoggerService logger) : IRequestHandler<UpdatePricingSettingsCommand, Result>
    {
        public async Task<Result> Handle(UpdatePricingSettingsCommand request, CancellationToken cancellationToken)
        {
            if (request.UsdToNgnRate <= 0)
                return Result.Fail("Exchange rate must be greater than zero.");

            if (request.MarkupPercentage < 0)
                return Result.Fail("Markup cannot be negative.");

            var settings = await settingsRepo.GetSingleAsync(s => true);

            if (settings == null)
            {
                // First time anyone's set this - create the one and only row.
                settings = new PlatformSettings
                {
                    Id = Guid.NewGuid(),
                    UsdToNgnRate = request.UsdToNgnRate,
                    MarkupPercentage = request.MarkupPercentage,
                    UpdatedAt = DateTime.UtcNow,
                };
                await settingsRepo.AddAsync(settings, cancellationToken);
            }
            else
            {
                settings.UsdToNgnRate = request.UsdToNgnRate;
                settings.MarkupPercentage = request.MarkupPercentage;
                settings.UpdatedAt = DateTime.UtcNow;
            }

            await settingsRepo.SaveChanges(cancellationToken);

            await logger.LogInfo(
                $"Pricing settings updated: rate={request.UsdToNgnRate}, markup={request.MarkupPercentage}%",
                nameof(UpdatePricingSettingsCommandHandler), Guid.NewGuid().ToString());

            return Result.Ok();
        }
    }
}