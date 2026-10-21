using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;
using SoftUmar_Virtuals.Infrastructure.Services.FiveSim;

namespace SoftUmar_Virtuals.Application.Features.Activation.Command
{
    public record BuyActivationCommand(
        Guid UserId,
        string Country,
        string Product,
        string Operator = "any") : IRequest<Result<BuyActivationResult>>;

    public class BuyActivationResult
    {
        public Guid ActivationId { get; set; }
        public required string PhoneNumber { get; set; }
        public decimal PriceUsd { get; set; } // NEW
        public decimal PriceNgn { get; set; } // what actually gets charged
                                              // to the wallet - this one
                                              // matters for the deduction
        public required string Status { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class BuyActivationCommandHandler(
        IRepository<Wallets> walletRepo,
        IRepository<WalletTransaction> transactionRepo,
        IRepository<FiveSimOrder> orderRepo,
        IFiveSimClient fiveSimClient,
        IPricingService pricingService,
        ILoggerService logger) : IRequestHandler<BuyActivationCommand, Result<BuyActivationResult>>
    {
        public async Task<Result<BuyActivationResult>> Handle(
            BuyActivationCommand request, CancellationToken cancellationToken)
        {
            var traceId = Guid.NewGuid().ToString();

            try
            {
                var wallet = await walletRepo.GetSingleAsync(w => w.UserId == request.UserId);

                if (wallet == null)
                    return Result.Fail("Wallet not found.");

                var estimatedRawUsd = await fiveSimClient.GetCheapestPriceAsync(
                    request.Country, request.Product, cancellationToken);

                if (estimatedRawUsd == null)
                {
                    return Result.Fail("This service is not available for the selected country right now.");
                }

                var estimatedPricing = await pricingService.GetSellingPriceAsync(
                    estimatedRawUsd.Value, cancellationToken);

                // The WALLET is NGN-denominated (funded via Monnify), so the
                // balance check always uses NgnPrice, never UsdPrice -
                // UsdPrice is purely a DISPLAY figure for the frontend.
                if (wallet.Balance < estimatedPricing.NgnPrice)
                {
                    await logger.LogInfo(
                        $"Insufficient balance for user {request.UserId}: has {wallet.Balance}, needs ~{estimatedPricing.NgnPrice}",
                        nameof(BuyActivationCommandHandler), traceId);
                    return Result.Fail("Insufficient balance.");
                }

                FiveSimOrderResult fiveSimResult;
                try
                {
                    fiveSimResult = await fiveSimClient.BuyActivationNumberAsync(
                        request.Country, request.Operator, request.Product, cancellationToken);
                }
                catch (FiveSimApiException ex) when (ex.Code == "no_numbers")
                {
                    return Result.Fail("No numbers available for this country/service right now.");
                }
                catch (FiveSimApiException ex)
                {
                    await logger.LogError("5SIM purchase failed.",
                        nameof(BuyActivationCommandHandler), ex, traceId);
                    return Result.Fail("Service unavailable. Please try again shortly.");
                }

                var actualPricing = await pricingService.GetSellingPriceAsync(
                    fiveSimResult.Price, cancellationToken);

                if (wallet.Balance < actualPricing.NgnPrice)
                {
                    try
                    {
                        await fiveSimClient.CancelOrderAsync(fiveSimResult.Id, cancellationToken);
                    }
                    catch (FiveSimApiException)
                    {
                        await logger.LogError(
                            $"Price mismatch AND cancel failed for 5SIM order {fiveSimResult.Id} - manual reconciliation needed.",
                            nameof(BuyActivationCommandHandler), null, traceId);
                    }

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
                    Service = request.Product,
                    Country = request.Country,
                    Status = fiveSimResult.Status ?? "PENDING",
                    Price = actualPricing.NgnPrice, // the single source of
                                                    // truth stored in your DB stays NGN (matches the
                                                    // wallet/WalletTransaction currency) - PriceUsd is
                                                    // only ever a display-time calculation, recomputed
                                                    // live rather than stored, so it always reflects
                                                    // your CURRENT rate if you check it later rather
                                                    // than a stale snapshot
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = fiveSimResult.Expires,
                };

                var transaction = new WalletTransaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    Type = "Debit",
                    Amount = actualPricing.NgnPrice,
                    Reference = $"5SIM-{fiveSimResult.Id}",
                    Status = "Success",
                    CreatedAt = DateTime.UtcNow,
                };

                await orderRepo.AddAsync(order, cancellationToken);
                await walletRepo.SaveChanges(cancellationToken);
                await transactionRepo.AddAsync(transaction, cancellationToken);
                await transactionRepo.SaveChanges(cancellationToken);
                await orderRepo.SaveChanges(cancellationToken);

                await logger.LogInfo(
                    $"Activation bought for user {request.UserId}: order {fiveSimResult.Id}, phone {fiveSimResult.Phone}, " +
                    $"5SIM raw cost ${fiveSimResult.Price}, sold at ${actualPricing.UsdPrice} / N{actualPricing.NgnPrice}",
                    nameof(BuyActivationCommandHandler), traceId);

                return Result.Ok(new BuyActivationResult
                {
                    ActivationId = order.Id,
                    PhoneNumber = order.PhoneNumber,
                    PriceUsd = actualPricing.UsdPrice,
                    PriceNgn = order.Price,
                    Status = order.Status,
                    ExpiresAt = order.ExpiresAt,
                });
            }
            catch (Exception ex)
            {
                await logger.LogError("Buy activation failed.",
                    nameof(BuyActivationCommandHandler), ex, traceId);
                return Result.Fail("An error occurred processing your purchase.");
            }
        }
    }
}