using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Domain.Entities
{
    public class WalletTransaction
    {
        public Guid Id { get; set; }

        public Guid WalletId { get; set; }

        public Guid UserId { get; set; }

        public decimal Amount { get; set; }

        public string Type { get; set; }
        // CREDIT or DEBIT

        public string Reference { get; set; } = default!;
        // Paystack ref, 5sim order, etc

        public string Reason { get; set; } = default!;
        // Deposit, Buy Number, Refund

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Wallets Wallet { get; set; }
    }
}
