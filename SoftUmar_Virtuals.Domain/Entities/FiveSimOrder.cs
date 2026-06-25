using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Domain.Entities
{
    public class FiveSimOrder
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string ActivationId { get; set; } = default!;

        public string PhoneNumber { get; set; } = default!;

        public string Country { get; set; } = default!;

        public string Service { get; set; } = default!;

        public decimal Price { get; set; }

        public string Status { get; set; }
        // PENDING, RECEIVED, CANCELLED, FINISHED

        public string? SmsCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public User User { get; set; }
    }
}
