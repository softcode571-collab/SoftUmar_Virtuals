using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Infrastructure.Services.FiveSim.Activation.Command
{
    public record CancelActivationCommand(Guid UserId, Guid ActivationId) : IRequest<Result>;

    public class CancelActivationCommandHandler(
        IRepository<FiveSimOrder> orderRepo,
        IRepository<Wallets> walletRepo,
        IRepository<WalletTransaction> transactionRepo,
        FiveSimClient fiveSimClient,
        ILoggerService logger) : IRequestHandler<CancelActivationCommand, Result>
    {
        public async Task<Result> Handle(CancelActivationCommand request, CancellationToken cancellationToken)
        {
            var traceId = Guid.NewGuid().ToString();

            var order = await orderRepo.GetSingleAsync(
                o => o.Id == request.ActivationId && o.UserId == request.UserId);

            if (order == null)
                return Result.Fail("Activation not found.");

            if (order.Status != "PENDING")
            {
                // Matches 5SIM's own rule: can't cancel once an SMS has
                // arrived, or if it's already in a final state.
                return Result.Fail("This activation can no longer be canceled.");
            }

            try
            {
                var fiveSimResult = await fiveSimClient.CancelOrderAsync(order.ProviderOrderId, cancellationToken);
                order.Status = fiveSimResult.Status ?? "CANCELED";
            }
            catch (FiveSimApiException ex)
            {
                await logger.LogError($"5SIM cancel failed for order {order.ProviderOrderId}.",
                    nameof(CancelActivationCommandHandler), ex, traceId);
                return Result.Fail("Could not cancel this activation right now.");
            }

            // Refund: give the wallet its money back now that the number
            // wasn't used.
            var wallet = await walletRepo.GetSingleAsync(w => w.UserId == request.UserId);

            if (wallet != null)
            {
                wallet.Balance += order.Price;
                wallet.TotalCredit += order.Price;

                var refundTransaction = new WalletTransaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    Type = "Refund",
                    Amount = order.Price,
                    Reference = $"5SIM-REFUND-{order.ProviderOrderId}",
                    Status = "Success",
                    CreatedAt = DateTime.UtcNow,
                };

                await transactionRepo.AddAsync(refundTransaction, cancellationToken);
                await walletRepo.SaveChanges(cancellationToken);
                await transactionRepo.SaveChanges(cancellationToken);
            }
            else
            {
                // Shouldn't happen (every user has a wallet from
                // registration) but don't silently lose the refund if it does.
                await logger.LogError(
                    $"No wallet found to refund user {request.UserId} after canceling activation {order.Id}.",
                    nameof(CancelActivationCommandHandler), null, traceId);
            }

            await orderRepo.SaveChanges(cancellationToken);

            await logger.LogInfo($"Activation {order.Id} canceled and refunded {order.Price} to user {request.UserId}.",
                nameof(CancelActivationCommandHandler), traceId);

            return Result.Ok();
        }
    }
}
