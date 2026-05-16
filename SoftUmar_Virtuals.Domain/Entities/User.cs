using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }

        public required string FirstName { get; set; }

        public required string  LastName { get; set; }

        public required string  Email { get; set; }

        public required string PasswordHash { get; set; }

        public required string PhoneNumber { get; set; }

        public required string Country { get; set; }

        public string Role { get; set; } = "User";

        public bool EmailVerified { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsSuspended { get; set; }

        public int FailedLoginAttempts { get; set; }

        public DateTime? LockoutEnd { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
