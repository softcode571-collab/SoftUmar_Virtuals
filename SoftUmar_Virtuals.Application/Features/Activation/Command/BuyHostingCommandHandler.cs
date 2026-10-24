using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;
using SoftUmar_Virtuals.Infrastructure.Services.FiveSim;

namespace SoftUmar_Virtuals.Application.Features.Activation.Command
{
    public record BuyHostingCommand(
        Guid UserId,
        string Country,
        string Duration, // e.g. "1month", "10days", "1day", "3hours" - a
                         // value from GetAvailableHostingDurationsAsync,
                         // NOT a service name like "whatsapp"
        string Operator = "any") : IRequest<Result<BuyHostingResult>>;

    public class BuyHostingResult
    {
        public Guid ActivationId { get; set; }
        public required string PhoneNumber { get; set; }
        public decimal PriceUsd { get; set; }
        public decimal PriceNgn { get; set; }
        public required string Status { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public required string Disclaimer { get; set; } // honest messaging,
                                                        // per your decision - this is NOT a permanent number
    }

    public class BuyHostingCommandHandler(
        IRepository<Wallets> walletRepo,
        IRepository<WalletTransaction> transactionRepo,
        IRepository<FiveSimOrder> orderRepo,
        IFiveSimClient fiveSimClient,
        IPricingService pricingService,
        ILoggerService logger) : IRequestHandler<BuyHostingCommand, Result<BuyHostingResult>>
    {
        public async Task<Result<BuyHostingResult>> Handle(
            BuyHostingCommand request, CancellationToken cancellationToken)
        {
            var traceId = Guid.NewGuid().ToString();

            try
            {
                var wallet = await walletRepo.GetSingleAsync(w => w.UserId == request.UserId);

                if (wallet == null)
                    return Result.Fail("Wallet not found.");

                // Hosting pricing comes from the products list directly
                // (price per duration), not the /guest/prices endpoint -
                // that endpoint is activation-specific.
                var durations = await fiveSimClient.GetAvailableHostingDurationsAsync(
                    request.Country, request.Operator, cancellationToken);

                var matchingDuration = durations.FirstOrDefault(d => d.Product == request.Duration);

                if (matchingDuration == null)
                {
                    return Result.Fail("This rental duration is not available for the selected country right now.");
                }

                var estimatedPricing = await pricingService.GetSellingPriceAsync(
                    matchingDuration.Price, cancellationToken);

                if (wallet.Balance < estimatedPricing.NgnPrice)
                {
                    return Result.Fail("Insufficient balance.");
                }

                FiveSimOrderResult fiveSimResult;
                try
                {
                    fiveSimResult = await fiveSimClient.BuyHostingNumberAsync(
                        request.Country, request.Operator, request.Duration, cancellationToken);
                }
                catch (FiveSimApiException ex) when (ex.Code == "no_numbers")
                {
                    return Result.Fail("No numbers available for rental right now.");
                }
                catch (FiveSimApiException ex)
                {
                    await logger.LogError("5SIM hosting purchase failed.",
                        nameof(BuyHostingCommandHandler), ex, traceId);
                    return Result.Fail("Service unavailable. Please try again shortly.");
                }

                var actualPricing = await pricingService.GetSellingPriceAsync(
                    fiveSimResult.Price, cancellationToken);

                if (wallet.Balance < actualPricing.NgnPrice)
                {
                    // Note: unlike activation, hosting orders generally
                    // CANNOT be canceled (5SIM rejects cancel for hosting
                    // orders with a 400). If this mismatch happens, you're
                    // stuck having bought it - logging loudly is the best
                    // you can do here.
                    await logger.LogError(
                        $"Price mismatch for hosting order {fiveSimResult.Id} and hosting orders cannot be canceled - manual reconciliation needed.",
                        nameof(BuyHostingCommandHandler), null, traceId);
                    return Result.Fail("Insufficient balance.");
                }

                wallet.Balance -= actualPricing.NgnPrice;
                wallet.TotalDebit += actualPricing.NgnPrice;

                var order = new FiveSimOrder
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    ActivationId = fiveSimResult.Id.ToString(),
                    PhoneNumber = fiveSimResult.Phone ?? string.Empty,
                    Operator = fiveSimResult.Operator ?? request.Operator,
                    Service = request.Duration, // the "service" field holds
                                                // the duration for hosting
                                                // orders, same column reused
                    Country = request.Country,
                    Status = fiveSimResult.Status ?? "PENDING",
                    Price = actualPricing.NgnPrice,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = fiveSimResult.Expires,
                    Category = "Hosting", // NEW field - requires adding to
                                          // FiveSimOrder entity + migration
                };

                var transaction = new WalletTransaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    Type = "Debit",
                    Amount = actualPricing.NgnPrice,
                    Reference = $"5SIM-HOSTING-{fiveSimResult.Id}",
                    Status = "Success",
                    CreatedAt = DateTime.UtcNow,
                };

                await orderRepo.AddAsync(order, cancellationToken);
                await walletRepo.SaveChanges(cancellationToken);
                await transactionRepo.AddAsync(transaction, cancellationToken);
                await transactionRepo.SaveChanges(cancellationToken);
                await orderRepo.SaveChanges(cancellationToken);

                await logger.LogInfo(
                    $"Hosting number bought for user {request.UserId}: order {fiveSimResult.Id}, phone {fiveSimResult.Phone}, " +
                    $"duration {request.Duration}, charged N{actualPricing.NgnPrice}",
                    nameof(BuyHostingCommandHandler), traceId);

                return Result.Ok(new BuyHostingResult
                {
                    ActivationId = order.Id,
                    PhoneNumber = order.PhoneNumber,
                    PriceUsd = actualPricing.UsdPrice,
                    PriceNgn = order.Price,
                    Status = order.Status,
                    ExpiresAt = order.ExpiresAt,
                    Disclaimer = "This number is rented for a fixed period and is NOT permanent. " +
                                 "Once the rental expires, the number may be reassigned to another customer. " +
                                 "Any accounts (e.g. WhatsApp) registered on this number may become inaccessible after expiry.",
                });
            }
            catch (Exception ex)
            {
                await logger.LogError("Buy hosting failed.",
                    nameof(BuyHostingCommandHandler), ex, traceId);
                return Result.Fail("An error occurred processing your purchase.");
            }
        }
    }
}