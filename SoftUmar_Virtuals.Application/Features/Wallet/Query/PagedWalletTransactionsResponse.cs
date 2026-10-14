using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Application.Features.Wallet.Query
{
    public class PagedWalletTransactionsResponse
    {
        public required List<WalletTransactionDTO> Transactions { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}
