using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;

namespace SoftUmar_Virtuals.Application.Features.Wallet.Query
{
    public record GetWalletQuery(Guid UserId) : IRequest<Result<WalletDTO>>;

    public class GetWalletQueryHandler(
        IRepository<Wallets> walletRepo) : IRequestHandler<GetWalletQuery, Result<WalletDTO>>
    {
        public async Task<Result<WalletDTO>> Handle(GetWalletQuery request, CancellationToken cancellationToken)
        {
            var wallet = await walletRepo.GetSingleAsync(w => w.UserId == request.UserId);

            if (wallet == null)
                return Result.Fail("Wallet not found.");

            return Result.Ok(new WalletDTO
            {
                Id = wallet.Id,
                UserId = wallet.UserId,
                Balance = wallet.Balance,
                TotalCredit = wallet.TotalCredit,
                TotalDebit = wallet.TotalDebit,
                ReservedAccountStatus = wallet.ReservedAccountStatus,
                // Only expose the account number/bank once it's actually
                // Active - while Pending, these are null on the entity
                // anyway, so the frontend naturally shows "setting up your
                // account" instead of blank/broken fields.
                ReservedAccountNumber = wallet.ReservedAccountNumber,
                ReservedAccountBank = wallet.ReservedAccountBank,
                UpdatedAt = wallet.UpdatedAt ?? wallet.CreatedAt,
            });
        }
    }
}