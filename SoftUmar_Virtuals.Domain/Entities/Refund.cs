using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Domain.Entities
{
    public class Refund
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid FiveSimOrderId { get; set; }

        public decimal Amount { get; set; }

        public string Reason { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
