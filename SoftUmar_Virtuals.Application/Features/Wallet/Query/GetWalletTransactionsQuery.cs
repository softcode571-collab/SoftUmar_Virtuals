using FluentResults;
using MediatR;
using SoftUmar_Virtuals.Application.Interfaces;
using SoftUmar_Virtuals.Domain.Entities;

namespace SoftUmar_Virtuals.Application.Features.Wallet.Query
{
    public record GetWalletTransactionsQuery(Guid UserId, int Page = 1, int PageSize = 20) : IRequest<Result<PagedWalletTransactionsResponse>>;

    public class GetWalletTransactionsQueryHandler(
        IRepository<Wallets> walletRepo,
        IRepository<WalletTransaction> transactionRepo)
        : IRequestHandler<GetWalletTransactionsQuery, Result<PagedWalletTransactionsResponse>>
    {
        public async Task<Result<PagedWalletTransactionsResponse>> Handle(
            GetWalletTransactionsQuery request, CancellationToken cancellationToken)
        {
            var wallet = await walletRepo.GetSingleAsync(w => w.UserId == request.UserId);

            if (wallet == null)
                return Result.Fail("Wallet not found.");

            // Cap page size so a client can't request 100,000 rows in one
            // call and hammer your DB - adjust the ceiling if you have a
            // real reason to need more per page.
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var page = Math.Max(request.Page, 1);

            // ASSUMPTION: adjust method names below to whatever your
            // IRepository<T> actually exposes for paged/ordered queries -
            // this assumes GetPagedAsync exists; if it doesn't, you'd use
            // GetAllAsync(...).OrderByDescending(...).Skip().Take() instead,
            // or add a GetPagedAsync overload to your repository interface.
            var (transactions, totalCount) = await transactionRepo.GetFilteredPagedAsync(
                predicate: t => t.WalletId == wallet.Id,
                orderByDescending: t => t.CreatedAt,
                pageNumber: page,
                pageSize: pageSize,
                cancellationToken: cancellationToken);

            return Result.Ok(new PagedWalletTransactionsResponse
            {
                Transactions = transactions.Select(t => new WalletTransactionDTO
                {
                    Id = t.Id,
                    Type = t.Type,
                    Amount = t.Amount,
                    Reference = t.Reference,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt,
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
            });
        }
    }
}