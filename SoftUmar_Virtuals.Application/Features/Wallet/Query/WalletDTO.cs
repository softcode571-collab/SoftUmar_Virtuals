using SoftUmar_Virtuals.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Application.Features.Wallet.Query
{
    public class WalletDTO
    {
        public Guid Id { get; internal set; }
        public Guid UserId { get; internal set; }
        public decimal Balance { get; internal set; }
        public decimal TotalCredit { get; internal set; }
        public decimal TotalDebit { get; internal set; }
        public ReservedAccountStatus ReservedAccountStatus { get; internal set; }
        public string? ReservedAccountNumber { get; internal set; }
        public string? ReservedAccountBank { get; internal set; }
        public DateTime UpdatedAt { get; internal set; }
    }
}
