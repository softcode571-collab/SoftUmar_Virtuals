using SoftUmar_Virtuals.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Domain.Entities
{
    public class Wallets
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Email { get; set; } 

        public decimal Balance { get; set; } = 0;

        public ReservedAccountStatus ReservedAccountStatus { get; set; }

        public string? ReservedAccountNumber { get; set; } // null until Active

        public string? ReservedAccountBank { get; set; }   // null until Active

        public decimal TotalCredit { get; set; } = 0;

        public decimal TotalDebit { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public User User { get; set; }
    }
}
