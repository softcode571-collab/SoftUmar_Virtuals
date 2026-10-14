using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;
using SoftUmar_Virtuals.Infrastructure.Services.FiveSim;


namespace SoftUmar_Virtuals.Infrastructure.Services.FiveSim.Activation.Command;

public record BuyActivationCommand(Guid UserId, string Country, string Product, string Operator = "any") : IRequest<Result<BuyActivationResult>>;


public class BuyActivationCommandHandler(
    IRepository<Wallets> walletRepo,
    IRepository<WalletTransaction> transactionRepo,
    IRepository<FiveSimOrder> orderRepo,
    FiveSimClient fiveSimClient,
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

            // Price check BEFORE buying - this is a real external
            // purchase, can't be undone like a DB row. Better to reject
            // up front than buy something the user can't pay for.
            var estimatedPrice = await fiveSimClient.GetCheapestPriceAsync(
                request.Country, request.Product, cancellationToken);

            if (estimatedPrice == null)
            {
                return Result.Fail("This service is not available for the selected country right now.");
            }

            if (wallet.Balance < estimatedPrice)
            {
                await logger.LogInfo(
                    $"Insufficient balance for user {request.UserId}: has {wallet.Balance}, needs ~{estimatedPrice}",
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

            // Re-check balance against the ACTUAL price 5SIM charged -
            // can differ slightly from the estimate, especially with
            // operator="any". This is the real debit amount.
            if (wallet.Balance < fiveSimResult.Price)
            {
                // Edge case: balance was fine for the estimate but the
                // actual price came back higher. You've already bought
                // the number from 5SIM at this point (money already
                // spent on YOUR 5SIM account) - you cannot just discard
                // it. Best option: let the purchase proceed anyway
                // (small discrepancy absorbed), or immediately cancel
                // the 5SIM order if it's still cancellable. Cancelling
                // is the safer choice here.
                try
                {
                    await fiveSimClient.CancelOrderAsync(fiveSimResult.Id, cancellationToken);
                }
                catch (FiveSimApiException)
                {
                    // best-effort - if cancel fails too, log loudly,
                    // this needs manual reconciliation
                    await logger.LogError(
                        $"Price mismatch AND cancel failed for 5SIM order {fiveSimResult.Id} - manual reconciliation needed.",
                        nameof(BuyActivationCommandHandler), null, traceId);
                }

                return Result.Fail("Insufficient balance.");
            }

            wallet.Balance -= fiveSimResult.Price;
            wallet.TotalDebit += fiveSimResult.Price;

            var order = new FiveSimOrder
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                ProviderOrderId = fiveSimResult.Id,
                PhoneNumber = fiveSimResult.Phone ?? string.Empty,
                Operator = fiveSimResult.Operator ?? request.Operator,
                Product = request.Product,
                Country = request.Country,
                Status = fiveSimResult.Status ?? "PENDING",
                Price = fiveSimResult.Price,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = fiveSimResult.Expires,
            };

            var transaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Type = "Debit",
                Amount = fiveSimResult.Price,
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
                $"Activation bought for user {request.UserId}: order {fiveSimResult.Id}, phone {fiveSimResult.Phone}, price {fiveSimResult.Price}",
                nameof(BuyActivationCommandHandler), traceId);

            return Result.Ok(new BuyActivationResult
            {
                ActivationId = order.Id,
                PhoneNumber = order.PhoneNumber,
                Price = order.Price,
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