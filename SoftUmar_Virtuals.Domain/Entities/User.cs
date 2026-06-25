using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Domain.Entities
{
    public class User : BaseEntity
    {

        public required string FirstName { get; set; }

        public required string  LastName { get; set; }

        public string? MiddleName { get; set; }

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

        public Wallets Wallet { get; set; }
    }
}
