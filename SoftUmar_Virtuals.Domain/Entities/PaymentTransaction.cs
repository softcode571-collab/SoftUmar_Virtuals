using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Domain.Entities
{
    public class PaymentTransaction
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public decimal Amount { get; set; }

        public string Provider { get; set; }
        // Paystack, Flutterwave

        public string Reference { get; set; } = default!;

        public string Status { get; set; }
        // SUCCESS, FAILED, PENDING

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
