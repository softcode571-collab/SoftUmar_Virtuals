using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;
using SoftUmar_Virtuals.Domain.Enum;

namespace SoftUmar_Virtuals.Infrastructure.Services.Monnify
{
    // REPLACES your placeholder - same class name, same namespace, so this is
    // a direct swap of the file contents, nothing else needs to change.
    public class MonnifyReservedAccountService(
        MonnifyClient monnifyClient,
        IRepository<Wallets> walletRepo,
        IRepository<User> userRepository,
        ILoggerService logger) : IMonnifyReservedAccountService
    {
        public async Task CreateReservedAccount(Guid userId)
        {
            var traceId = Guid.NewGuid().ToString();

            var wallet = await walletRepo.GetSingleAsync(w => w.UserId == userId);
            var user = await userRepository.GetSingleAsync(u => u.Id == userId);

            if (wallet == null || user == null)
            {
                await logger.LogWarning(
                    $"Wallet or user not found for reserved account creation: {userId}",
                    nameof(MonnifyReservedAccountService), traceId);
                return; // data problem, not transient - don't let Hangfire retry this
            }

            // Idempotency: if this job somehow runs twice, don't recreate.
            if (wallet.ReservedAccountStatus == ReservedAccountStatus.Active)
            {
                await logger.LogInfo(
                    $"Reserved account already active for user {userId}, skipping.",
                    nameof(MonnifyReservedAccountService), traceId);
                return;
            }

            try
            {
                var customerName = $"{user.FirstName} {user.LastName}";

                var result = await monnifyClient.CreateReservedAccountAsync(
                    userId, user.Email, customerName, user.Bvn, user.Nin, CancellationToken.None);

                // THIS is where the account number actually lands in your DB.
                wallet.ReservedAccountNumber = result.AccountNumber;
                wallet.ReservedAccountBank = result.BankName;
                wallet.ReservedAccountStatus = ReservedAccountStatus.Active;

                await walletRepo.SaveChanges(CancellationToken.None);

                await logger.LogInfo(
                    $"Reserved account created for user {userId}: {result.AccountNumber} ({result.BankName})",
                    nameof(MonnifyReservedAccountService), traceId);
            }
            catch (MonnifyApiException ex)
            {
                await logger.LogError(
                    $"Monnify reserved account creation failed for user {userId}.",
                    nameof(MonnifyReservedAccountService), ex, traceId);

                wallet.ReservedAccountStatus = ReservedAccountStatus.Failed;
                await walletRepo.SaveChanges(CancellationToken.None);

                throw; // let Hangfire retry
            }
        }
    }
}