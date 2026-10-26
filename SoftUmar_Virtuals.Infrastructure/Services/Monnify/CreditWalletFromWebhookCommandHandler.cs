using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;


namespace SoftUmar_Virtuals.Infrastructure.Services.Monnify
{
    public record CreditWalletFromWebhookCommand(MonnifyWebhookEventData EventData) : IRequest<Result>;

    public class CreditWalletFromWebhookCommandHandler(
        IRepository<Wallets> walletRepo,
        IRepository<WalletTransaction> transactionRepo, // ASSUMPTION: adjust field
                                                        // names below (WalletId, Type, Amount, Reference, Status,
                                                        // CreatedAt) to match your actual WalletTransaction entity - this
                                                        // is built from your requirements doc's table shape, not a file
                                                        // you've shown me.
        
        INotificationService notificationService,
        ILoggerService logger) : IRequestHandler<CreditWalletFromWebhookCommand, Result>
    {
        public async Task<Result> Handle(CreditWalletFromWebhookCommand request, CancellationToken cancellationToken)
        {
            var traceId = Guid.NewGuid().ToString();
            var data = request.EventData;

            try
            {
                // Only credit on an actual successful payment - ignore
                // pending/failed notifications (Monnify can send webhooks for
                // non-success states too, depending on event type).
                if (!string.Equals(data.PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase))
                {
                    await logger.LogInfo(
                        $"Webhook received with non-PAID status '{data.PaymentStatus}' for {data.TransactionReference} - ignored.",
                        nameof(CreditWalletFromWebhookCommandHandler), traceId);
                    return Result.Ok();
                }

                // IDEMPOTENCY: the single most important check in this whole
                // file. Monnify can and will send the same webhook more than
                // once (retries on their end if your server is slow to
                // respond, network blips, etc). Without this check, a
                // retried webhook would credit the wallet TWICE for one
                // real transfer.
                var alreadyProcessed = transactionRepo.Exists(
                    t => t.Reference == data.TransactionReference);

                if (alreadyProcessed)
                {
                    await logger.LogInfo(
                        $"Webhook for {data.TransactionReference} already processed - skipping duplicate.",
                        nameof(CreditWalletFromWebhookCommandHandler), traceId);
                    return Result.Ok();
                }

                var accountNumber = data.DestinationAccountInformation?.AccountNumber;

                if (string.IsNullOrWhiteSpace(accountNumber))
                {
                    await logger.LogWarning(
                        $"Webhook {data.TransactionReference} had no destination account number.",
                        nameof(CreditWalletFromWebhookCommandHandler), traceId);
                    return Result.Fail("No destination account in webhook payload.");
                }

                var wallet = await walletRepo.GetSingleAsync(w => w.ReservedAccountNumber == accountNumber);

                if (wallet == null)
                {
                    // This would mean money arrived at an account number that
                    // doesn't match any wallet in your system - shouldn't
                    // happen if reserved accounts are only ever created by
                    // YOUR app, but worth logging loudly if it ever does,
                    // since it likely means money needs manual investigation.
                    await logger.LogError(
                        $"No wallet found for account number {accountNumber} - transaction {data.TransactionReference} could not be credited.",
                        nameof(CreditWalletFromWebhookCommandHandler), null, traceId);
                    return Result.Fail("No matching wallet for this account number.");
                }

                wallet.Balance += data.AmountPaid;
                wallet.TotalCredit += data.AmountPaid;

                var transaction = new WalletTransaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    Type = "Credit",
                    Amount = data.AmountPaid,
                    Reference = data.TransactionReference,
                    Status = "Success",
                    CreatedAt = DateTime.UtcNow,
                };

                await walletRepo.SaveChanges(cancellationToken);
                await transactionRepo.AddAsync(transaction, cancellationToken);
                await transactionRepo.SaveChanges(cancellationToken);


                await notificationService.NotifyWalletUpdatedAsync(wallet.UserId, wallet.Balance);

                await logger.LogInfo(
                    $"Wallet {wallet.Id} credited {data.AmountPaid} from transaction {data.TransactionReference}. New balance: {wallet.Balance}",
                    nameof(CreditWalletFromWebhookCommandHandler), traceId);

                // FUTURE (not built yet, separate module): push a SignalR
                // "WalletUpdated" event here so the frontend updates live
                // instead of the user having to refresh. Flagging it since
                // it's in your spec's Real-Time Notifications module, but
                // out of scope for this file.

                return Result.Ok();
            }
            catch (Exception ex)
            {
                await logger.LogError("Failed to credit wallet from webhook.",
                    nameof(CreditWalletFromWebhookCommandHandler), ex, traceId);
                return Result.Fail("An error occurred processing the payment.");
            }
        }
    }
}