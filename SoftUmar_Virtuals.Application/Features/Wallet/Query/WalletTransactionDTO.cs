using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Application.Features.Wallet.Query
{
    public class WalletTransactionDTO
    {
        public Guid Id { get; set; }
        public required string Type { get; set; }
        public decimal Amount { get; set; }
        public required string Reference { get; set; }
        public required string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
