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

        public string email { get; set; } 

        public decimal Balance { get; set; } = 0;

        public decimal TotalCredit { get; set; } = 0;

        public decimal TotalDebit { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public User User { get; set; }
    }
}
