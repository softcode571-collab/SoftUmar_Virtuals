using SoftUmar_Virtuals.Domain.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public required string Nin { get; set; }

        public required string Bvn { get; set; }

        public bool IsEmailVerified { get; set; }

        public string? PasswordResetToken { get; set; }

        public DateTime PasswordResetTokenExpiry { get; set; }

        public int PasswordResetAttemptCount { get; set; }

        public UserStatus Status { get; set; }

        public string? EmailVerificationToken { get; set; }

        public DateTime? EmailVerificationTokenExpiry { get; set; }

        public int EmailVerificationAttemptCount { get; set; }

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
